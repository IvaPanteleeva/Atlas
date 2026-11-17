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

/** Who worked on a day (ordered by name by the server), with their pay for that day. */
export interface WorkDayWorker {
    id: string;
    name: string;
    /** Pay for the day (own copy, prefilled from the daily rate); null = „за уточняване“. */
    pay: number | null;
    /** Partner: the pay is optional; empty = shared profit (not a cost, not „за уточняване“). */
    isPartner: boolean;
}

/** One worker of the save body. A partner's pay is null unless a day's wage was written. */
export interface SaveWorkDayWorkerRequest {
    personId: string;
    pay: number | null;
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
    workers: WorkDayWorker[];
    /** Sum of the items with a price, computed by the server. */
    total: number;
    /** Items still to be clarified, counted by the server. */
    openCount: number;
}

/** One item of the save body. Existing items send their id, so their stored prices can stay. */
export interface SaveWorkItemRequest {
    id: string | null;
    serviceId: string | null;
    description: string;
    quantity: number;
    unit: Unit;
    /** null without canEditPrices: the server then resolves the price itself. */
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
    /** People who worked that day with their pay. New ones must be crew; ones already on the day stay valid. */
    workers: SaveWorkDayWorkerRequest[];
}

/** Same limits as the API. */
export const WORK_DAY_LIMITS = {
    title: 200,
    note: 2000,
    description: 200,
    maxItems: 50,
    quantityMax: 100000,
} as const;
