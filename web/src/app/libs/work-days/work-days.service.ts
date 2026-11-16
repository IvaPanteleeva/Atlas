import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { SaveWorkDayRequest, WorkDay } from './work-day.model';

/** Work days of one site. Owner and Crew read and write; only Owner deletes (the API checks it). */
@Injectable({ providedIn: 'root' })
export class WorkDaysService {
    private readonly http = inject(HttpClient);

    list(siteId: string): Observable<WorkDay[]> {
        return this.http.get<WorkDay[]>(`/api/sites/${siteId}/workdays`);
    }

    create(siteId: string, request: SaveWorkDayRequest): Observable<WorkDay> {
        return this.http.post<WorkDay>(`/api/sites/${siteId}/workdays`, request);
    }

    update(siteId: string, id: string, request: SaveWorkDayRequest): Observable<WorkDay> {
        return this.http.put<WorkDay>(`/api/sites/${siteId}/workdays/${id}`, request);
    }

    remove(siteId: string, id: string): Observable<void> {
        return this.http.delete<void>(`/api/sites/${siteId}/workdays/${id}`);
    }
}
