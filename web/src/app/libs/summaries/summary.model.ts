/** Money of one site, computed by the server (GET /api/sites/{siteId}/summary). Owner and Manager. */
export interface SiteSummary {
    /** Sum of the items with a price (also those to be clarified). */
    earned: number;
    /** Sum of the client payments. */
    paid: number;
    /** earned − paid; negative = overpaid. */
    remaining: number;
    /** Items without a price or flagged „за уточняване“. */
    openCount: number;
    /** Σ pay of the people on the site's work days. */
    workerCost: number;
    /** Workers on the site's days without a pay („за уточняване“). */
    workerOpenCount: number;
    /** earned − workerCost: „Остава за нас“ (from the earned money, not the paid). */
    net: number;
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
    workerCost: number;
    workerOpenCount: number;
    /** earned − workerCost over all sites. */
    net: number;
}

/** One site in GET /api/sites/summaries. */
export interface SiteSummaryRow {
    siteId: string;
    earned: number;
    paid: number;
    /** earned − paid; negative = overpaid. */
    remaining: number;
    openCount: number;
    workerCost: number;
    workerOpenCount: number;
    net: number;
}

/** GET /api/sites/summaries. Owner and Manager. */
export interface SitesSummaries {
    totals: SummaryTotals;
    sites: SiteSummaryRow[];
}

/** Per-site money by site id, for the sites list. */
export type SummariesBySite = Record<string, SiteSummaryRow>;
