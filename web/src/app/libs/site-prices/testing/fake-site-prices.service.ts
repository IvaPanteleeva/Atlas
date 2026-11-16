import { Observable, of } from 'rxjs';
import { CreateSitePriceRequest, SitePrice, UpdateSitePriceRequest } from '../site-price.model';

/** Test data and a hand-written SitePricesService fake shared by the specs. */
export function testSitePrice(overrides: Partial<SitePrice> = {}): SitePrice {
    return {
        id: 'p1',
        serviceId: 'sv1',
        serviceName: 'Боядисване',
        unit: 'SquareMeter',
        rate: 6.5,
        defaultRate: 6,
        ...overrides,
    };
}

export class FakeSitePricesService {
    listResult: Observable<SitePrice[]> = of([]);
    createResult: Observable<SitePrice> | null = null;
    updateResult: Observable<SitePrice> | null = null;
    removeResult: Observable<void> = of(undefined);
    calls: string[] = [];
    lastCreate: CreateSitePriceRequest | null = null;
    lastUpdate: UpdateSitePriceRequest | null = null;

    list(siteId: string): Observable<SitePrice[]> {
        this.calls.push(`list ${siteId}`);
        return this.listResult;
    }

    create(siteId: string, request: CreateSitePriceRequest): Observable<SitePrice> {
        this.calls.push(`create ${siteId} ${request.serviceId} ${request.rate}`);
        this.lastCreate = request;
        return (
            this.createResult ??
            of(testSitePrice({ id: 'new', serviceId: request.serviceId, serviceName: 'Нова', rate: request.rate }))
        );
    }

    update(siteId: string, id: string, request: UpdateSitePriceRequest): Observable<SitePrice> {
        this.calls.push(`update ${siteId} ${id} ${request.rate}`);
        this.lastUpdate = request;
        return this.updateResult ?? of(testSitePrice({ id, rate: request.rate }));
    }

    remove(siteId: string, id: string): Observable<void> {
        this.calls.push(`remove ${siteId} ${id}`);
        return this.removeResult;
    }
}
