import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
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
import { Role } from '../../auth/auth.model';
import { Company } from '../../companies/company.model';
import { FieldErrors } from '../../shared/api-errors';
import { CreateUserRequest, PASSWORD_MIN_LENGTH, ROLE_OPTIONS } from '../user.model';

const FORM_FIELDS = ['userName', 'password', 'role', 'companyId'];
const DEFAULT_ROLE: Role = 'Manager';

function notBlank(control: AbstractControl<string>): ValidationErrors | null {
    return control.value.trim() ? null : { required: true };
}

/** Dumb form for a new login profile. The company is asked for (and required) only for „Възложител“. */
@Component({
    selector: 'atlas-user-form',
    imports: [
        ReactiveFormsModule,
        NzAlertModule,
        NzButtonModule,
        NzFormModule,
        NzIconModule,
        NzInputModule,
        NzSelectModule,
    ],
    templateUrl: './user-form.component.html',
    styleUrl: './user-form.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserFormComponent {
    readonly companies = input<Company[]>([]);
    readonly saving = input(false);
    readonly error = input<string | null>(null);
    readonly fieldErrors = input<FieldErrors>({});

    readonly save = output<CreateUserRequest>();
    readonly cancel = output<void>();

    protected readonly roles = ROLE_OPTIONS;
    protected readonly passwordVisible = signal(false);

    protected readonly form = inject(NonNullableFormBuilder).group({
        userName: ['', notBlank],
        password: ['', [Validators.required, Validators.minLength(PASSWORD_MIN_LENGTH)]],
        role: [DEFAULT_ROLE],
        companyId: [null as string | null],
    });

    private readonly role = toSignal(this.form.controls.role.valueChanges, { initialValue: DEFAULT_ROLE });
    protected readonly needsCompany = computed(() => this.role() === 'ClientViewer');

    protected readonly otherErrors = computed(() =>
        Object.entries(this.fieldErrors())
            .filter(([key]) => !FORM_FIELDS.includes(key))
            .map(([, message]) => message),
    );

    constructor() {
        const company = this.form.controls.companyId;
        this.form.controls.role.valueChanges.pipe(takeUntilDestroyed()).subscribe((role) => {
            company.setValidators(role === 'ClientViewer' ? Validators.required : null);
            company.updateValueAndValidity({ emitEvent: false });
        });
    }

    protected serverError(field: string): string | null {
        return this.fieldErrors()[field] ?? null;
    }

    protected togglePassword(): void {
        this.passwordVisible.update((visible) => !visible);
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
            userName: value.userName.trim(),
            password: value.password,
            role: value.role,
            companyId: value.role === 'ClientViewer' ? value.companyId : null,
        });
    }
}
