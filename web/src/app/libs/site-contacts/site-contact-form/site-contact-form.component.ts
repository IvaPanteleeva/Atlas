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
import { NzInputModule } from 'ng-zorro-antd/input';
import { FieldErrors } from '../../shared/api-errors';
import { SaveSiteContactRequest, SITE_CONTACT_LIMITS, SiteContact } from '../site-contact.model';

const FORM_FIELDS = ['name', 'role', 'phone', 'note'];

function notBlank(control: AbstractControl<string>): ValidationErrors | null {
    return control.value.trim() ? null : { required: true };
}

function orNull(value: string): string | null {
    const trimmed = value.trim();
    return trimmed ? trimmed : null;
}

/** Dumb form for a site contact (create when contact is null, edit otherwise). The page owns saving and the sheet. */
@Component({
    selector: 'atlas-site-contact-form',
    imports: [ReactiveFormsModule, NzAlertModule, NzButtonModule, NzFormModule, NzInputModule],
    templateUrl: './site-contact-form.component.html',
    styleUrl: './site-contact-form.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SiteContactFormComponent {
    readonly contact = input<SiteContact | null>(null);
    /** Owner only. */
    readonly canDelete = input(false);
    readonly saving = input(false);
    readonly error = input<string | null>(null);
    readonly fieldErrors = input<FieldErrors>({});

    readonly save = output<SaveSiteContactRequest>();
    readonly delete = output<void>();
    readonly cancel = output<void>();

    protected readonly limits = SITE_CONTACT_LIMITS;

    protected readonly form = inject(NonNullableFormBuilder).group({
        name: ['', [notBlank, Validators.maxLength(SITE_CONTACT_LIMITS.name)]],
        role: ['', Validators.maxLength(SITE_CONTACT_LIMITS.role)],
        phone: ['', Validators.maxLength(SITE_CONTACT_LIMITS.phone)],
        note: ['', Validators.maxLength(SITE_CONTACT_LIMITS.note)],
    });

    protected readonly confirmingDelete = signal(false);

    protected readonly otherErrors = computed(() =>
        Object.entries(this.fieldErrors())
            .filter(([key]) => !FORM_FIELDS.includes(key))
            .map(([, message]) => message),
    );

    constructor() {
        effect(() => {
            const contact = this.contact();
            untracked(() => {
                this.form.reset(
                    {
                        name: contact?.name ?? '',
                        role: contact?.role ?? '',
                        phone: contact?.phone ?? '',
                        note: contact?.note ?? '',
                    },
                    { emitEvent: false },
                );
                this.confirmingDelete.set(false);
            });
        });
    }

    protected serverError(field: string): string | null {
        return this.fieldErrors()[field] ?? null;
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
                control.updateValueAndValidity();
            }
            return;
        }
        const value = this.form.getRawValue();
        this.save.emit({
            name: value.name.trim(),
            role: orNull(value.role),
            phone: orNull(value.phone),
            note: orNull(value.note),
        });
    }
}
