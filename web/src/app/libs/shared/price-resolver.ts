import { Service } from '../services/service.model';
import { SitePrice } from '../site-prices/site-price.model';

/**
 * Price of a service on a site, for prefilling the work day form: the site's agreed price, else the service's
 * default price, else null ("цена?"). Free text (no service) has no price. The server applies the same rule
 * and is the source of truth (when the form sends no price it resolves one itself).
 */
export function resolveRate(serviceId: string | null, sitePrices: SitePrice[], services: Service[]): number | null {
    if (!serviceId) {
        return null;
    }
    const sitePrice = sitePrices.find((price) => price.serviceId === serviceId);
    if (sitePrice) {
        return sitePrice.rate;
    }
    return services.find((service) => service.id === serviceId)?.defaultRate ?? null;
}
