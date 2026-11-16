import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { describeError } from '../../shared/api-errors';
import { SummariesService } from '../summaries.service';
import { SiteSummary, SummariesBySite, SummaryTotals } from '../summary.model';

interface SummariesState {
    /** The site whose summary is loaded (detail page). */
    siteId: string | null;
    site: SiteSummary | null;
    siteLoading: boolean;
    /** All sites together (sites list); null until loaded. */
    totals: SummaryTotals | null;
    bySite: SummariesBySite;
    allLoading: boolean;
    error: string | null;
}

const initialState: SummariesState = {
    siteId: null,
    site: null,
    siteLoading: false,
    totals: null,
    bySite: {},
    allLoading: false,
    error: null,
};

const LOAD_FALLBACK_ERROR = 'Грешка при зареждане на сумите';

/** Money computed by the server (Owner and Manager): the open site's summary and the totals of all sites. */
export const SummariesStore = signalStore(
    { providedIn: 'root' },
    withState(initialState),
    withMethods((store, service = inject(SummariesService)) => ({
        /** Loads the summary of one site; another site's summary is dropped first. */
        async loadSite(siteId: string): Promise<void> {
            const site = store.siteId() === siteId ? store.site() : null;
            patchState(store, { siteId, site, siteLoading: true, error: null });
            try {
                const loaded = await firstValueFrom(service.getSite(siteId));
                if (store.siteId() === siteId) {
                    patchState(store, { site: loaded, siteLoading: false });
                }
            } catch (error) {
                if (store.siteId() === siteId) {
                    patchState(store, { siteLoading: false, error: describeError(error, LOAD_FALLBACK_ERROR).error });
                }
            }
        },

        /** Reloads the open site's summary quietly (after a work day or a payment changed); a failure keeps it. */
        async refreshSite(siteId: string): Promise<void> {
            try {
                const loaded = await firstValueFrom(service.getSite(siteId));
                if (store.siteId() === siteId) {
                    patchState(store, { site: loaded });
                }
            } catch {
                // The old amounts stay; the next full load shows any problem.
            }
        },

        /** Loads the totals and the per-site summaries for the sites list; the old ones stay until the reply. */
        async loadAll(): Promise<void> {
            patchState(store, { allLoading: true, error: null });
            try {
                const { totals, sites } = await firstValueFrom(service.getAll());
                const bySite: SummariesBySite = {};
                for (const row of sites) {
                    bySite[row.siteId] = row;
                }
                patchState(store, { totals, bySite, allLoading: false });
            } catch (error) {
                patchState(store, { allLoading: false, error: describeError(error, LOAD_FALLBACK_ERROR).error });
            }
        },
    })),
);
