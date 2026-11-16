import { Unit } from '../shared/units';

/** A row of GET /api/sites/{siteId}/prices: the agreed price of one service on one site. */
export interface SitePrice {
    id: string;
    serviceId: string;
    serviceName: string;
    unit: Unit;
    rate: number;
    /** The service's default price, for reference; null = none. */
    defaultRate: number | null;
}

/** Body for POST /api/sites/{siteId}/prices. */
export interface CreateSitePriceRequest {
    serviceId: string;
    rate: number;
}

/** Body for PUT /api/sites/{siteId}/prices/{priceId}. */
export interface UpdateSitePriceRequest {
    rate: number;
}
