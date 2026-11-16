import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { SaveServiceRequest, Service } from './service.model';

/** Service endpoints. Owner and Crew read; only Owner writes (the API checks it). */
@Injectable({ providedIn: 'root' })
export class ServicesService {
    private readonly http = inject(HttpClient);

    list(): Observable<Service[]> {
        return this.http.get<Service[]>('/api/services');
    }

    create(request: SaveServiceRequest): Observable<Service> {
        return this.http.post<Service>('/api/services', request);
    }

    update(id: string, request: SaveServiceRequest): Observable<Service> {
        return this.http.put<Service>(`/api/services/${id}`, request);
    }

    remove(id: string): Observable<void> {
        return this.http.delete<void>(`/api/services/${id}`);
    }
}
