import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { describeError, FieldErrors, isNotFound } from '../../shared/api-errors';
import { PeopleService } from '../people.service';
import { Person, SavePersonRequest } from '../person.model';

interface PeopleState {
    people: Person[];
    loading: boolean;
    saving: boolean;
    error: string | null;
    fieldErrors: FieldErrors;
}

const initialState: PeopleState = {
    people: [],
    loading: false,
    saving: false,
    error: null,
    fieldErrors: {},
};

const SAVE_FALLBACK_ERROR = 'Грешка при записа';
const LOAD_FALLBACK_ERROR = 'Грешка при зареждане на хората';
const NOT_FOUND_ERROR = 'Човекът вече го няма';

/** Same order as GET /api/people (by name), with Bulgarian collation. */
function sortByName(people: Person[]): Person[] {
    return [...people].sort((a, b) => a.name.localeCompare(b.name, 'bg'));
}

function saveError(error: unknown): { error: string | null; fieldErrors: FieldErrors } {
    return isNotFound(error) ? { error: NOT_FOUND_ERROR, fieldErrors: {} } : describeError(error, SAVE_FALLBACK_ERROR);
}

/** Our people: crew („работи с нас“) and contacts. Owner and Manager write; only Owner deletes. */
export const PeopleStore = signalStore(
    { providedIn: 'root' },
    withState(initialState),
    withComputed(({ people }) => ({
        crew: computed(() => people().filter((person) => person.isCrew)),
        contacts: computed(() => people().filter((person) => !person.isCrew)),
    })),
    withMethods((store, service = inject(PeopleService)) => ({
        async load(): Promise<void> {
            patchState(store, { loading: true, error: null });
            try {
                const people = await firstValueFrom(service.list());
                patchState(store, { people, loading: false });
            } catch (error) {
                patchState(store, { loading: false, error: describeError(error, LOAD_FALLBACK_ERROR).error });
            }
        },

        /** Adds a person (Owner). Returns it; null on failure (messages in error / fieldErrors). */
        async create(request: SavePersonRequest): Promise<Person | null> {
            patchState(store, { saving: true, error: null, fieldErrors: {} });
            try {
                const created = await firstValueFrom(service.create(request));
                patchState(store, { people: sortByName([...store.people(), created]), saving: false });
                return created;
            } catch (error) {
                patchState(store, { saving: false, ...saveError(error) });
                return null;
            }
        },

        /** Saves a person (Owner). Returns it; null on failure. */
        async update(id: string, request: SavePersonRequest): Promise<Person | null> {
            patchState(store, { saving: true, error: null, fieldErrors: {} });
            try {
                const updated = await firstValueFrom(service.update(id, request));
                const people = store.people().map((person) => (person.id === id ? updated : person));
                patchState(store, { people: sortByName(people), saving: false });
                return updated;
            } catch (error) {
                patchState(store, { saving: false, ...saveError(error) });
                return null;
            }
        },

        /** Deletes a person (Owner). The API refuses (409) while they have work days. */
        async remove(id: string): Promise<boolean> {
            patchState(store, { saving: true, error: null, fieldErrors: {} });
            try {
                await firstValueFrom(service.remove(id));
                patchState(store, { people: store.people().filter((person) => person.id !== id), saving: false });
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
