import { Unit } from '../shared/units';

/** A position of a work day, as returned by the API. The amount comes ready from the server. */
export interface WorkItem {
    id: string;
    /** The service the item came from (for reference only); null = free text. */
    serviceId: string | null;
    description: string;
    quantity: number;
    unit: Unit;
    /** Own copy of the price; null = "цена?" (not in the sums). */
    rate: number | null;
    needsReview: boolean;
    /** quantity × rate, computed by the server; null when there is no rate. */
    amount: number | null;
}

/** GET /api/sites/{siteId}/workdays (newest first) and the response of POST/PUT. */
export interface WorkDay {
    id: string;
    siteId: string;
    /** yyyy-MM-dd */
    date: string;
    title: string | null;
    note: string | null;
    items: WorkItem[];
    /** Sum of the items with a price, computed by the server. */
    total: number;
    /** Items still to be clarified, counted by the server. */
    openCount: number;
}

/** One item of the save body. Existing items send their id, so prices kept for Crew stay. */
export interface SaveWorkItemRequest {
    id: string | null;
    serviceId: string | null;
    description: string;
    quantity: number;
    unit: Unit;
    /** Ignored by the server for Crew (it resolves the price itself); the form sends null then. */
    rate: number | null;
    needsReview: boolean;
}

/** Body for POST /api/sites/{siteId}/workdays and PUT /api/sites/{siteId}/workdays/{id}. */
export interface SaveWorkDayRequest {
    /** yyyy-MM-dd, today or earlier. */
    date: string;
    title: string | null;
    note: string | null;
    items: SaveWorkItemRequest[];
}

/** Same limits as the API. */
export const WORK_DAY_LIMITS = {
    title: 200,
    note: 2000,
    description: 200,
    maxItems: 50,
    quantityMax: 100000,
} as const;
