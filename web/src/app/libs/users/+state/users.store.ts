import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { describeError, FieldErrors, isNotFound } from '../../shared/api-errors';
import { CreateUserRequest, UserAccount } from '../user.model';
import { UsersService } from '../users.service';

interface UsersState {
    users: UserAccount[];
    loading: boolean;
    saving: boolean;
    error: string | null;
    fieldErrors: FieldErrors;
}

const initialState: UsersState = {
    users: [],
    loading: false,
    saving: false,
    error: null,
    fieldErrors: {},
};

const SAVE_FALLBACK_ERROR = 'Грешка при записа';
const LOAD_FALLBACK_ERROR = 'Грешка при зареждане на профилите';
const NOT_FOUND_ERROR = 'Профилът вече го няма';

function sortByName(users: UserAccount[]): UserAccount[] {
    return [...users].sort((a, b) => a.userName.localeCompare(b.userName, 'bg'));
}

function saveError(error: unknown): { error: string | null; fieldErrors: FieldErrors } {
    return isNotFound(error) ? { error: NOT_FOUND_ERROR, fieldErrors: {} } : describeError(error, SAVE_FALLBACK_ERROR);
}

/** Login profiles (Owner only): list, create, new password, delete. */
export const UsersStore = signalStore(
    { providedIn: 'root' },
    withState(initialState),
    withMethods((store, service = inject(UsersService)) => ({
        async load(): Promise<void> {
            patchState(store, { loading: true, error: null });
            try {
                const users = await firstValueFrom(service.list());
                patchState(store, { users, loading: false });
            } catch (error) {
                patchState(store, { loading: false, error: describeError(error, LOAD_FALLBACK_ERROR).error });
            }
        },

        /** Creates a profile. Returns it; null on failure (409 „exists“ in error, 400 in fieldErrors). */
        async create(request: CreateUserRequest): Promise<UserAccount | null> {
            patchState(store, { saving: true, error: null, fieldErrors: {} });
            try {
                const created = await firstValueFrom(service.create(request));
                patchState(store, { users: sortByName([...store.users(), created]), saving: false });
                return created;
            } catch (error) {
                patchState(store, { saving: false, ...saveError(error) });
                return null;
            }
        },

        /** Sets a new password for a profile. */
        async setPassword(id: string, password: string): Promise<boolean> {
            patchState(store, { saving: true, error: null, fieldErrors: {} });
            try {
                await firstValueFrom(service.setPassword(id, { password }));
                patchState(store, { saving: false });
                return true;
            } catch (error) {
                patchState(store, { saving: false, ...saveError(error) });
                return false;
            }
        },

        /** Links a crew person to the profile (null = unlink). Returns the profile; null on failure (409 in error). */
        async linkPerson(id: string, personId: string | null): Promise<UserAccount | null> {
            patchState(store, { saving: true, error: null, fieldErrors: {} });
            try {
                const updated = await firstValueFrom(service.linkPerson(id, { personId }));
                patchState(store, {
                    users: store.users().map((user) => (user.id === id ? updated : user)),
                    saving: false,
                });
                return updated;
            } catch (error) {
                patchState(store, { saving: false, ...saveError(error) });
                return null;
            }
        },

        /** Deletes a profile. The API refuses (409) the own profile and the last Owner. */
        async remove(id: string): Promise<boolean> {
            patchState(store, { saving: true, error: null, fieldErrors: {} });
            try {
                await firstValueFrom(service.remove(id));
                patchState(store, { users: store.users().filter((user) => user.id !== id), saving: false });
                return true;
            } catch (error) {
                patchState(store, { saving: false, ...saveError(error) });
                return false;
            }
        },

        clearErrors(): void {
            patchState(store, { error: null, fieldErrors: {} });
        },
    })),
);
