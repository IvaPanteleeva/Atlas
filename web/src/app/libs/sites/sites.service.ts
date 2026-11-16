import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { SaveSiteRequest, Site, SiteListItem } from './site.model';

/** Site endpoints. Owner and Crew read and write; only Owner deletes (the API checks it). */
@Injectable({ providedIn: 'root' })
export class SitesService {
    private readonly http = inject(HttpClient);

    list(): Observable<SiteListItem[]> {
        return this.http.get<SiteListItem[]>('/api/sites');
    }

    get(id: string): Observable<Site> {
        return this.http.get<Site>(`/api/sites/${id}`);
    }

    create(request: SaveSiteRequest): Observable<Site> {
        return this.http.post<Site>('/api/sites', request);
    }

    update(id: string, request: SaveSiteRequest): Observable<Site> {
        return this.http.put<Site>(`/api/sites/${id}`, request);
    }

    remove(id: string): Observable<void> {
        return this.http.delete<void>(`/api/sites/${id}`);
    }
}
