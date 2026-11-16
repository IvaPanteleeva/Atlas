import { Location } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { Observable, of, throwError } from 'rxjs';
import { routes } from '../../../app.routes';
import { APP_ICONS } from '../../shared/icons';
import { AuthStore } from '../+state/auth.store';
import { Me, Role } from '../auth.model';
import { AuthService } from '../auth.service';
import { SitesService } from '../../sites/sites.service';

class FakeAuthService {
    loginResult: Observable<Me> = of({ id: '1', userName: 'iva', role: 'Owner', companyId: null });

    me(): Observable<Me> {
        return throwError(() => new HttpErrorResponse({ status: 401 }));
    }

    login(): Observable<Me> {
        return this.loginResult;
    }
}

describe('LoginPageComponent', () => {
    let fake: FakeAuthService;

    beforeEach(async () => {
        fake = new FakeAuthService();
        TestBed.configureTestingModule({
            providers: [
                provideRouter(routes, withComponentInputBinding()),
                provideNzIcons(APP_ICONS),
                { provide: AuthService, useValue: fake },
                // /sites is the start page; it loads the list.
                { provide: SitesService, useValue: { list: () => of([]) } },
            ],
        });
        await TestBed.inject(AuthStore).loadMe();
    });

    const path = () => TestBed.inject(Location).path();

    async function signIn(url: string, role: Role = 'Owner') {
        fake.loginResult = of({ id: '1', userName: 'iva', role, companyId: null });
        const harness = await RouterTestingHarness.create();
        await harness.navigateByUrl(url);
        const element = harness.fixture.nativeElement as HTMLElement;
        for (const [selector, value] of [
            ['input[autocomplete="username"]', 'iva'],
            ['input[autocomplete="current-password"]', 'secret-pass'],
        ]) {
            const input = element.querySelector<HTMLInputElement>(selector)!;
            input.value = value;
            input.dispatchEvent(new Event('input'));
        }
        element.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
        await harness.fixture.whenStable();
        return { harness, element };
    }

    it('shows the login form in its own layout, without the shell', async () => {
        const harness = await RouterTestingHarness.create();
        await harness.navigateByUrl('/login');
        const element = harness.fixture.nativeElement as HTMLElement;

        expect(element.querySelector('atlas-login-form')).not.toBeNull();
        expect(element.querySelector('atlas-shell')).toBeNull();
        expect(element.textContent).toContain('АТЛАС');
    });

    it('goes to returnUrl after a successful login', async () => {
        await signIn('/login?returnUrl=%2Fpeople');
        expect(path()).toBe('/people');
    });

    it('goes home by role without returnUrl', async () => {
        await signIn('/login', 'ClientViewer');
        expect(path()).toBe('/portal');
    });

    it('ignores an external returnUrl', async () => {
        const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl');

        await signIn('/login?returnUrl=%2F%2Fevil.example.com', 'Owner');

        expect(navigate).toHaveBeenLastCalledWith('/sites');
        expect(path()).toBe('/sites');
    });

    it('shows the API error and stays on /login', async () => {
        fake.loginResult = throwError(
            () => new HttpErrorResponse({ status: 401, error: { title: 'Грешно име или парола' } }),
        );
        const harness = await RouterTestingHarness.create();
        await harness.navigateByUrl('/login');
        const element = harness.fixture.nativeElement as HTMLElement;
        for (const [selector, value] of [
            ['input[autocomplete="username"]', 'iva'],
            ['input[autocomplete="current-password"]', 'wrong-pass'],
        ]) {
            const input = element.querySelector<HTMLInputElement>(selector)!;
            input.value = value;
            input.dispatchEvent(new Event('input'));
        }
        element.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
        await harness.fixture.whenStable();

        expect(path()).toBe('/login');
        expect(element.querySelector('nz-alert')?.textContent).toContain('Грешно име или парола');
    });
});
