import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject, Injector } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthStore } from './+state/auth.store';

/** 401 from these is an expected answer (wrong password, not signed in yet), not an expired session. */
const IGNORED_URLS = ['/api/auth/login', '/api/auth/me'];

function isSessionExpired(error: unknown, url: string): boolean {
    const path = url.split('?')[0];
    return (
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        path.startsWith('/api/') &&
        !IGNORED_URLS.includes(path)
    );
}

/** On 401 from the API the session is gone: forget the user and go to /login, then rethrow. */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
    // AuthStore -> AuthService -> HttpClient -> this interceptor: resolve the store lazily to avoid a DI cycle.
    const injector = inject(Injector);
    return next(request).pipe(
        catchError((error: unknown) => {
            if (isSessionExpired(error, request.url)) {
                const router = injector.get(Router);
                injector.get(AuthStore).clear();
                const returnUrl = router.url;
                if (!returnUrl.startsWith('/login')) {
                    void router.navigateByUrl(router.createUrlTree(['/login'], { queryParams: { returnUrl } }));
                }
            }
            return throwError(() => error);
        }),
    );
};
