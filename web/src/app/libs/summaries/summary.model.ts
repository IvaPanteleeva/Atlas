/** Money of one site, computed by the server (GET /api/sites/{siteId}/summary). Owner only. */
export interface SiteSummary {
    /** Sum of the items with a price (also those to be clarified). */
    earned: number;
    /** Sum of the client payments. */
    paid: number;
    /** earned − paid; negative = overpaid. */
    remaining: number;
    /** Items without a price or flagged „за уточняване“. */
    openCount: number;
    workDayCount: number;
    /** yyyy-MM-dd; null when the site has no work days. */
    lastWorkDate: string | null;
}

/**
 * The money of all sites together. „Дължат ни“ (owed) adds up only what sites still owe: an overpaid site does
 * not lower another site's debt. The overpaid money is shown apart („Надплатено X € по N обекта“).
 */
export interface SummaryTotals {
    earned: number;
    paid: number;
    /** Sum of the positive remaining amounts. */
    owed: number;
    /** Sum of the overpaid amounts, as a positive number. */
    overpaid: number;
    /** How many sites are overpaid. */
    overpaidSiteCount: number;
    openCount: number;
}

/** One site in GET /api/sites/summaries. */
export interface SiteSummaryRow {
    siteId: string;
    earned: number;
    paid: number;
    /** earned − paid; negative = overpaid. */
    remaining: number;
    openCount: number;
}

/** GET /api/sites/summaries. Owner only. */
export interface SitesSummaries {
    totals: SummaryTotals;
    sites: SiteSummaryRow[];
}

/** Per-site money by site id, for the sites list. */
export type SummariesBySite = Record<string, SiteSummaryRow>;
