import { HttpErrorResponse } from '@angular/common/http';
import { EnvironmentInjector } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
    ActivatedRouteSnapshot,
    PartialMatchRouteSnapshot,
    provideRouter,
    Router,
    RouterStateSnapshot,
    UrlTree,
} from '@angular/router';
import { Observable, of, throwError } from 'rxjs';
import { AuthStore } from './+state/auth.store';
import { authGuard, clientGuard, guestGuard, homeUrlFor, ownerGuard, staffGuard } from './auth.guards';
import { Me, Role } from './auth.model';
import { AuthService } from './auth.service';

class FakeAuthService {
    meResult: Observable<Me> = throwError(() => new HttpErrorResponse({ status: 401 }));

    me(): Observable<Me> {
        return this.meResult;
    }
}

describe('auth guards', () => {
    let fake: FakeAuthService;

    beforeEach(() => {
        fake = new FakeAuthService();
        TestBed.configureTestingModule({ providers: [provideRouter([]), { provide: AuthService, useValue: fake }] });
    });

    async function signInAs(role: Role | null) {
        if (role) {
            fake.meResult = of({ id: '1', userName: 'u', role, companyId: null });
        }
        await TestBed.inject(AuthStore).loadMe();
    }

    function run<T>(guard: () => T): T {
        return TestBed.inject(EnvironmentInjector).runInContext(guard);
    }

    const route = {} as ActivatedRouteSnapshot;
    const matchSnapshot = {} as PartialMatchRouteSnapshot;
    const state = (url: string) => ({ url }) as RouterStateSnapshot;
    const serialize = (result: unknown) => TestBed.inject(Router).serializeUrl(result as UrlTree);

    it('homeUrlFor sends ClientViewer to /portal and staff to /sites', () => {
        expect(homeUrlFor('ClientViewer')).toBe('/portal');
        expect(homeUrlFor('Owner')).toBe('/sites');
        expect(homeUrlFor('Manager')).toBe('/sites');
        expect(homeUrlFor(null)).toBe('/sites');
    });

    describe('authGuard', () => {
        it('redirects an anonymous user to /login with returnUrl', async () => {
            await signInAs(null);

            const result = run(() => authGuard(route, state('/people?x=1')));

            expect(result).toBeInstanceOf(UrlTree);
            expect(serialize(result)).toBe('/login?returnUrl=%2Fpeople%3Fx%3D1');
        });

        it('lets a signed-in user through', async () => {
            await signInAs('Manager');

            expect(run(() => authGuard(route, state('/sites')))).toBe(true);
        });
    });

    describe('guestGuard', () => {
        it('lets an anonymous user open /login', async () => {
            await signInAs(null);

            expect(run(() => guestGuard({}, [], matchSnapshot))).toBe(true);
        });

        it('sends a signed-in Owner from /login to /sites', async () => {
            await signInAs('Owner');

            expect(serialize(run(() => guestGuard({}, [], matchSnapshot)))).toBe('/sites');
        });

        it('sends a signed-in ClientViewer from /login to /portal', async () => {
            await signInAs('ClientViewer');

            expect(serialize(run(() => guestGuard({}, [], matchSnapshot)))).toBe('/portal');
        });
    });

    describe('staffGuard', () => {
        it('sends ClientViewer from a staff route to /portal', async () => {
            await signInAs('ClientViewer');

            expect(serialize(run(() => staffGuard(route, state('/sites'))))).toBe('/portal');
        });

        it('lets Owner and Manager through', async () => {
            await signInAs('Owner');
            expect(run(() => staffGuard(route, state('/sites')))).toBe(true);
        });
    });

    describe('clientGuard', () => {
        it('sends Owner from /portal to /sites', async () => {
            await signInAs('Owner');

            expect(serialize(run(() => clientGuard(route, state('/portal'))))).toBe('/sites');
        });

        it('lets ClientViewer through', async () => {
            await signInAs('ClientViewer');

            expect(run(() => clientGuard(route, state('/portal')))).toBe(true);
        });
    });

    describe('ownerGuard', () => {
        it('lets Owner through', async () => {
            await signInAs('Owner');

            expect(run(() => ownerGuard(route, state('/companies')))).toBe(true);
        });

        it('sends Manager to /sites', async () => {
            await signInAs('Manager');

            expect(serialize(run(() => ownerGuard(route, state('/companies'))))).toBe('/sites');
        });

        it('sends ClientViewer to /portal', async () => {
            await signInAs('ClientViewer');

            expect(serialize(run(() => ownerGuard(route, state('/companies'))))).toBe('/portal');
        });
    });
});
