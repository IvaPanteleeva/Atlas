import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateSitePriceRequest, SitePrice, UpdateSitePriceRequest } from './site-price.model';

/** Agreed prices of one site. Owner and Crew read; only Owner writes (the API checks it). */
@Injectable({ providedIn: 'root' })
export class SitePricesService {
    private readonly http = inject(HttpClient);

    list(siteId: string): Observable<SitePrice[]> {
        return this.http.get<SitePrice[]>(`/api/sites/${siteId}/prices`);
    }

    create(siteId: string, request: CreateSitePriceRequest): Observable<SitePrice> {
        return this.http.post<SitePrice>(`/api/sites/${siteId}/prices`, request);
    }

    update(siteId: string, id: string, request: UpdateSitePriceRequest): Observable<SitePrice> {
        return this.http.put<SitePrice>(`/api/sites/${siteId}/prices/${id}`, request);
    }

    remove(siteId: string, id: string): Observable<void> {
        return this.http.delete<void>(`/api/sites/${siteId}/prices/${id}`);
    }
}
