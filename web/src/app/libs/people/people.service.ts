import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Person, SavePersonRequest } from './person.model';

/** People endpoints. Owner and Manager read and write; only Owner deletes (the API checks it). */
@Injectable({ providedIn: 'root' })
export class PeopleService {
    private readonly http = inject(HttpClient);

    list(): Observable<Person[]> {
        return this.http.get<Person[]>('/api/people');
    }

    create(request: SavePersonRequest): Observable<Person> {
        return this.http.post<Person>('/api/people', request);
    }

    update(id: string, request: SavePersonRequest): Observable<Person> {
        return this.http.put<Person>(`/api/people/${id}`, request);
    }

    remove(id: string): Observable<void> {
        return this.http.delete<void>(`/api/people/${id}`);
    }
}
