import { HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { CompaniesService } from '../companies.service';
import { Company } from '../company.model';

interface CompaniesState {
    companies: Company[];
    loading: boolean;
    saving: boolean;
    error: string | null;
}

const initialState: CompaniesState = {
    companies: [],
    loading: false,
    saving: false,
    error: null,
};

const SAVE_FALLBACK_ERROR = 'Грешка при записа';
const LOAD_FALLBACK_ERROR = 'Грешка при зареждане на фирмите';

/** Text for the alert: the first validation message (400), the ProblemDetails title (409), or the fallback. */
function errorText(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse) {
        const body = error.error as { title?: unknown; errors?: Record<string, unknown> } | null;
        const firstValidation = Object.values(body?.errors ?? {})
            .flatMap((messages) => (Array.isArray(messages) ? messages : []))
            .find((message): message is string => typeof message === 'string' && !!message.trim());
        if (firstValidation) {
            return firstValidation;
        }
        if (typeof body?.title === 'string' && body.title.trim()) {
            return body.title;
        }
    }
    return fallback;
}

/** Same order as the API (by name), with Bulgarian collation. */
function sortByName(companies: Company[]): Company[] {
    return [...companies].sort((a, b) => a.name.localeCompare(b.name, 'bg'));
}

export const CompaniesStore = signalStore(
    { providedIn: 'root' },
    withState(initialState),
    withMethods((store, service = inject(CompaniesService)) => ({
        async load(): Promise<void> {
            patchState(store, { loading: true, error: null });
            try {
                const companies = await firstValueFrom(service.list());
                patchState(store, { companies, loading: false });
            } catch (error) {
                patchState(store, { loading: false, error: errorText(error, LOAD_FALLBACK_ERROR) });
            }
        },

        /** Adds a company. Returns it on success; null on failure (the message is in error). */
        async create(name: string): Promise<Company | null> {
            patchState(store, { saving: true, error: null });
            try {
                const company = await firstValueFrom(service.create({ name: name.trim() }));
                patchState(store, { companies: sortByName([...store.companies(), company]), saving: false });
                return company;
            } catch (error) {
                patchState(store, { saving: false, error: errorText(error, SAVE_FALLBACK_ERROR) });
                return null;
            }
        },

        /** Renames a company. Returns true on success; on failure the old name stays and error is set. */
        async rename(id: string, name: string): Promise<boolean> {
            patchState(store, { saving: true, error: null });
            try {
                const updated = await firstValueFrom(service.update(id, { name: name.trim() }));
                const companies = store.companies().map((company) => (company.id === id ? updated : company));
                patchState(store, { companies: sortByName(companies), saving: false });
                return true;
            } catch (error) {
                patchState(store, { saving: false, error: errorText(error, SAVE_FALLBACK_ERROR) });
                return false;
            }
        },

        /** Deletes a company. The API refuses (409) while sites or users use it. */
        async remove(id: string): Promise<boolean> {
            patchState(store, { saving: true, error: null });
            try {
                await firstValueFrom(service.remove(id));
                patchState(store, {
                    companies: store.companies().filter((company) => company.id !== id),
                    saving: false,
                });
                return true;
            } catch (error) {
                patchState(store, { saving: false, error: errorText(error, SAVE_FALLBACK_ERROR) });
                return false;
            }
        },

        clearError(): void {
            patchState(store, { error: null });
        },
    })),
);
