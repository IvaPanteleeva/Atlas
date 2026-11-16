import { HttpErrorResponse } from '@angular/common/http';

/** Server validation messages by camelCase field name, first message only. */
export type FieldErrors = Record<string, string>;

function camelCase(key: string): string {
    return key ? key[0].toLowerCase() + key.slice(1) : key;
}

export function isNotFound(error: unknown): boolean {
    return error instanceof HttpErrorResponse && error.status === 404;
}

/** Splits an API error into per-field messages (400) and a general message (title or fallback). */
export function describeError(error: unknown, fallback: string): { error: string | null; fieldErrors: FieldErrors } {
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
