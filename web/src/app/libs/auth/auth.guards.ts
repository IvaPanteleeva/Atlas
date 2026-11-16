import { inject } from '@angular/core';
import { CanActivateFn, CanMatchFn, Router } from '@angular/router';
import { AuthStore } from './+state/auth.store';
import { Role } from './auth.model';

/** Start page for a role: the portal for the client, the sites list for the team. */
export function homeUrlFor(role: Role | null): string {
    return role === 'ClientViewer' ? '/portal' : '/sites';
}

/** Only signed-in users; others go to /login and come back after signing in. */
export const authGuard: CanActivateFn = (_route, state) => {
    if (inject(AuthStore).isAuthenticated()) {
        return true;
    }
    return inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

/** /login is only for anonymous users; a signed-in user goes to their start page. */
export const guestGuard: CanMatchFn = () => {
    const store = inject(AuthStore);
    return store.isAuthenticated() ? inject(Router).parseUrl(homeUrlFor(store.role())) : true;
};

/** Internal pages are for Owner and Crew; the client goes to the portal. */
export const staffGuard: CanActivateFn = () => {
    return inject(AuthStore).isStaff() ? true : inject(Router).parseUrl('/portal');
};

/** The portal is for the client; the team goes to the sites list. */
export const clientGuard: CanActivateFn = () => {
    return inject(AuthStore).isClient() ? true : inject(Router).parseUrl('/sites');
};

/** Owner-only pages (e.g. Фирми); everyone else goes to their start page. */
export const ownerGuard: CanActivateFn = () => {
    const store = inject(AuthStore);
    return store.isOwner() ? true : inject(Router).parseUrl(homeUrlFor(store.role()));
};
