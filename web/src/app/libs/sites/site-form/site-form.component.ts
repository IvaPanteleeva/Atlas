import {
    ChangeDetectionStrategy,
    Component,
    computed,
    effect,
    inject,
    input,
    output,
    signal,
    untracked,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
    AbstractControl,
    NonNullableFormBuilder,
    ReactiveFormsModule,
    ValidationErrors,
    Validators,
} from '@angular/forms';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { Company, COMPANY_NAME_MAX_LENGTH } from '../../companies/company.model';
import {
    FieldErrors,
    SaveSiteRequest,
    Site,
    SITE_MAX_LENGTHS,
    SITE_STATUS_LABELS,
    SITE_STATUSES,
    SiteStatus,
} from '../site.model';

/** Select value for "Без фирма" (nz-select shows null as an empty field, so an empty string is used). */
const NO_COMPANY = '';
/** Select value of the "+ Нова фирма" option; it never stays selected. */
const NEW_COMPANY = '__new__';

/** Fields that have a place in the form; server errors for other keys go to the alert. */
const FORM_FIELDS = ['name', 'status', 'companyId', 'endCustomer', 'address', 'mapUrl', 'note'];

function notBlank(control: AbstractControl<string>): ValidationErrors | null {
    return control.value.trim() ? null : { required: true };
}

function httpUrl(control: AbstractControl<string>): ValidationErrors | null {
    const value = control.value.trim();
    return !value || /^https?:\/\//i.test(value) ? null : { url: true };
}

function orNull(value: string): string | null {
    const trimmed = value.trim();
    return trimmed ? trimmed : null;
}

/** Dumb form for a site (create when site is null, edit otherwise). The page owns saving and the modal. */
@Component({
    selector: 'atlas-site-form',
    imports: [
        ReactiveFormsModule,
        NzAlertModule,
        NzButtonModule,
        NzFormModule,
        NzIconModule,
        NzInputModule,
        NzSelectModule,
    ],
    templateUrl: './site-form.component.html',
    styleUrl: './site-form.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SiteFormComponent {
    readonly site = input<Site | null>(null);
    readonly companies = input<Company[]>([]);
    readonly canCreateCompany = input(false);
    readonly canDelete = input(false);
    readonly saving = input(false);
    readonly error = input<string | null>(null);
    readonly fieldErrors = input<FieldErrors>({});
    /** Set by the page after "+ Нова фирма" succeeds; the form selects it. */
    readonly createdCompany = input<Company | null>(null);
    readonly companySaving = input(false);
    readonly companyError = input<string | null>(null);

    readonly save = output<SaveSiteRequest>();
    readonly createCompany = output<string>();
    readonly delete = output<void>();
    readonly cancel = output<void>();

    protected readonly maxLengths = SITE_MAX_LENGTHS;
    protected readonly companyNameMaxLength = COMPANY_NAME_MAX_LENGTH;
    protected readonly statuses = SITE_STATUSES.map((value) => ({ value, label: SITE_STATUS_LABELS[value] }));
    protected readonly noCompany = NO_COMPANY;
    protected readonly newCompany = NEW_COMPANY;

    protected readonly form = inject(NonNullableFormBuilder).group({
        name: ['', [notBlank, Validators.maxLength(SITE_MAX_LENGTHS.name)]],
        status: ['Lead' as SiteStatus],
        companyId: [NO_COMPANY],
        endCustomer: ['', Validators.maxLength(SITE_MAX_LENGTHS.endCustomer)],
        address: ['', Validators.maxLength(SITE_MAX_LENGTHS.address)],
        mapUrl: ['', [httpUrl, Validators.maxLength(SITE_MAX_LENGTHS.mapUrl)]],
        note: ['', Validators.maxLength(SITE_MAX_LENGTHS.note)],
    });

    protected readonly addingCompany = signal(false);
    protected readonly newCompanyName = signal('');
    protected readonly confirmingDelete = signal(false);

    protected readonly canAddCompany = computed(() => !this.companySaving() && !!this.newCompanyName().trim());
    /** Server messages for keys the form has no field for. */
    protected readonly otherErrors = computed(() =>
        Object.entries(this.fieldErrors())
            .filter(([key]) => !FORM_FIELDS.includes(key))
            .map(([, message]) => message),
    );

    /** The last real company choice, restored when "+ Нова фирма" is picked. */
    private lastCompanyId = NO_COMPANY;

    constructor() {
        effect(() => {
            const site = this.site();
            untracked(() => this.reset(site));
        });

        effect(() => {
            const company = this.createdCompany();
            if (company) {
                untracked(() => {
                    this.form.controls.companyId.setValue(company.id);
                    this.closeNewCompany();
                });
            }
        });

        this.form.controls.companyId.valueChanges.pipe(takeUntilDestroyed()).subscribe((value) => {
            if (value === NEW_COMPANY) {
                this.form.controls.companyId.setValue(this.lastCompanyId, { emitEvent: false });
                this.addingCompany.set(true);
            } else {
                this.lastCompanyId = value;
            }
        });
    }

    protected serverError(field: string): string | null {
        return this.fieldErrors()[field] ?? null;
    }

    protected addCompany(): void {
        if (this.canAddCompany()) {
            this.createCompany.emit(this.newCompanyName().trim());
        }
    }

    protected closeNewCompany(): void {
        this.addingCompany.set(false);
        this.newCompanyName.set('');
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
            for (const control of Object.values(this.form.controls)) {
                control.markAsDirty();
                control.markAsTouched();
                control.updateValueAndValidity({ emitEvent: false });
            }
            return;
        }
        const value = this.form.getRawValue();
        this.save.emit({
            name: value.name.trim(),
            status: value.status,
            companyId: orNull(value.companyId),
            endCustomer: orNull(value.endCustomer),
            address: orNull(value.address),
            mapUrl: orNull(value.mapUrl),
            note: orNull(value.note),
        });
    }

    private reset(site: Site | null): void {
        this.lastCompanyId = site?.companyId ?? NO_COMPANY;
        this.form.reset(
            {
                name: site?.name ?? '',
                status: site?.status ?? 'Lead',
                companyId: this.lastCompanyId,
                endCustomer: site?.endCustomer ?? '',
                address: site?.address ?? '',
                mapUrl: site?.mapUrl ?? '',
                note: site?.note ?? '',
            },
            { emitEvent: false },
        );
        this.closeNewCompany();
        this.confirmingDelete.set(false);
    }
}
