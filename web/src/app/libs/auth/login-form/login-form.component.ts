import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { LoginRequest } from '../auth.model';

/** Dumb login form: validates the fields and emits the request; the page talks to the store. */
@Component({
    selector: 'atlas-login-form',
    imports: [ReactiveFormsModule, NzFormModule, NzInputModule, NzButtonModule, NzAlertModule, NzIconModule],
    templateUrl: './login-form.component.html',
    styleUrl: './login-form.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginFormComponent {
    readonly pending = input(false);
    readonly error = input<string | null>(null);
    readonly submitted = output<LoginRequest>();

    protected readonly passwordVisible = signal(false);

    protected readonly form = inject(NonNullableFormBuilder).group({
        userName: ['', Validators.required],
        password: ['', Validators.required],
    });

    protected togglePassword(): void {
        this.passwordVisible.update((visible) => !visible);
    }

    protected submit(): void {
        if (this.pending()) {
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
        const { userName, password } = this.form.getRawValue();
        // Phones often add a trailing space after the name; the password is sent as typed.
        this.submitted.emit({ userName: userName.trim(), password });
    }
}
