/** A row of GET /api/tasks: open ones first (oldest first), then the done ones (last done first). */
export interface SiteTask {
    id: string;
    text: string;
    done: boolean;
    /** null = a task of the company („Фирмата“), not of a site. */
    siteId: string | null;
    /** One crew person, optional. A former crew member stays until changed. */
    assigneeId: string | null;
    assigneeName: string | null;
    /** ISO date-time. */
    createdAt: string;
    /** ISO date-time; null while open. */
    doneAt: string | null;
}

/** Filter for GET /api/tasks. Nothing set = every task. */
export interface TaskQuery {
    siteId?: string;
    assigneeId?: string;
    /** Only the tasks without a site. */
    scope?: 'company';
}

/** Body for POST /api/tasks. */
export interface CreateTaskRequest {
    text: string;
    siteId: string | null;
    assigneeId: string | null;
}

/** Body for PUT /api/tasks/{id}. */
export interface UpdateTaskRequest {
    text: string;
    assigneeId: string | null;
}

/** Emitted by the add row: the page adds the site. */
export interface NewTask {
    text: string;
    assigneeId: string | null;
}

/** Same limit as the API. */
export const TASK_TEXT_MAX = 500;

/** Done tasks shown before „Покажи всички завършени (N)“. */
export const DONE_SHOWN = 5;
