import { HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { CreateUserRequest, SetPasswordRequest, UserAccount } from '../user.model';

/** Test data and a hand-written UsersService fake shared by the specs. */
export function testUser(overrides: Partial<UserAccount> = {}): UserAccount {
    return { id: 'u1', userName: 'iva', role: 'Owner', companyId: null, companyName: null, ...overrides };
}

export function httpError(status: number, error: unknown = null): Observable<never> {
    return throwError(() => new HttpErrorResponse({ status, error }));
}

export class FakeUsersService {
    listResult: Observable<UserAccount[]> = of([
        testUser({ id: 'u1', userName: 'iva', role: 'Owner' }),
        testUser({ id: 'u2', userName: 'pesho', role: 'Manager' }),
        testUser({ id: 'u3', userName: 'zheni', role: 'ClientViewer', companyId: 'c1', companyName: 'Женистрой' }),
    ]);
    createResult: Observable<UserAccount> | null = null;
    setPasswordResult: Observable<void> = of(undefined);
    removeResult: Observable<void> = of(undefined);
    calls: string[] = [];
    lastCreate: CreateUserRequest | null = null;
    lastPassword: SetPasswordRequest | null = null;

    list(): Observable<UserAccount[]> {
        this.calls.push('list');
        return this.listResult;
    }

    create(request: CreateUserRequest): Observable<UserAccount> {
        this.calls.push(`create ${request.userName}`);
        this.lastCreate = request;
        const companyName = request.companyId ? 'Женистрой' : null;
        return (
            this.createResult ??
            of(
                testUser({
                    id: 'new',
                    userName: request.userName,
                    role: request.role,
                    companyId: request.companyId,
                    companyName,
                }),
            )
        );
    }

    setPassword(id: string, request: SetPasswordRequest): Observable<void> {
        this.calls.push(`password ${id}`);
        this.lastPassword = request;
        return this.setPasswordResult;
    }

    remove(id: string): Observable<void> {
        this.calls.push(`remove ${id}`);
        return this.removeResult;
    }
}
