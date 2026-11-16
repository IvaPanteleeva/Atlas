import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';
import { LoginRequest, Me } from '../auth.model';
import { AuthService } from '../auth.service';
import { AuthStore } from './auth.store';

const owner: Me = { id: '1', userName: 'iva', role: 'Owner', companyId: null };
const client: Me = { id: '2', userName: 'jeni', role: 'ClientViewer', companyId: 'c1' };

function httpError(status: number, error: unknown = null) {
    return throwError(() => new HttpErrorResponse({ status, error }));
}

/** Hand-written fake: each test sets what the next call returns. */
class FakeAuthService {
    meResult: Observable<Me> = of(owner);
    loginResult: Observable<Me> = of(owner);
    logoutCalls = 0;
    lastLogin: LoginRequest | null = null;

    me(): Observable<Me> {
        return this.meResult;
    }

    login(request: LoginRequest): Observable<Me> {
        this.lastLogin = request;
        return this.loginResult;
    }

    logout(): Observable<void> {
        this.logoutCalls++;
        return of(undefined);
    }
}

describe('AuthStore', () => {
    let fake: FakeAuthService;
    let store: InstanceType<typeof AuthStore>;

    beforeEach(() => {
        fake = new FakeAuthService();
        TestBed.configureTestingModule({ providers: [{ provide: AuthService, useValue: fake }] });
        store = TestBed.inject(AuthStore);
    });

    it('starts as unknown', () => {
        expect(store.status()).toBe('unknown');
        expect(store.isAuthenticated()).toBe(false);
    });

    it('loadMe with 200 marks the user as authenticated', async () => {
        await store.loadMe();

        expect(store.status()).toBe('authenticated');
        expect(store.user()).toEqual(owner);
        expect(store.isAuthenticated()).toBe(true);
        expect(store.role()).toBe('Owner');
        expect(store.isStaff()).toBe(true);
        expect(store.isOwner()).toBe(true);
        expect(store.isClient()).toBe(false);
    });

    it('loadMe with 401 marks the user as anonymous', async () => {
        fake.meResult = httpError(401);

        await store.loadMe();

        expect(store.status()).toBe('anonymous');
        expect(store.user()).toBeNull();
    });

    it('loadMe with another error does not block startup and is anonymous', async () => {
        fake.meResult = httpError(500);

        await expect(store.loadMe()).resolves.toBeUndefined();
        expect(store.status()).toBe('anonymous');
    });

    it('login success stores the user and returns true', async () => {
        fake.loginResult = of(client);

        const ok = await store.login({ userName: 'jeni', password: 'password1' });

        expect(ok).toBe(true);
        expect(fake.lastLogin).toEqual({ userName: 'jeni', password: 'password1' });
        expect(store.status()).toBe('authenticated');
        expect(store.isClient()).toBe(true);
        expect(store.isStaff()).toBe(false);
        expect(store.isOwner()).toBe(false);
        expect(store.loginPending()).toBe(false);
        expect(store.loginError()).toBeNull();
    });

    it('login 401 sets loginError to the ProblemDetails title', async () => {
        fake.loginResult = httpError(401, { title: 'Грешно име или парола', status: 401 });

        const ok = await store.login({ userName: 'iva', password: 'wrong-pass' });

        expect(ok).toBe(false);
        expect(store.loginError()).toBe('Грешно име или парола');
        expect(store.loginPending()).toBe(false);
        expect(store.isAuthenticated()).toBe(false);
    });

    it('login error without a title falls back to a generic message', async () => {
        fake.loginResult = httpError(500);

        await store.login({ userName: 'iva', password: 'whatever1' });

        expect(store.loginError()).toBe('Грешка при вход');
    });

    it('a new login attempt clears the previous error', async () => {
        fake.loginResult = httpError(401, { title: 'Грешно име или парола' });
        await store.login({ userName: 'iva', password: 'wrong-pass' });
        fake.loginResult = of(owner);

        await store.login({ userName: 'iva', password: 'right-pass' });

        expect(store.loginError()).toBeNull();
    });

    it('logout calls the API and makes the user anonymous', async () => {
        await store.loadMe();

        await store.logout();

        expect(fake.logoutCalls).toBe(1);
        expect(store.status()).toBe('anonymous');
        expect(store.user()).toBeNull();
    });

    it('logout is anonymous even when the API call fails', async () => {
        await store.loadMe();
        fake.logout = () => httpError(500) as Observable<void>;

        await store.logout();

        expect(store.status()).toBe('anonymous');
    });

    it('clear makes the user anonymous without calling the API', async () => {
        await store.loadMe();

        store.clear();

        expect(store.status()).toBe('anonymous');
        expect(fake.logoutCalls).toBe(0);
    });
});
