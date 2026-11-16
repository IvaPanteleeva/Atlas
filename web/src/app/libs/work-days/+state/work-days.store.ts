import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { describeError, FieldErrors, isNotFound } from '../../shared/api-errors';
import { SaveWorkDayRequest, WorkDay } from '../work-day.model';
import { WorkDaysService } from '../work-days.service';

interface WorkDaysState {
    /** The site whose days are loaded; a reload of the same site keeps the list on screen. */
    siteId: string | null;
    days: WorkDay[];
    loading: boolean;
    saving: boolean;
    error: string | null;
    fieldErrors: FieldErrors;
}

const initialState: WorkDaysState = {
    siteId: null,
    days: [],
    loading: false,
    saving: false,
    error: null,
    fieldErrors: {},
};

const SAVE_FALLBACK_ERROR = 'Грешка при записа';
const LOAD_FALLBACK_ERROR = 'Грешка при зареждане на работните дни';
const NOT_FOUND_ERROR = 'Денят вече го няма';

/** Newest first, like the API. Stable, so a day put first stays first among days with the same date. */
function newestFirst(days: WorkDay[]): WorkDay[] {
    return [...days].sort((a, b) => b.date.localeCompare(a.date));
}

function saveError(error: unknown): { error: string | null; fieldErrors: FieldErrors } {
    return isNotFound(error) ? { error: NOT_FOUND_ERROR, fieldErrors: {} } : describeError(error, SAVE_FALLBACK_ERROR);
}

/** Work days of the site that is open in the detail page. */
export const WorkDaysStore = signalStore(
    { providedIn: 'root' },
    withState(initialState),
    withMethods((store, service = inject(WorkDaysService)) => ({
        /** Loads the days of one site. Another site's days are dropped first; the same site's stay until the reply. */
        async load(siteId: string): Promise<void> {
            const days = store.siteId() === siteId ? store.days() : [];
            patchState(store, { siteId, days, loading: true, error: null, fieldErrors: {} });
            try {
                const loaded = await firstValueFrom(service.list(siteId));
                if (store.siteId() === siteId) {
                    patchState(store, { days: loaded, loading: false });
                }
            } catch (error) {
                if (store.siteId() === siteId) {
                    patchState(store, { loading: false, error: describeError(error, LOAD_FALLBACK_ERROR).error });
                }
            }
        },

        /** Adds a day. Returns it; null on failure (messages in error / fieldErrors). */
        async create(siteId: string, request: SaveWorkDayRequest): Promise<WorkDay | null> {
            patchState(store, { saving: true, error: null, fieldErrors: {} });
            try {
                const created = await firstValueFrom(service.create(siteId, request));
                patchState(store, { days: newestFirst([created, ...store.days()]), saving: false });
                return created;
            } catch (error) {
                patchState(store, { saving: false, ...saveError(error) });
                return null;
            }
        },

        /** Saves a day. Returns the saved day; null on failure. */
        async update(siteId: string, id: string, request: SaveWorkDayRequest): Promise<WorkDay | null> {
            patchState(store, { saving: true, error: null, fieldErrors: {} });
            try {
                const updated = await firstValueFrom(service.update(siteId, id, request));
                const others = store.days().filter((day) => day.id !== id);
                patchState(store, { days: newestFirst([updated, ...others]), saving: false });
                return updated;
            } catch (error) {
                patchState(store, { saving: false, ...saveError(error) });
                return null;
            }
        },

        /** Deletes a day (Owner). */
        async remove(siteId: string, id: string): Promise<boolean> {
            patchState(store, { saving: true, error: null, fieldErrors: {} });
            try {
                await firstValueFrom(service.remove(siteId, id));
                patchState(store, { days: store.days().filter((day) => day.id !== id), saving: false });
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
