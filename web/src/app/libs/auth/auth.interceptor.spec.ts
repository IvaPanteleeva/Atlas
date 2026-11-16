import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { AuthStore } from './+state/auth.store';
import { authInterceptor } from './auth.interceptor';
import { Me } from './auth.model';
import { AuthService } from './auth.service';

const owner: Me = { id: '1', userName: 'iva', role: 'Owner', companyId: null };

describe('authInterceptor', () => {
    let http: HttpClient;
    let controller: HttpTestingController;
    let store: InstanceType<typeof AuthStore>;
    let navigate: ReturnType<typeof vi.spyOn>;

    beforeEach(async () => {
        TestBed.configureTestingModule({
            providers: [
                provideRouter([]),
                provideHttpClient(withInterceptors([authInterceptor])),
                provideHttpClientTesting(),
                { provide: AuthService, useValue: { me: () => of(owner) } },
            ],
        });
        http = TestBed.inject(HttpClient);
        controller = TestBed.inject(HttpTestingController);
        store = TestBed.inject(AuthStore);
        await store.loadMe();
        const router = TestBed.inject(Router);
        vi.spyOn(router, 'url', 'get').mockReturnValue('/people');
        navigate = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    });

    afterEach(() => controller.verify());

    function request(url: string) {
        let error: unknown = null;
        http.get(url).subscribe({ error: (e) => (error = e) });
        controller.expectOne(url).flush({ title: 'x' }, { status: 401, statusText: 'Unauthorized' });
        return error as { status: number } | null;
    }

    it('on 401 from /api/sites clears the user and goes to /login with returnUrl', () => {
        const error = request('/api/sites');

        expect(error?.status).toBe(401);
        expect(store.status()).toBe('anonymous');
        expect(navigate).toHaveBeenCalledTimes(1);
        const target = navigate.mock.calls[0][0];
        expect(TestBed.inject(Router).serializeUrl(target)).toBe('/login?returnUrl=%2Fpeople');
    });

    it('on 401 from /api/auth/login does not navigate', () => {
        const error = request('/api/auth/login');

        expect(error?.status).toBe(401);
        expect(navigate).not.toHaveBeenCalled();
        expect(store.status()).toBe('authenticated');
    });

    it('on 401 from /api/auth/me does not navigate', () => {
        request('/api/auth/me');

        expect(navigate).not.toHaveBeenCalled();
    });

    it('ignores 401 from URLs outside /api', () => {
        request('/other/thing');

        expect(navigate).not.toHaveBeenCalled();
    });

    it('passes other errors through without navigation', () => {
        let status = 0;
        http.get('/api/sites').subscribe({ error: (e) => (status = e.status) });
        controller.expectOne('/api/sites').flush(null, { status: 500, statusText: 'Server Error' });

        expect(status).toBe(500);
        expect(navigate).not.toHaveBeenCalled();
    });
});
