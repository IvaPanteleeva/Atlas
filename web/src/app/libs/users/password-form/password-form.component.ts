import { ChangeDetectionStrategy, Component, effect, inject, input, output, signal, untracked } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { FieldErrors } from '../../shared/api-errors';
import { PASSWORD_MIN_LENGTH } from '../user.model';

/** Dumb form with one field: a new password for a login profile. */
@Component({
    selector: 'atlas-password-form',
    imports: [ReactiveFormsModule, NzAlertModule, NzButtonModule, NzFormModule, NzIconModule, NzInputModule],
    templateUrl: './password-form.component.html',
    styleUrl: './password-form.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PasswordFormComponent {
    /** Whose password (only for the reset when another profile is chosen). */
    readonly userId = input<string | null>(null);
    readonly saving = input(false);
    readonly error = input<string | null>(null);
    readonly fieldErrors = input<FieldErrors>({});

    readonly save = output<string>();
    readonly cancel = output<void>();

    protected readonly minLength = PASSWORD_MIN_LENGTH;
    protected readonly passwordVisible = signal(false);
    protected readonly form = inject(NonNullableFormBuilder).group({
        password: ['', [Validators.required, Validators.minLength(PASSWORD_MIN_LENGTH)]],
    });

    constructor() {
        effect(() => {
            this.userId();
            untracked(() => {
                this.form.reset({ password: '' }, { emitEvent: false });
                this.passwordVisible.set(false);
            });
        });
    }

    protected togglePassword(): void {
        this.passwordVisible.update((visible) => !visible);
    }

    protected submit(): void {
        if (this.saving()) {
            return;
        }
        const control = this.form.controls.password;
        if (control.invalid) {
            control.markAsDirty();
            control.markAsTouched();
            control.updateValueAndValidity();
            return;
        }
        this.save.emit(control.value);
    }
}
