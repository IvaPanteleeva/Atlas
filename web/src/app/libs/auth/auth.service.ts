import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { LoginRequest, Me } from './auth.model';

/** Auth endpoints. The session is an HttpOnly cookie on the same origin, so nothing is stored here. */
@Injectable({ providedIn: 'root' })
export class AuthService {
    private readonly http = inject(HttpClient);

    login(request: LoginRequest): Observable<Me> {
        return this.http.post<Me>('/api/auth/login', request);
    }

    logout(): Observable<void> {
        return this.http.post<void>('/api/auth/logout', null);
    }

    me(): Observable<Me> {
        return this.http.get<Me>('/api/auth/me');
    }
}
