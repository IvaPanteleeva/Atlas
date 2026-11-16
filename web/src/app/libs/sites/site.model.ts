export type SiteStatus = 'Lead' | 'Active' | 'Done';

/** Labels from round 20; the order is the order in the status select. */
export const SITE_STATUS_LABELS: Record<SiteStatus, string> = {
    Lead: 'Оглед / оферта',
    Active: 'Текущ',
    Done: 'Приключен',
};

export const SITE_STATUSES: SiteStatus[] = ['Lead', 'Active', 'Done'];

/** A row of GET /api/sites. */
export interface SiteListItem {
    id: string;
    name: string;
    status: SiteStatus;
    companyName: string | null;
    endCustomer: string | null;
    address: string | null;
}

/** GET /api/sites/{id}, and the response of POST/PUT. */
export interface Site extends SiteListItem {
    companyId: string | null;
    mapUrl: string | null;
    note: string | null;
}

/** Body for POST /api/sites and PUT /api/sites/{id}. Empty optional fields are sent as null. */
export interface SaveSiteRequest {
    name: string;
    status: SiteStatus;
    companyId: string | null;
    endCustomer: string | null;
    address: string | null;
    mapUrl: string | null;
    note: string | null;
}

/** Server validation messages by camelCase field name, first message only. */
export type FieldErrors = Record<string, string>;

/** Same limits as the API (SiteConfiguration). */
export const SITE_MAX_LENGTHS = {
    name: 200,
    endCustomer: 200,
    address: 300,
    mapUrl: 500,
    note: 2000,
} as const;

/** Joins the non-empty parts of a meta line with " · ". */
export function joinMeta(...parts: (string | null | undefined)[]): string {
    return parts.filter((part): part is string => !!part?.trim()).join(' · ');
}
