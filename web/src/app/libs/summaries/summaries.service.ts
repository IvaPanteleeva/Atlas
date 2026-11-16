import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { SiteSummary, SitesSummaries } from './summary.model';

/** Money summaries computed by the server. Owner only (the API answers 403 to Crew). */
@Injectable({ providedIn: 'root' })
export class SummariesService {
    private readonly http = inject(HttpClient);

    getSite(siteId: string): Observable<SiteSummary> {
        return this.http.get<SiteSummary>(`/api/sites/${siteId}/summary`);
    }

    getAll(): Observable<SitesSummaries> {
        return this.http.get<SitesSummaries>('/api/sites/summaries');
    }
}
