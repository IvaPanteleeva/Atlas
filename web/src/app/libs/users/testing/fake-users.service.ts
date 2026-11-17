import { HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { CreateUserRequest, LinkPersonRequest, SetPasswordRequest, UserAccount } from '../user.model';

/** Test data and a hand-written UsersService fake shared by the specs. */
export function testUser(overrides: Partial<UserAccount> = {}): UserAccount {
    return {
        id: 'u1',
        userName: 'iva',
        role: 'Owner',
        companyId: null,
        companyName: null,
        personId: null,
        personName: null,
        ...overrides,
    };
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
    linkResult: Observable<UserAccount> | null = null;
    /** Names for the echoed link reply. */
    personNames: Record<string, string> = { p1: 'Гошо', p3: 'Пешо', p4: 'Ива' };
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
                    personId: request.personId,
                    personName: request.personId ? (this.personNames[request.personId] ?? request.personId) : null,
                }),
            )
        );
    }

    setPassword(id: string, request: SetPasswordRequest): Observable<void> {
        this.calls.push(`password ${id}`);
        this.lastPassword = request;
        return this.setPasswordResult;
    }

    /** Echoes the profile with the new person unless linkResult is set. */
    linkPerson(id: string, request: LinkPersonRequest): Observable<UserAccount> {
        this.calls.push(`person ${id} ${request.personId}`);
        const personName = request.personId ? (this.personNames[request.personId] ?? request.personId) : null;
        return (
            this.linkResult ??
            of(testUser({ id, userName: id, role: 'Manager', personId: request.personId, personName }))
        );
    }

    remove(id: string): Observable<void> {
        this.calls.push(`remove ${id}`);
        return this.removeResult;
    }
}
