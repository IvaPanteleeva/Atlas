import { HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { CreateTaskRequest, SiteTask, TaskQuery, UpdateTaskRequest } from '../task.model';

/** Test data and a hand-written TasksService fake shared by the specs. */
export function testTask(overrides: Partial<SiteTask> = {}): SiteTask {
    return {
        id: 't1',
        text: 'Да се купи лепило',
        done: false,
        siteId: 's1',
        assigneeId: null,
        assigneeName: null,
        createdAt: '2026-11-10T08:00:00Z',
        doneAt: null,
        ...overrides,
    };
}

export function httpError(status: number, error: unknown = null): Observable<never> {
    return throwError(() => new HttpErrorResponse({ status, error }));
}

/** „list siteId=s1“, „list assigneeId=p1“, „list“ (no filter). */
function describeQuery(query: TaskQuery): string {
    const parts = Object.entries(query)
        .filter(([, value]) => !!value)
        .map(([key, value]) => `${key}=${value}`);
    return ['list', ...parts].join(' ');
}

/**
 * Lists `tasks` (unless listResult is set). update / setDone answer with the known task changed, so the other
 * fields (site, assignee, dates) stay as the server would keep them.
 */
export class FakeTasksService {
    tasks: SiteTask[] = [];
    listResult: Observable<SiteTask[]> | null = null;
    createResult: Observable<SiteTask> | null = null;
    updateResult: Observable<SiteTask> | null = null;
    setDoneResult: Observable<SiteTask> | null = null;
    removeResult: Observable<void> = of(undefined);
    calls: string[] = [];
    lastCreate: CreateTaskRequest | null = null;
    lastUpdate: UpdateTaskRequest | null = null;

    list(query: TaskQuery = {}): Observable<SiteTask[]> {
        this.calls.push(describeQuery(query));
        return this.listResult ?? of(this.tasks);
    }

    /** Echoes the request back as the saved task unless createResult is set. */
    create(request: CreateTaskRequest): Observable<SiteTask> {
        this.calls.push(`create ${request.text}`);
        this.lastCreate = request;
        return (
            this.createResult ??
            of(testTask({ id: 'new', createdAt: '2026-11-17T09:00:00Z', ...request, assigneeName: null }))
        );
    }

    update(id: string, request: UpdateTaskRequest): Observable<SiteTask> {
        this.calls.push(`update ${id} ${request.text}`);
        this.lastUpdate = request;
        return this.updateResult ?? of({ ...this.known(id), ...request });
    }

    setDone(id: string, done: boolean): Observable<SiteTask> {
        this.calls.push(`done ${id} ${done}`);
        return this.setDoneResult ?? of({ ...this.known(id), done, doneAt: done ? '2026-11-17T10:00:00Z' : null });
    }

    private known(id: string): SiteTask {
        return this.tasks.find((task) => task.id === id) ?? testTask({ id });
    }

    remove(id: string): Observable<void> {
        this.calls.push(`remove ${id}`);
        return this.removeResult;
    }
}
