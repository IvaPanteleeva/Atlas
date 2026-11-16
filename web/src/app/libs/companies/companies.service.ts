import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Company, SaveCompanyRequest } from './company.model';

/** Company endpoints. Reading is for Owner and Crew; writing is Owner only (the API checks it). */
@Injectable({ providedIn: 'root' })
export class CompaniesService {
    private readonly http = inject(HttpClient);

    list(): Observable<Company[]> {
        return this.http.get<Company[]>('/api/companies');
    }

    create(request: SaveCompanyRequest): Observable<Company> {
        return this.http.post<Company>('/api/companies', request);
    }

    update(id: string, request: SaveCompanyRequest): Observable<Company> {
        return this.http.put<Company>(`/api/companies/${id}`, request);
    }

    remove(id: string): Observable<void> {
        return this.http.delete<void>(`/api/companies/${id}`);
    }
}
