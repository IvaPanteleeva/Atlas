import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { Observable, of, throwError } from 'rxjs';
import { App } from './app';
import { routes } from './app.routes';
import { AuthStore } from './libs/auth/+state/auth.store';
import { Me } from './libs/auth/auth.model';
import { AuthService } from './libs/auth/auth.service';
import { APP_ICONS } from './libs/shared/icons';

describe('App', () => {
    let meResult: Observable<Me>;

    beforeEach(async () => {
        meResult = throwError(() => new HttpErrorResponse({ status: 401 }));
        await TestBed.configureTestingModule({
            imports: [App],
            providers: [
                provideRouter(routes, withComponentInputBinding()),
                provideNzIcons(APP_ICONS),
                { provide: AuthService, useValue: { me: () => meResult } },
            ],
        }).compileComponents();
    });

    async function start() {
        await TestBed.inject(AuthStore).loadMe();
        const fixture = TestBed.createComponent(App);
        await TestBed.inject(Router).navigateByUrl('/');
        await fixture.whenStable();
        return fixture.nativeElement as HTMLElement;
    }

    it('should create the app', () => {
        const fixture = TestBed.createComponent(App);
        expect(fixture.componentInstance).toBeTruthy();
    });

    it('renders the shell for a signed-in user', async () => {
        meResult = of({ id: '1', userName: 'iva', role: 'Owner', companyId: null });

        const element = await start();

        expect(element.querySelector('atlas-shell')).not.toBeNull();
        expect(element.querySelector('atlas-login-page')).toBeNull();
    });

    it('renders the login page without the shell for an anonymous user', async () => {
        const element = await start();

        expect(element.querySelector('atlas-login-page')).not.toBeNull();
        expect(element.querySelector('atlas-shell')).toBeNull();
    });
});
