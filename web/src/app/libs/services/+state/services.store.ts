import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { describeError, FieldErrors, isNotFound } from '../../shared/api-errors';
import { SaveServiceRequest, Service } from '../service.model';
import { ServicesService } from '../services.service';

interface ServicesState {
    services: Service[];
    loading: boolean;
    saving: boolean;
    error: string | null;
    fieldErrors: FieldErrors;
}

const initialState: ServicesState = {
    services: [],
    loading: false,
    saving: false,
    error: null,
    fieldErrors: {},
};

const SAVE_FALLBACK_ERROR = 'Грешка при записа';
const LOAD_FALLBACK_ERROR = 'Грешка при зареждане на услугите';
const NOT_FOUND_ERROR = 'Услугата вече я няма';

/** Same order as GET /api/services (by name), with Bulgarian collation. */
function sortByName(services: Service[]): Service[] {
    return [...services].sort((a, b) => a.name.localeCompare(b.name, 'bg'));
}

function saveError(error: unknown): { error: string | null; fieldErrors: FieldErrors } {
    return isNotFound(error) ? { error: NOT_FOUND_ERROR, fieldErrors: {} } : describeError(error, SAVE_FALLBACK_ERROR);
}

export const ServicesStore = signalStore(
    { providedIn: 'root' },
    withState(initialState),
    withMethods((store, service = inject(ServicesService)) => ({
        async load(): Promise<void> {
            patchState(store, { loading: true, error: null });
            try {
                const services = await firstValueFrom(service.list());
                patchState(store, { services, loading: false });
            } catch (error) {
                patchState(store, { loading: false, error: describeError(error, LOAD_FALLBACK_ERROR).error });
            }
        },

        /** Adds a service (Owner). Returns it; null on failure (messages in error / fieldErrors). */
        async create(request: SaveServiceRequest): Promise<Service | null> {
            patchState(store, { saving: true, error: null, fieldErrors: {} });
            try {
                const created = await firstValueFrom(service.create(request));
                patchState(store, { services: sortByName([...store.services(), created]), saving: false });
                return created;
            } catch (error) {
                patchState(store, { saving: false, ...saveError(error) });
                return null;
            }
        },

        /** Saves a service (Owner). Returns it; null on failure. */
        async update(id: string, request: SaveServiceRequest): Promise<Service | null> {
            patchState(store, { saving: true, error: null, fieldErrors: {} });
            try {
                const updated = await firstValueFrom(service.update(id, request));
                const services = store.services().map((item) => (item.id === id ? updated : item));
                patchState(store, { services: sortByName(services), saving: false });
                return updated;
            } catch (error) {
                patchState(store, { saving: false, ...saveError(error) });
                return null;
            }
        },

        /** Deletes a service (Owner). The API refuses (409) while sites have prices for it. */
        async remove(id: string): Promise<boolean> {
            patchState(store, { saving: true, error: null, fieldErrors: {} });
            try {
                await firstValueFrom(service.remove(id));
                patchState(store, { services: store.services().filter((item) => item.id !== id), saving: false });
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
