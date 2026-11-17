import { Unit } from '../shared/units';
import { SiteStatus } from '../sites/site.model';

/**
 * The client portal (ClientViewer). Separate types from the internal ones on purpose: the /portal/... API has no
 * notes, contacts, people, pay, tasks or site prices, so these types cannot show them either.
 * All amounts come ready from the server; the portal only formats them.
 */

/** One site in GET /portal/sites. */
export interface PortalSiteListItem {
    id: string;
    name: string;
    endCustomer: string | null;
    status: SiteStatus;
    earned: number;
    paid: number;
    /** earned − paid; negative = overpaid. */
    remaining: number;
    /** Items without a price or flagged „за уточняване“. */
    openCount: number;
}

/** The money of all the client's sites. „Дължите“ (owed) adds up only the positive remaining amounts. */
export interface PortalTotals {
    owed: number;
    /** Sum of the overpaid amounts, as a positive number. */
    overpaid: number;
    overpaidSiteCount: number;
}

/** GET /portal/sites. */
export interface PortalSites {
    sites: PortalSiteListItem[];
    totals: PortalTotals;
}

export interface PortalSummary {
    earned: number;
    paid: number;
    remaining: number;
    openCount: number;
}

export interface PortalWorkItem {
    description: string;
    quantity: number;
    unit: Unit;
    /** null = „цена?“ (not in the sums). */
    rate: number | null;
    needsReview: boolean;
    /** quantity × rate from the server; null without a rate. */
    amount: number | null;
}

export interface PortalWorkDay {
    id: string;
    /** yyyy-MM-dd */
    date: string;
    title: string | null;
    total: number;
    items: PortalWorkItem[];
}

export interface PortalPayment {
    /** yyyy-MM-dd */
    date: string;
    amount: number;
}

/** GET /portal/sites/{id}. */
export interface PortalSite {
    id: string;
    name: string;
    endCustomer: string | null;
    address: string | null;
    mapUrl: string | null;
    status: SiteStatus;
    summary: PortalSummary;
    workDays: PortalWorkDay[];
    payments: PortalPayment[];
}
