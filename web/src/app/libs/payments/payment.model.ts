/** A payment from the client to Атлас for one site (GET /api/sites/{siteId}/payments, newest first). Owner and Manager. */
export interface Payment {
    id: string;
    siteId: string;
    /** yyyy-MM-dd */
    date: string;
    amount: number;
    note: string | null;
}

/** Body for POST /api/sites/{siteId}/payments and PUT /api/sites/{siteId}/payments/{id}. */
export interface SavePaymentRequest {
    /** yyyy-MM-dd, today or earlier. */
    date: string;
    amount: number;
    note: string | null;
}

/** Same limits as the API. */
export const PAYMENT_LIMITS = {
    amountMin: 0.01,
    amountMax: 1_000_000,
    note: 200,
} as const;
