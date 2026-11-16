/** A row of GET /api/people (ordered by name). */
export interface Person {
    id: string;
    name: string;
    phone: string | null;
    /** Internal note (Owner and Manager). */
    note: string | null;
    /** „Работи с нас“: crew (picked in work days); the others are contacts. */
    isCrew: boolean;
    /** Distinct dates this person worked (two sites on one day = 1 day), counted by the server. */
    dayCount: number;
    /** yyyy-MM-dd of the last work day; null = never. */
    lastWorkDate: string | null;
}

/** Body for POST /api/people and PUT /api/people/{id}. */
export interface SavePersonRequest {
    name: string;
    phone: string | null;
    note: string | null;
    isCrew: boolean;
}

/** Same limits as the API. */
export const PERSON_LIMITS = {
    name: 100,
    phone: 30,
    note: 500,
} as const;
