import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { describeError, FieldErrors, isNotFound } from '../../shared/api-errors';
import { CreateSitePriceRequest, SitePrice, UpdateSitePriceRequest } from '../site-price.model';
import { SitePricesService } from '../site-prices.service';

interface SitePricesState {
    prices: SitePrice[];
    loading: boolean;
    saving: boolean;
    error: string | null;
    fieldErrors: FieldErrors;
}

const initialState: SitePricesState = {
    prices: [],
    loading: false,
    saving: false,
    error: null,
    fieldErrors: {},
};

const SAVE_FALLBACK_ERROR = 'Грешка при записа';
const LOAD_FALLBACK_ERROR = 'Грешка при зареждане на цените';
const NOT_FOUND_ERROR = 'Цената вече я няма';

/** Same order as the API (by service name), with Bulgarian collation. */
function sortByServiceName(prices: SitePrice[]): SitePrice[] {
    return [...prices].sort((a, b) => a.serviceName.localeCompare(b.serviceName, 'bg'));
}

function saveError(error: unknown): { error: string | null; fieldErrors: FieldErrors } {
    return isNotFound(error) ? { error: NOT_FOUND_ERROR, fieldErrors: {} } : describeError(error, SAVE_FALLBACK_ERROR);
}

/** Prices of the site that is open in the detail page. */
export const SitePricesStore = signalStore(
    { providedIn: 'root' },
    withState(initialState),
    withMethods((store, service = inject(SitePricesService)) => ({
        /** Loads the prices of one site; the previous site's prices are dropped first. */
        async load(siteId: string): Promise<void> {
            patchState(store, { prices: [], loading: true, error: null, fieldErrors: {} });
            try {
                const prices = await firstValueFrom(service.list(siteId));
                patchState(store, { prices, loading: false });
            } catch (error) {
                patchState(store, { loading: false, error: describeError(error, LOAD_FALLBACK_ERROR).error });
            }
        },

        /** Adds a price (Owner). Returns it; null on failure (messages in error / fieldErrors). */
        async create(siteId: string, request: CreateSitePriceRequest): Promise<SitePrice | null> {
            patchState(store, { saving: true, error: null, fieldErrors: {} });
            try {
                const created = await firstValueFrom(service.create(siteId, request));
                patchState(store, { prices: sortByServiceName([...store.prices(), created]), saving: false });
                return created;
            } catch (error) {
                patchState(store, { saving: false, ...saveError(error) });
                return null;
            }
        },

        /** Changes the rate (Owner). Returns the saved price; null on failure. */
        async update(siteId: string, id: string, request: UpdateSitePriceRequest): Promise<SitePrice | null> {
            patchState(store, { saving: true, error: null, fieldErrors: {} });
            try {
                const updated = await firstValueFrom(service.update(siteId, id, request));
                const prices = store.prices().map((price) => (price.id === id ? updated : price));
                patchState(store, { prices: sortByServiceName(prices), saving: false });
                return updated;
            } catch (error) {
                patchState(store, { saving: false, ...saveError(error) });
                return null;
            }
        },

        /** Deletes a price (Owner); work days keep their own copy of the rate. */
        async remove(siteId: string, id: string): Promise<boolean> {
            patchState(store, { saving: true, error: null, fieldErrors: {} });
            try {
                await firstValueFrom(service.remove(siteId, id));
                patchState(store, { prices: store.prices().filter((price) => price.id !== id), saving: false });
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
