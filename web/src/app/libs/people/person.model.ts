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
    /** Suggested pay for one day (prefilled in the work day form); null = none. */
    dailyRate: number | null;
    /** Σ pay of their work days, computed by the server. */
    earned: number;
    /**
     * Partner („съдружник“), computed by the server: crew linked to an Owner or Manager login profile.
     * Their days are not a cost (no pay for the day).
     */
    isPartner: boolean;
    /** The login profile linked to this person; null = none. */
    linkedUserName: string | null;
}

/** Body for POST /api/people and PUT /api/people/{id}. */
export interface SavePersonRequest {
    name: string;
    phone: string | null;
    note: string | null;
    isCrew: boolean;
    dailyRate: number | null;
}

/** Same limits as the API. */
export const PERSON_LIMITS = {
    name: 100,
    phone: 30,
    note: 500,
} as const;
