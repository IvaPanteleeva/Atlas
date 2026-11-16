/** A row of GET /api/sites/{siteId}/contacts (by name): someone to call about this site. */
export interface SiteContact {
    id: string;
    siteId: string;
    name: string;
    /** Free text, e.g. „собственик“, „съсед“. */
    role: string | null;
    phone: string | null;
    note: string | null;
}

/** Body for POST /api/sites/{siteId}/contacts and PUT /api/sites/{siteId}/contacts/{id}. */
export interface SaveSiteContactRequest {
    name: string;
    role: string | null;
    phone: string | null;
    note: string | null;
}

/** Same limits as the API. */
export const SITE_CONTACT_LIMITS = {
    name: 100,
    role: 50,
    phone: 30,
    note: 500,
} as const;
