import { HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { FieldErrors, SaveSiteRequest, Site, SiteListItem, SiteStatus } from '../site.model';
import { SitesService } from '../sites.service';

interface SitesState {
    list: SiteListItem[];
    selected: Site | null;
    loading: boolean;
    saving: boolean;
    error: string | null;
    fieldErrors: FieldErrors;
    notFound: boolean;
}

const initialState: SitesState = {
    list: [],
    selected: null,
    loading: false,
    saving: false,
    error: null,
    fieldErrors: {},
    notFound: false,
};

const SAVE_FALLBACK_ERROR = 'Грешка при записа';
const LOAD_LIST_FALLBACK_ERROR = 'Грешка при зареждане на обектите';
const LOAD_ONE_FALLBACK_ERROR = 'Грешка при зареждане на обекта';

/** Same order as GET /api/sites: current, leads, done; then by name. */
const STATUS_ORDER: Record<SiteStatus, number> = { Active: 0, Lead: 1, Done: 2 };

function sortLikeApi(list: SiteListItem[]): SiteListItem[] {
    return [...list].sort(
        (a, b) => STATUS_ORDER[a.status] - STATUS_ORDER[b.status] || a.name.localeCompare(b.name, 'bg'),
    );
}

function toListItem({ id, name, status, companyName, endCustomer, address }: Site): SiteListItem {
    return { id, name, status, companyName, endCustomer, address };
}

function camelCase(key: string): string {
    return key ? key[0].toLowerCase() + key.slice(1) : key;
}

/** Splits an API error into per-field messages (400) and a general message (title or fallback). */
function describeError(error: unknown, fallback: string): { error: string | null; fieldErrors: FieldErrors } {
    if (error instanceof HttpErrorResponse) {
        const body = error.error as { title?: unknown; errors?: Record<string, unknown> } | null;
        const fieldErrors: FieldErrors = {};
        for (const [key, messages] of Object.entries(body?.errors ?? {})) {
            const first = (Array.isArray(messages) ? messages : []).find(
                (message): message is string => typeof message === 'string' && !!message.trim(),
            );
            if (first) {
                fieldErrors[camelCase(key)] = first;
            }
        }
        if (Object.keys(fieldErrors).length) {
            return { error: null, fieldErrors };
        }
        if (typeof body?.title === 'string' && body.title.trim()) {
            return { error: body.title, fieldErrors };
        }
    }
    return { error: fallback, fieldErrors: {} };
}

function isNotFound(error: unknown): boolean {
    return error instanceof HttpErrorResponse && error.status === 404;
}

export const SitesStore = signalStore(
    { providedIn: 'root' },
    withState(initialState),
    withMethods((store, service = inject(SitesService)) => ({
        async load(): Promise<void> {
            patchState(store, { loading: true, error: null });
            try {
                const list = await firstValueFrom(service.list());
                patchState(store, { list, loading: false });
            } catch (error) {
                patchState(store, { loading: false, error: describeError(error, LOAD_LIST_FALLBACK_ERROR).error });
            }
        },

        /** Loads one site for the detail page; a 404 sets notFound. */
        async loadOne(id: string): Promise<void> {
            patchState(store, { selected: null, notFound: false, loading: true, error: null, fieldErrors: {} });
            try {
                const selected = await firstValueFrom(service.get(id));
                patchState(store, { selected, loading: false });
            } catch (error) {
                if (isNotFound(error)) {
                    patchState(store, { loading: false, notFound: true });
                } else {
                    patchState(store, { loading: false, error: LOAD_ONE_FALLBACK_ERROR });
                }
            }
        },

        /**
         * Reloads the open site quietly (no loading state, nothing hidden), e.g. after a work day changed its status.
         * A failed reload keeps what is on screen.
         */
        async refreshOne(id: string): Promise<void> {
            try {
                const site = await firstValueFrom(service.get(id));
                if (store.selected()?.id === id) {
                    patchState(store, {
                        selected: site,
                        list: sortLikeApi(store.list().map((item) => (item.id === id ? toListItem(site) : item))),
                    });
                }
            } catch {
                // The site stays as it was; the next full load shows any problem.
            }
        },

        /** Creates a site. Returns it on success; null on failure (messages in error / fieldErrors). */
        async create(request: SaveSiteRequest): Promise<Site | null> {
            patchState(store, { saving: true, error: null, fieldErrors: {} });
            try {
                const site = await firstValueFrom(service.create(request));
                patchState(store, {
                    selected: site,
                    list: sortLikeApi([...store.list(), toListItem(site)]),
                    saving: false,
                });
                return site;
            } catch (error) {
                patchState(store, { saving: false, ...describeError(error, SAVE_FALLBACK_ERROR) });
                return null;
            }
        },

        /** Saves the site data. Returns the saved site; null on failure (a 404 sets notFound). */
        async update(id: string, request: SaveSiteRequest): Promise<Site | null> {
            patchState(store, { saving: true, error: null, fieldErrors: {} });
            try {
                const site = await firstValueFrom(service.update(id, request));
                patchState(store, {
                    selected: site,
                    list: sortLikeApi(store.list().map((item) => (item.id === id ? toListItem(site) : item))),
                    saving: false,
                });
                return site;
            } catch (error) {
                if (isNotFound(error)) {
                    patchState(store, { saving: false, notFound: true, selected: null });
                } else {
                    patchState(store, { saving: false, ...describeError(error, SAVE_FALLBACK_ERROR) });
                }
                return null;
            }
        },

        /** Deletes a site (Owner). The API refuses (409) while it has work days or payments. */
        async remove(id: string): Promise<boolean> {
            patchState(store, { saving: true, error: null, fieldErrors: {} });
            try {
                await firstValueFrom(service.remove(id));
                patchState(store, {
                    list: store.list().filter((item) => item.id !== id),
                    selected: store.selected()?.id === id ? null : store.selected(),
                    saving: false,
                });
                return true;
            } catch (error) {
                patchState(store, { saving: false, ...describeError(error, SAVE_FALLBACK_ERROR) });
                return false;
            }
        },

        clearErrors(): void {
            patchState(store, { error: null, fieldErrors: {} });
        },
    })),
);
