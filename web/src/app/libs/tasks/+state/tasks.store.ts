import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { describeError, FieldErrors, isNotFound } from '../../shared/api-errors';
import { CreateTaskRequest, SiteTask, TaskQuery, UpdateTaskRequest } from '../task.model';
import { TasksService } from '../tasks.service';

interface TasksState {
    /** The filter of the loaded tasks (a site in its detail page, everything or one person in „Задачи“). */
    query: TaskQuery | null;
    tasks: SiteTask[];
    loading: boolean;
    saving: boolean;
    error: string | null;
    /** Messages of the add row (POST /api/tasks). */
    fieldErrors: FieldErrors;
}

const initialState: TasksState = {
    query: null,
    tasks: [],
    loading: false,
    saving: false,
    error: null,
    fieldErrors: {},
};

const SAVE_FALLBACK_ERROR = 'Грешка при записа';
const LOAD_FALLBACK_ERROR = 'Грешка при зареждане на задачите';
const NOT_FOUND_ERROR = 'Задачата вече я няма';

function sameQuery(a: TaskQuery | null, b: TaskQuery): boolean {
    return !!a && a.siteId === b.siteId && a.assigneeId === b.assigneeId && a.scope === b.scope;
}

/** Whether the loaded filter shows this task (a new or changed task can fall outside it). */
function matches(query: TaskQuery | null, task: SiteTask): boolean {
    if (!query) {
        return false;
    }
    if (query.siteId && task.siteId !== query.siteId) {
        return false;
    }
    if (query.scope === 'company' && task.siteId !== null) {
        return false;
    }
    return !query.assigneeId || task.assigneeId === query.assigneeId;
}

const time = (value: string | null) => (value ? Date.parse(value) : 0);

/** Same order as the API: open ones by creation (oldest first), then done ones (last done first). */
function sortLikeApi(tasks: SiteTask[]): SiteTask[] {
    return [...tasks].sort((a, b) => {
        if (a.done !== b.done) {
            return a.done ? 1 : -1;
        }
        return a.done ? time(b.doneAt) - time(a.doneAt) : time(a.createdAt) - time(b.createdAt);
    });
}

/** A failed change in the list: one message (the list has no fields to put a 400 under). */
function listError(error: unknown): { error: string | null; fieldErrors: FieldErrors } {
    if (isNotFound(error)) {
        return { error: NOT_FOUND_ERROR, fieldErrors: {} };
    }
    const described = describeError(error, SAVE_FALLBACK_ERROR);
    return {
        error: described.error ?? Object.values(described.fieldErrors)[0] ?? SAVE_FALLBACK_ERROR,
        fieldErrors: {},
    };
}

/** Tasks of the open site or of the „Задачи“ tab. Owner and Manager write and tick; only Owner deletes. */
export const TasksStore = signalStore(
    { providedIn: 'root' },
    withState(initialState),
    withComputed(({ tasks, fieldErrors }) => ({
        openCount: computed(() => tasks().filter((task) => !task.done).length),
        /** First message of the add row (text, person or site). */
        fieldError: computed(() => Object.values(fieldErrors())[0] ?? null),
    })),
    withMethods((store, service = inject(TasksService)) => {
        /** Puts a saved task in its place (or drops it when the filter no longer shows it). */
        function put(saved: SiteTask): void {
            const others = store.tasks().filter((task) => task.id !== saved.id);
            patchState(store, {
                tasks: sortLikeApi(matches(store.query(), saved) ? [...others, saved] : others),
            });
        }

        return {
            /** Loads the tasks of a filter. Another filter's tasks are dropped first. */
            async load(query: TaskQuery): Promise<void> {
                const tasks = sameQuery(store.query(), query) ? store.tasks() : [];
                patchState(store, { query, tasks, loading: true, error: null, fieldErrors: {} });
                try {
                    const loaded = await firstValueFrom(service.list(query));
                    if (sameQuery(store.query(), query)) {
                        patchState(store, { tasks: loaded, loading: false });
                    }
                } catch (error) {
                    if (sameQuery(store.query(), query)) {
                        patchState(store, { loading: false, error: describeError(error, LOAD_FALLBACK_ERROR).error });
                    }
                }
            },

            /** Adds a task (Owner, Manager). Returns it; null on failure (messages in fieldErrors / error). */
            async create(request: CreateTaskRequest): Promise<SiteTask | null> {
                patchState(store, { saving: true, error: null, fieldErrors: {} });
                try {
                    const created = await firstValueFrom(service.create(request));
                    put(created);
                    patchState(store, { saving: false });
                    return created;
                } catch (error) {
                    patchState(store, { saving: false, ...describeError(error, SAVE_FALLBACK_ERROR) });
                    return null;
                }
            },

            /** Saves the text and the person (Owner, Manager). Returns the task; null on failure. */
            async update(id: string, request: UpdateTaskRequest): Promise<SiteTask | null> {
                patchState(store, { saving: true, error: null, fieldErrors: {} });
                try {
                    const updated = await firstValueFrom(service.update(id, request));
                    put(updated);
                    patchState(store, { saving: false });
                    return updated;
                } catch (error) {
                    patchState(store, { saving: false, ...listError(error) });
                    return null;
                }
            },

            /**
             * Ticks or unticks a task. The row moves at once (the checkbox follows the tap); a failure puts it back.
             * The real time it was done comes from the server.
             */
            async setDone(id: string, done: boolean): Promise<boolean> {
                const before = store.tasks().find((task) => task.id === id);
                if (!before) {
                    return false;
                }
                patchState(store, { error: null, fieldErrors: {} });
                put({ ...before, done, doneAt: done ? new Date().toISOString() : null });
                try {
                    put(await firstValueFrom(service.setDone(id, done)));
                    return true;
                } catch (error) {
                    put(before);
                    patchState(store, listError(error));
                    return false;
                }
            },

            /** Deletes a task (Owner only). */
            async remove(id: string): Promise<boolean> {
                patchState(store, { saving: true, error: null, fieldErrors: {} });
                try {
                    await firstValueFrom(service.remove(id));
                    patchState(store, { tasks: store.tasks().filter((task) => task.id !== id), saving: false });
                    return true;
                } catch (error) {
                    patchState(store, { saving: false, ...listError(error) });
                    return false;
                }
            },

            clearErrors(): void {
                patchState(store, { error: null, fieldErrors: {} });
            },
        };
    }),
);
