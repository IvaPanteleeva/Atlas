import { HttpErrorResponse } from '@angular/common/http';
import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { LoginRequest, Me } from '../auth.model';
import { AuthService } from '../auth.service';

export type AuthStatus = 'unknown' | 'authenticated' | 'anonymous';

interface AuthState {
    user: Me | null;
    status: AuthStatus;
    loginPending: boolean;
    loginError: string | null;
}

const initialState: AuthState = {
    user: null,
    status: 'unknown',
    loginPending: false,
    loginError: null,
};

const LOGIN_FALLBACK_ERROR = 'Грешка при вход';

/** Text for the login alert: the ProblemDetails title from the API, or a generic message. */
function loginErrorText(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
        const title = (error.error as { title?: unknown } | null)?.title;
        if (typeof title === 'string' && title.trim()) {
            return title;
        }
    }
    return LOGIN_FALLBACK_ERROR;
}

export const AuthStore = signalStore(
    { providedIn: 'root' },
    withState(initialState),
    withComputed(({ user, status }) => ({
        isAuthenticated: computed(() => status() === 'authenticated'),
        role: computed(() => user()?.role ?? null),
        /** Owner or Manager: everything in the internal app except deleting and login profiles. */
        isStaff: computed(() => user()?.role === 'Owner' || user()?.role === 'Manager'),
        /** Owner only: deleting and login profiles. */
        isOwner: computed(() => user()?.role === 'Owner'),
        isClient: computed(() => user()?.role === 'ClientViewer'),
    })),
    withMethods((store, service = inject(AuthService)) => ({
        /** Asks the API who is signed in. Never rejects: any error means anonymous, so startup is not blocked. */
        async loadMe(): Promise<void> {
            try {
                const user = await firstValueFrom(service.me());
                patchState(store, { user, status: 'authenticated' });
            } catch {
                patchState(store, { user: null, status: 'anonymous' });
            }
        },

        /** Signs in. Returns true on success; on failure the message is in loginError. */
        async login(request: LoginRequest): Promise<boolean> {
            patchState(store, { loginPending: true, loginError: null });
            try {
                const user = await firstValueFrom(service.login(request));
                patchState(store, { user, status: 'authenticated', loginPending: false });
                return true;
            } catch (error) {
                patchState(store, { loginPending: false, loginError: loginErrorText(error) });
                return false;
            }
        },

        /** Signs out on the server; locally the user is anonymous even if the call fails. */
        async logout(): Promise<void> {
            try {
                await firstValueFrom(service.logout());
            } catch {
                // The cookie may already be gone; nothing else to do.
            } finally {
                patchState(store, { user: null, status: 'anonymous', loginError: null });
            }
        },

        /** Forgets the user without calling the API (the session has already expired). */
        clear(): void {
            patchState(store, { user: null, status: 'anonymous' });
        },
    })),
);
