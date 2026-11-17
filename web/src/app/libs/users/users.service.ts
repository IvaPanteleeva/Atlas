import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateUserRequest, LinkPersonRequest, SetPasswordRequest, UserAccount } from './user.model';

/** Login profile endpoints (Owner only; the API checks it). */
@Injectable({ providedIn: 'root' })
export class UsersService {
    private readonly http = inject(HttpClient);

    list(): Observable<UserAccount[]> {
        return this.http.get<UserAccount[]>('/api/users');
    }

    create(request: CreateUserRequest): Observable<UserAccount> {
        return this.http.post<UserAccount>('/api/users', request);
    }

    setPassword(id: string, request: SetPasswordRequest): Observable<void> {
        return this.http.put<void>(`/api/users/${id}/password`, request);
    }

    linkPerson(id: string, request: LinkPersonRequest): Observable<UserAccount> {
        return this.http.put<UserAccount>(`/api/users/${id}/person`, request);
    }

    remove(id: string): Observable<void> {
        return this.http.delete<void>(`/api/users/${id}`);
    }
}
