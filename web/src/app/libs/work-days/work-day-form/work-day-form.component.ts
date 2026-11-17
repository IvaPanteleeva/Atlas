import {
    afterNextRender,
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    effect,
    inject,
    Injector,
    input,
    output,
    signal,
    untracked,
    viewChildren,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
    AbstractControl,
    FormArray,
    FormControl,
    FormGroup,
    NonNullableFormBuilder,
    ReactiveFormsModule,
    ValidationErrors,
    Validators,
} from '@angular/forms';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCheckboxModule } from 'ng-zorro-antd/checkbox';
import { NzDatePickerModule } from 'ng-zorro-antd/date-picker';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSelectComponent, NzSelectModule } from 'ng-zorro-antd/select';
import { Person } from '../../people/person.model';
import { MONEY_MAX, MONEY_MIN, Service } from '../../services/service.model';
import { FieldErrors } from '../../shared/api-errors';
import { DATE_PICKER_PROVIDERS } from '../../shared/date-adapter';
import { EurPipe } from '../../shared/eur.pipe';
import { fromIsoDate, isAfterToday, toIsoDate } from '../../shared/iso-date';
import { DecimalInputDirective } from '../../shared/decimal-input';
import { resolveRate } from '../../shared/price-resolver';
import { Unit, UNIT_LABELS, UNIT_OPTIONS } from '../../shared/units';
import { SitePrice } from '../../site-prices/site-price.model';
import { SaveWorkDayRequest, WORK_DAY_LIMITS, WorkDay, WorkDayWorker, WorkItem } from '../work-day.model';

/** Select value of „Свободен текст“ (nz-select shows null as an empty field, so an empty string is used). */
const FREE_TEXT = '';
const DEFAULT_UNIT: Unit = 'SquareMeter';

/** Server error keys that have a place in the form; the rest go to the alert. */
const DAY_FIELDS = ['date', 'title', 'note', 'items', 'workers'];
const WORKER_FIELD_KEY = /^workers\[\d+\]\.(personId|pay)$/;
const ITEM_FIELD_KEY = /^items\[\d+\]\.(serviceId|description|quantity|unit|rate|needsReview)$/;

/**
 * One chosen person with their pay for the day; name is only for the label.
 * A partner's pay is optional (rule of round 31): empty = works for the shared profit, filled = a day's wage.
 */
type WorkerForm = FormGroup<{
    personId: FormControl<string>;
    name: FormControl<string>;
    isPartner: FormControl<boolean>;
    pay: FormControl<number | null>;
}>;

interface WorkerOption {
    id: string;
    name: string;
}

type ItemForm = FormGroup<{
    id: FormControl<string | null>;
    serviceId: FormControl<string>;
    description: FormControl<string>;
    quantity: FormControl<number | null>;
    unit: FormControl<Unit>;
    rate: FormControl<number | null>;
    needsReview: FormControl<boolean>;
}>;

function notBlank(control: AbstractControl<string>): ValidationErrors | null {
    return control.value.trim() ? null : { required: true };
}

function positive(control: AbstractControl<number | null>): ValidationErrors | null {
    return control.value === null || control.value > 0 ? null : { positive: true };
}

function orNull(value: string): string | null {
    const trimmed = value.trim();
    return trimmed ? trimmed : null;
}

/** Marks every control so its error shows, also untouched ones. */
function revealErrors(control: AbstractControl): void {
    if (control instanceof FormGroup || control instanceof FormArray) {
        Object.values(control.controls).forEach(revealErrors);
        return;
    }
    control.markAsDirty();
    control.markAsTouched();
    // With events: nz-form-control shows the error tip on statusChanges, also for untouched fields.
    control.updateValueAndValidity();
}

/**
 * Preview of the day total while typing, in whole cents per item (quantity has up to 3 decimals, rate 2).
 * Only a preview: the saved amounts and totals come from the server.
 */
function previewTotal(items: { quantity: number | null; rate: number | null }[]): { total: number; missing: number } {
    let cents = 0;
    let missing = 0;
    for (const { quantity, rate } of items) {
        if (rate === null) {
            missing++;
        } else if (quantity !== null) {
            cents += Math.round((Math.round(quantity * 1000) * Math.round(rate * 100)) / 1000);
        }
    }
    return { total: cents / 100, missing };
}

/**
 * Who can be picked: the crew, plus the people already on the day (even if no longer crew or not in the list),
 * sorted by name.
 */
function workerOptions(people: Person[], dayWorkers: WorkDayWorker[]): WorkerOption[] {
    const options = new Map<string, string>();
    for (const person of people) {
        if (person.isCrew) {
            options.set(person.id, person.name);
        }
    }
    for (const worker of dayWorkers) {
        if (!options.has(worker.id)) {
            options.set(worker.id, people.find((person) => person.id === worker.id)?.name ?? worker.name);
        }
    }
    return [...options].map(([id, name]) => ({ id, name })).sort((a, b) => a.name.localeCompare(b.name, 'bg'));
}

/**
 * Dumb form for a work day (create when day is null, edit otherwise): date, title, who worked, item cards, note.
 * Choosing a service fills description, unit and price (site price → default → „цена?“).
 * Without canEditPrices the price is read-only and sent as null: the server resolves it (today Owner and Manager both edit prices).
 */
@Component({
    selector: 'atlas-work-day-form',
    imports: [
        ReactiveFormsModule,
        NzAlertModule,
        NzButtonModule,
        NzCheckboxModule,
        NzDatePickerModule,
        NzFormModule,
        NzIconModule,
        NzInputModule,
        NzSelectModule,
        DecimalInputDirective,
        EurPipe,
    ],
    providers: DATE_PICKER_PROVIDERS,
    templateUrl: './work-day-form.component.html',
    styleUrl: './work-day-form.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkDayFormComponent {
    readonly day = input<WorkDay | null>(null);
    readonly services = input<Service[]>([]);
    readonly sitePrices = input<SitePrice[]>([]);
    /** Everyone from GET /api/people; the crew is offered under „Кой работи“. */
    readonly people = input<Person[]>([]);
    /** true: may type or change prices; false: prices are read-only and sent as null. */
    readonly canEditPrices = input(false);
    readonly canDelete = input(false);
    readonly saving = input(false);
    readonly error = input<string | null>(null);
    readonly fieldErrors = input<FieldErrors>({});

    readonly save = output<SaveWorkDayRequest>();
    readonly delete = output<void>();
    readonly cancel = output<void>();

    protected readonly limits = WORK_DAY_LIMITS;
    protected readonly unitOptions = UNIT_OPTIONS;
    protected readonly freeText = FREE_TEXT;
    /** Days after today cannot be picked (the server refuses them too). */
    protected readonly disabledDate = isAfterToday;

    private readonly fb = inject(NonNullableFormBuilder);
    private readonly destroyRef = inject(DestroyRef);
    private readonly injector = inject(Injector);
    private readonly serviceSelects = viewChildren('serviceSelect', { read: NzSelectComponent });

    protected readonly form = this.fb.group({
        date: this.fb.control<Date | null>(null, Validators.required),
        title: ['', Validators.maxLength(WORK_DAY_LIMITS.title)],
        note: ['', Validators.maxLength(WORK_DAY_LIMITS.note)],
        items: this.fb.array<ItemForm>([]),
        workers: this.fb.array<WorkerForm>([]),
    });

    protected readonly confirmingDelete = signal(false);
    /** Ids of the people chosen under „Кой работи“. */
    protected readonly workerIds = signal<string[]>([]);
    protected readonly workerOptions = computed(() => workerOptions(this.people(), this.day()?.workers ?? []));
    /** Raw item values (disabled read-only prices included), kept in a signal for the live total. */
    private readonly itemValues = signal<{ quantity: number | null; rate: number | null }[]>([]);
    protected readonly summary = computed(() => previewTotal(this.itemValues()));
    /** Pays of the chosen people, for the live „Хора: X €“ (a preview; the server sums). */
    protected readonly workerPays = signal<{ pay: number | null; isPartner: boolean }[]>([]);
    /** An empty partner pay is neither a cost nor „без сума“, so it is left out; a filled one counts. */
    private readonly countedPays = computed(() =>
        this.workerPays().filter((worker) => !worker.isPartner || worker.pay !== null),
    );
    protected readonly showWorkerTotal = computed(() => this.countedPays().length > 0);
    protected readonly workerSummary = computed(() =>
        previewTotal(this.countedPays().map(({ pay }) => ({ quantity: 1, rate: pay }))),
    );

    protected readonly serviceOptions = computed(() =>
        this.services().map((service) => ({
            value: service.id,
            label: `${service.name} · ${UNIT_LABELS[service.unit]}`,
        })),
    );

    protected readonly otherErrors = computed(() =>
        Object.entries(this.fieldErrors())
            .filter(([key]) => !DAY_FIELDS.includes(key) && !ITEM_FIELD_KEY.test(key) && !WORKER_FIELD_KEY.test(key))
            .map(([, message]) => message),
    );

    constructor() {
        effect(() => {
            const day = this.day();
            untracked(() => this.reset(day));
        });

        effect(() => {
            const canEdit = this.canEditPrices();
            untracked(() => {
                for (const item of this.items.controls) {
                    if (canEdit) {
                        item.controls.rate.enable({ emitEvent: false });
                    } else {
                        item.controls.rate.disable({ emitEvent: false });
                    }
                }
            });
        });

        this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.syncItemValues());
    }

    protected get workerRows(): FormArray<WorkerForm> {
        return this.form.controls.workers;
    }

    protected get items(): FormArray<ItemForm> {
        return this.form.controls.items;
    }

    protected serverError(key: string): string | null {
        return this.fieldErrors()[key] ?? null;
    }

    protected isWorker(id: string): boolean {
        return this.workerIds().includes(id);
    }

    /** Checking prefills the pay from the daily rate (a partner's starts empty); unchecking drops the row. */
    protected toggleWorker(id: string): void {
        const index = this.workerRows.controls.findIndex((row) => row.controls.personId.value === id);
        if (index >= 0) {
            this.workerRows.removeAt(index);
        } else {
            const option = this.workerOptions().find((candidate) => candidate.id === id);
            const person = this.people().find((candidate) => candidate.id === id);
            const isPartner = this.isPartner(id, false);
            const pay = isPartner ? null : (person?.dailyRate ?? null);
            this.workerRows.push(this.buildWorker(id, option?.name ?? '', isPartner, pay));
            this.sortWorkers();
        }
        this.workerIds.set(this.workerRows.controls.map((row) => row.controls.personId.value));
    }

    protected addItem(): void {
        if (this.items.length >= WORK_DAY_LIMITS.maxItems) {
            return;
        }
        this.items.push(this.buildItem(null));
        afterNextRender(() => this.serviceSelects().at(-1)?.focus(), { injector: this.injector });
    }

    protected removeItem(index: number): void {
        if (this.items.length > 1) {
            this.items.removeAt(index);
        }
    }

    protected requestDelete(): void {
        if (this.confirmingDelete()) {
            this.delete.emit();
        } else {
            this.confirmingDelete.set(true);
        }
    }

    protected submit(): void {
        if (this.saving()) {
            return;
        }
        if (this.form.invalid) {
            revealErrors(this.form);
            return;
        }
        const value = this.form.getRawValue();
        const canEditPrices = this.canEditPrices();
        this.save.emit({
            date: toIsoDate(value.date!),
            title: orNull(value.title),
            note: orNull(value.note),
            // In the order shown (by name).
            workers: value.workers.map((worker) => ({
                personId: worker.personId,
                pay: worker.pay,
            })),
            items: value.items.map((item) => ({
                id: item.id,
                serviceId: item.serviceId || null,
                description: item.description.trim(),
                quantity: item.quantity!,
                unit: item.unit,
                rate: canEditPrices ? item.rate : null,
                needsReview: item.needsReview,
            })),
        });
    }

    private buildItem(item: WorkItem | null): ItemForm {
        const group: ItemForm = this.fb.group({
            id: this.fb.control<string | null>(item?.id ?? null),
            serviceId: [item?.serviceId ?? FREE_TEXT],
            description: [item?.description ?? '', [notBlank, Validators.maxLength(WORK_DAY_LIMITS.description)]],
            quantity: this.fb.control<number | null>(item?.quantity ?? null, [
                Validators.required,
                positive,
                Validators.max(WORK_DAY_LIMITS.quantityMax),
            ]),
            unit: [item?.unit ?? DEFAULT_UNIT],
            rate: this.fb.control<number | null>(item?.rate ?? null, [
                Validators.min(MONEY_MIN),
                Validators.max(MONEY_MAX),
            ]),
            needsReview: [item?.needsReview ?? false],
        });
        if (!this.canEditPrices()) {
            group.controls.rate.disable({ emitEvent: false });
        }

        let lastServiceId = group.controls.serviceId.value;
        group.controls.serviceId.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((serviceId) => {
            // Only a real change; submit re-validates with events and must not overwrite typed values.
            if (serviceId === lastServiceId) {
                return;
            }
            lastServiceId = serviceId;
            const service = this.services().find((candidate) => candidate.id === serviceId);
            if (service) {
                group.controls.description.setValue(service.name);
                group.controls.unit.setValue(service.unit);
            }
            // Without canEditPrices the server keeps the stored price of an existing item, so the form shows that one.
            if (!this.canEditPrices() && group.controls.id.value) {
                return;
            }
            group.controls.rate.setValue(resolveRate(serviceId || null, this.sitePrices(), this.services()));
        });
        return group;
    }

    /** Partner by the current people list; for someone not in it, by the flag stored on the day. */
    private isPartner(personId: string, onDay: boolean): boolean {
        return this.people().find((person) => person.id === personId)?.isPartner ?? onDay;
    }

    private buildWorker(personId: string, name: string, isPartner: boolean, pay: number | null): WorkerForm {
        return this.fb.group({
            personId: [personId],
            name: [name],
            isPartner: [isPartner],
            pay: this.fb.control<number | null>(pay, [Validators.min(MONEY_MIN), Validators.max(MONEY_MAX)]),
        });
    }

    /** Rows in the same order as the pills (by name). */
    private sortWorkers(): void {
        const order = this.workerOptions().map((option) => option.id);
        const rows = [...this.workerRows.controls].sort(
            (a, b) => order.indexOf(a.controls.personId.value) - order.indexOf(b.controls.personId.value),
        );
        this.workerRows.clear({ emitEvent: false });
        rows.forEach((row) => this.workerRows.push(row, { emitEvent: false }));
        this.workerRows.updateValueAndValidity();
    }

    private syncItemValues(): void {
        this.itemValues.set(this.items.getRawValue().map(({ quantity, rate }) => ({ quantity, rate })));
        this.workerPays.set(this.workerRows.getRawValue().map(({ pay, isPartner }) => ({ pay, isPartner })));
    }

    private reset(day: WorkDay | null): void {
        this.items.clear({ emitEvent: false });
        for (const item of day?.items.length ? day.items : [null]) {
            this.items.push(this.buildItem(item), { emitEvent: false });
        }
        this.form.reset(
            {
                date: day ? fromIsoDate(day.date) : new Date(),
                title: day?.title ?? '',
                note: day?.note ?? '',
            },
            { emitEvent: false },
        );
        // Existing days keep their stored pay (not the current daily rate).
        this.workerRows.clear({ emitEvent: false });
        for (const worker of day?.workers ?? []) {
            const name = this.workerOptions().find((option) => option.id === worker.id)?.name ?? worker.name;
            const isPartner = this.isPartner(worker.id, worker.isPartner);
            this.workerRows.push(this.buildWorker(worker.id, name, isPartner, worker.pay), { emitEvent: false });
        }
        this.sortWorkers();
        this.syncItemValues();
        this.workerIds.set(this.workerRows.controls.map((row) => row.controls.personId.value));
        this.confirmingDelete.set(false);
    }
}
