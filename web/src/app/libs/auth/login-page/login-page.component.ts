import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { Router } from '@angular/router';
import { AuthStore } from '../+state/auth.store';
import { homeUrlFor } from '../auth.guards';
import { LoginRequest } from '../auth.model';
import { LoginFormComponent } from '../login-form/login-form.component';

/** Only internal paths: "/people" yes; "//evil.com", "https://..." or "people" no. */
function isSafeReturnUrl(url: string | undefined): url is string {
    return !!url && url.startsWith('/') && !url.startsWith('//') && !url.startsWith('/\\');
}

/** Smart login page (route component, outside the shell). */
@Component({
    selector: 'atlas-login-page',
    imports: [LoginFormComponent],
    templateUrl: './login-page.component.html',
    styleUrl: './login-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPageComponent {
    /** From the query string (?returnUrl=...), set by authGuard and the 401 interceptor. */
    readonly returnUrl = input<string>();

    protected readonly store = inject(AuthStore);
    private readonly router = inject(Router);

    protected async signIn(request: LoginRequest): Promise<void> {
        if (!(await this.store.login(request))) {
            return;
        }
        const returnUrl = this.returnUrl();
        await this.router.navigateByUrl(isSafeReturnUrl(returnUrl) ? returnUrl : homeUrlFor(this.store.role()));
    }
}
