import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { SaveSiteContactRequest, SiteContact } from './site-contact.model';

/** Contacts of a site. Owner and Manager read and write; only Owner deletes (the API checks it). */
@Injectable({ providedIn: 'root' })
export class SiteContactsService {
    private readonly http = inject(HttpClient);

    list(siteId: string): Observable<SiteContact[]> {
        return this.http.get<SiteContact[]>(`/api/sites/${siteId}/contacts`);
    }

    create(siteId: string, request: SaveSiteContactRequest): Observable<SiteContact> {
        return this.http.post<SiteContact>(`/api/sites/${siteId}/contacts`, request);
    }

    update(siteId: string, id: string, request: SaveSiteContactRequest): Observable<SiteContact> {
        return this.http.put<SiteContact>(`/api/sites/${siteId}/contacts/${id}`, request);
    }

    remove(siteId: string, id: string): Observable<void> {
        return this.http.delete<void>(`/api/sites/${siteId}/contacts/${id}`);
    }
}
