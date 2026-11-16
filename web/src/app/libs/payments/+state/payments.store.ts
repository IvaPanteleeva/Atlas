import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { describeError, FieldErrors, isNotFound } from '../../shared/api-errors';
import { Payment, SavePaymentRequest } from '../payment.model';
import { PaymentsService } from '../payments.service';

interface PaymentsState {
    /** The site whose payments are loaded; a reload of the same site keeps the list on screen. */
    siteId: string | null;
    payments: Payment[];
    loading: boolean;
    saving: boolean;
    error: string | null;
    fieldErrors: FieldErrors;
}

const initialState: PaymentsState = {
    siteId: null,
    payments: [],
    loading: false,
    saving: false,
    error: null,
    fieldErrors: {},
};

const SAVE_FALLBACK_ERROR = 'Грешка при записа';
const LOAD_FALLBACK_ERROR = 'Грешка при зареждане на плащанията';
const NOT_FOUND_ERROR = 'Плащането вече го няма';

/** Newest first, like the API. Stable, so a payment put first stays first among payments with the same date. */
function newestFirst(payments: Payment[]): Payment[] {
    return [...payments].sort((a, b) => b.date.localeCompare(a.date));
}

function saveError(error: unknown): { error: string | null; fieldErrors: FieldErrors } {
    return isNotFound(error) ? { error: NOT_FOUND_ERROR, fieldErrors: {} } : describeError(error, SAVE_FALLBACK_ERROR);
}

/** Client payments of the site that is open in the detail page (Owner only). */
export const PaymentsStore = signalStore(
    { providedIn: 'root' },
    withState(initialState),
    withMethods((store, service = inject(PaymentsService)) => ({
        /** Loads the payments of one site. Another site's are dropped first; the same site's stay until the reply. */
        async load(siteId: string): Promise<void> {
            const payments = store.siteId() === siteId ? store.payments() : [];
            patchState(store, { siteId, payments, loading: true, error: null, fieldErrors: {} });
            try {
                const loaded = await firstValueFrom(service.list(siteId));
                if (store.siteId() === siteId) {
                    patchState(store, { payments: loaded, loading: false });
                }
            } catch (error) {
                if (store.siteId() === siteId) {
                    patchState(store, { loading: false, error: describeError(error, LOAD_FALLBACK_ERROR).error });
                }
            }
        },

        /** Adds a payment. Returns it; null on failure (messages in error / fieldErrors). */
        async create(siteId: string, request: SavePaymentRequest): Promise<Payment | null> {
            patchState(store, { saving: true, error: null, fieldErrors: {} });
            try {
                const created = await firstValueFrom(service.create(siteId, request));
                patchState(store, { payments: newestFirst([created, ...store.payments()]), saving: false });
                return created;
            } catch (error) {
                patchState(store, { saving: false, ...saveError(error) });
                return null;
            }
        },

        /** Saves a payment. Returns the saved payment; null on failure. */
        async update(siteId: string, id: string, request: SavePaymentRequest): Promise<Payment | null> {
            patchState(store, { saving: true, error: null, fieldErrors: {} });
            try {
                const updated = await firstValueFrom(service.update(siteId, id, request));
                const others = store.payments().filter((payment) => payment.id !== id);
                patchState(store, { payments: newestFirst([updated, ...others]), saving: false });
                return updated;
            } catch (error) {
                patchState(store, { saving: false, ...saveError(error) });
                return null;
            }
        },

        /** Deletes a payment. */
        async remove(siteId: string, id: string): Promise<boolean> {
            patchState(store, { saving: true, error: null, fieldErrors: {} });
            try {
                await firstValueFrom(service.remove(siteId, id));
                patchState(store, {
                    payments: store.payments().filter((payment) => payment.id !== id),
                    saving: false,
                });
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
