import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { describeError, FieldErrors, isNotFound } from '../../shared/api-errors';
import { SaveSiteContactRequest, SiteContact } from '../site-contact.model';
import { SiteContactsService } from '../site-contacts.service';

interface SiteContactsState {
    /** The site whose contacts are loaded. */
    siteId: string | null;
    contacts: SiteContact[];
    loading: boolean;
    saving: boolean;
    error: string | null;
    fieldErrors: FieldErrors;
}

const initialState: SiteContactsState = {
    siteId: null,
    contacts: [],
    loading: false,
    saving: false,
    error: null,
    fieldErrors: {},
};

const SAVE_FALLBACK_ERROR = 'Грешка при записа';
const LOAD_FALLBACK_ERROR = 'Грешка при зареждане на контактите';
const NOT_FOUND_ERROR = 'Контактът вече го няма';

/** Same order as the API (by name), with Bulgarian collation. */
function sortByName(contacts: SiteContact[]): SiteContact[] {
    return [...contacts].sort((a, b) => a.name.localeCompare(b.name, 'bg'));
}

function saveError(error: unknown): { error: string | null; fieldErrors: FieldErrors } {
    return isNotFound(error) ? { error: NOT_FOUND_ERROR, fieldErrors: {} } : describeError(error, SAVE_FALLBACK_ERROR);
}

/** Contacts of the site that is open in the detail page (owner, neighbour…). */
export const SiteContactsStore = signalStore(
    { providedIn: 'root' },
    withState(initialState),
    withMethods((store, service = inject(SiteContactsService)) => ({
        /** Loads the contacts of one site. Another site's contacts are dropped first. */
        async load(siteId: string): Promise<void> {
            const contacts = store.siteId() === siteId ? store.contacts() : [];
            patchState(store, { siteId, contacts, loading: true, error: null, fieldErrors: {} });
            try {
                const loaded = await firstValueFrom(service.list(siteId));
                if (store.siteId() === siteId) {
                    patchState(store, { contacts: loaded, loading: false });
                }
            } catch (error) {
                if (store.siteId() === siteId) {
                    patchState(store, { loading: false, error: describeError(error, LOAD_FALLBACK_ERROR).error });
                }
            }
        },

        /** Adds a contact (Owner, Manager). Returns it; null on failure. */
        async create(siteId: string, request: SaveSiteContactRequest): Promise<SiteContact | null> {
            patchState(store, { saving: true, error: null, fieldErrors: {} });
            try {
                const created = await firstValueFrom(service.create(siteId, request));
                patchState(store, { contacts: sortByName([...store.contacts(), created]), saving: false });
                return created;
            } catch (error) {
                patchState(store, { saving: false, ...saveError(error) });
                return null;
            }
        },

        /** Saves a contact (Owner, Manager). Returns it; null on failure. */
        async update(siteId: string, id: string, request: SaveSiteContactRequest): Promise<SiteContact | null> {
            patchState(store, { saving: true, error: null, fieldErrors: {} });
            try {
                const updated = await firstValueFrom(service.update(siteId, id, request));
                const contacts = store.contacts().map((contact) => (contact.id === id ? updated : contact));
                patchState(store, { contacts: sortByName(contacts), saving: false });
                return updated;
            } catch (error) {
                patchState(store, { saving: false, ...saveError(error) });
                return null;
            }
        },

        /** Deletes a contact (Owner only). */
        async remove(siteId: string, id: string): Promise<boolean> {
            patchState(store, { saving: true, error: null, fieldErrors: {} });
            try {
                await firstValueFrom(service.remove(siteId, id));
                patchState(store, {
                    contacts: store.contacts().filter((contact) => contact.id !== id),
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
