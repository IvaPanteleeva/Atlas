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
import { MONEY_MAX, MONEY_MIN, Service } from '../../services/service.model';
import { FieldErrors } from '../../shared/api-errors';
import { DATE_PICKER_PROVIDERS } from '../../shared/date-adapter';
import { EurPipe } from '../../shared/eur.pipe';
import { DecimalInputDirective } from '../../shared/decimal-input';
import { resolveRate } from '../../shared/price-resolver';
import { Unit, UNIT_LABELS, UNIT_OPTIONS } from '../../shared/units';
import { SitePrice } from '../../site-prices/site-price.model';
import { SaveWorkDayRequest, WORK_DAY_LIMITS, WorkDay, WorkItem } from '../work-day.model';

/** Select value of „Свободен текст“ (nz-select shows null as an empty field, so an empty string is used). */
const FREE_TEXT = '';
const DEFAULT_UNIT: Unit = 'SquareMeter';

/** Server error keys that have a place in the form; the rest go to the alert. */
const DAY_FIELDS = ['date', 'title', 'note', 'items'];
const ITEM_FIELD_KEY = /^items\[\d+\]\.(serviceId|description|quantity|unit|rate|needsReview)$/;

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

/** Local date as yyyy-MM-dd (toISOString would be UTC and can be the previous day). */
function toIsoDate(date: Date): string {
    const pad = (value: number) => String(value).padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

function fromIsoDate(value: string): Date {
    const [year, month, day] = value.split('-').map(Number);
    return new Date(year, month - 1, day);
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
 * Dumb form for a work day (create when day is null, edit otherwise): date, title, item cards, note.
 * Choosing a service fills description, unit and price (site price → default → „цена?“).
 * Crew (canEditPrices false) sees the price read-only and sends null: the server resolves it.
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
    /** Owner: may type or change prices. Crew: prices are read-only and sent as null. */
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
    protected readonly disabledDate = (current: Date): boolean => toIsoDate(current) > toIsoDate(new Date());

    private readonly fb = inject(NonNullableFormBuilder);
    private readonly destroyRef = inject(DestroyRef);
    private readonly injector = inject(Injector);
    private readonly serviceSelects = viewChildren('serviceSelect', { read: NzSelectComponent });

    protected readonly form = this.fb.group({
        date: this.fb.control<Date | null>(null, Validators.required),
        title: ['', Validators.maxLength(WORK_DAY_LIMITS.title)],
        note: ['', Validators.maxLength(WORK_DAY_LIMITS.note)],
        items: this.fb.array<ItemForm>([]),
    });

    protected readonly confirmingDelete = signal(false);
    /** Raw item values (disabled Crew prices included), kept in a signal for the live total. */
    private readonly itemValues = signal<{ quantity: number | null; rate: number | null }[]>([]);
    protected readonly summary = computed(() => previewTotal(this.itemValues()));

    protected readonly serviceOptions = computed(() =>
        this.services().map((service) => ({
            value: service.id,
            label: `${service.name} · ${UNIT_LABELS[service.unit]}`,
        })),
    );

    protected readonly otherErrors = computed(() =>
        Object.entries(this.fieldErrors())
            .filter(([key]) => !DAY_FIELDS.includes(key) && !ITEM_FIELD_KEY.test(key))
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

    protected get items(): FormArray<ItemForm> {
        return this.form.controls.items;
    }

    protected serverError(key: string): string | null {
        return this.fieldErrors()[key] ?? null;
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
            // For Crew the server keeps the stored price of an existing item, so the form shows that one.
            if (!this.canEditPrices() && group.controls.id.value) {
                return;
            }
            group.controls.rate.setValue(resolveRate(serviceId || null, this.sitePrices(), this.services()));
        });
        return group;
    }

    private syncItemValues(): void {
        this.itemValues.set(this.items.getRawValue().map(({ quantity, rate }) => ({ quantity, rate })));
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
        this.syncItemValues();
        this.confirmingDelete.set(false);
    }
}
