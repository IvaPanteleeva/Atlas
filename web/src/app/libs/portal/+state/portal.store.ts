import { HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { PortalSite, PortalSiteListItem, PortalTotals } from '../portal.model';
import { PortalService } from '../portal.service';

interface PortalState {
    sites: PortalSiteListItem[];
    totals: PortalTotals | null;
    selected: PortalSite | null;
    loading: boolean;
    error: string | null;
    notFound: boolean;
}

const initialState: PortalState = {
    sites: [],
    totals: null,
    selected: null,
    loading: false,
    error: null,
    notFound: false,
};

const LOAD_LIST_FALLBACK_ERROR = 'Грешка при зареждане на обектите';
const LOAD_ONE_FALLBACK_ERROR = 'Грешка при зареждане на обекта';

/** The ProblemDetails title from the API, or the fallback. */
function errorText(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse) {
        const title = (error.error as { title?: unknown } | null)?.title;
        if (typeof title === 'string' && title.trim()) {
            return title;
        }
    }
    return fallback;
}

/**
 * Read-only state of the client portal. Order and amounts come from the server as they are.
 * Not in root: each portal page provides its own store, so it lives and dies with the page. After signing out
 * (or an expired session) the page is gone, and the next client starts with an empty store, never with the
 * previous client's sites and money.
 */
export const PortalStore = signalStore(
    withState(initialState),
    withMethods((store, service = inject(PortalService)) => ({
        /** Drops whatever was shown before, so nothing old is on screen while the answer is on its way. */
        async loadSites(): Promise<void> {
            patchState(store, { sites: [], totals: null, loading: true, error: null });
            try {
                const { sites, totals } = await firstValueFrom(service.sites());
                patchState(store, { sites, totals, loading: false });
            } catch (error) {
                patchState(store, {
                    sites: [],
                    totals: null,
                    loading: false,
                    error: errorText(error, LOAD_LIST_FALLBACK_ERROR),
                });
            }
        },

        /** Loads one site; a 404 (no such site, or not the client's) sets notFound. */
        async loadSite(id: string): Promise<void> {
            patchState(store, { selected: null, notFound: false, loading: true, error: null });
            try {
                const selected = await firstValueFrom(service.site(id));
                patchState(store, { selected, loading: false });
            } catch (error) {
                if (error instanceof HttpErrorResponse && error.status === 404) {
                    patchState(store, { loading: false, notFound: true });
                } else {
                    patchState(store, { loading: false, error: errorText(error, LOAD_ONE_FALLBACK_ERROR) });
                }
            }
        },
    })),
);
