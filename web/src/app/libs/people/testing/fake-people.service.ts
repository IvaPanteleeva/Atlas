import { HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { Person, SavePersonRequest } from '../person.model';

/** Test data and a hand-written PeopleService fake shared by the specs. */
export function testPerson(overrides: Partial<Person> = {}): Person {
    return {
        id: 'p1',
        name: 'Пешо',
        phone: '0888 123 456',
        note: null,
        isCrew: true,
        dayCount: 0,
        lastWorkDate: null,
        dailyRate: null,
        earned: 0,
        isPartner: false,
        linkedUserName: null,
        ...overrides,
    };
}

export function httpError(status: number, error: unknown = null): Observable<never> {
    return throwError(() => new HttpErrorResponse({ status, error }));
}

export class FakePeopleService {
    listResult: Observable<Person[]> = of([
        testPerson({ id: 'p1', name: 'Гошо', phone: '0888 111 222', dayCount: 12, lastWorkDate: '2026-11-16' }),
        testPerson({ id: 'p2', name: 'Иван бояджия', phone: null, note: 'Само боя', isCrew: false }),
        testPerson({ id: 'p3', name: 'Пешо', phone: '0899 333 444', note: 'Има кола', dayCount: 1 }),
    ]);
    createResult: Observable<Person> | null = null;
    updateResult: Observable<Person> | null = null;
    removeResult: Observable<void> = of(undefined);
    calls: string[] = [];
    lastRequest: SavePersonRequest | null = null;

    list(): Observable<Person[]> {
        this.calls.push('list');
        return this.listResult;
    }

    /** Echoes the request back as the saved person unless createResult is set. */
    create(request: SavePersonRequest): Observable<Person> {
        this.calls.push(`create ${request.name}`);
        this.lastRequest = request;
        return this.createResult ?? of(testPerson({ id: 'new', ...request }));
    }

    update(id: string, request: SavePersonRequest): Observable<Person> {
        this.calls.push(`update ${id} ${request.name}`);
        this.lastRequest = request;
        return this.updateResult ?? of(testPerson({ id, ...request }));
    }

    remove(id: string): Observable<void> {
        this.calls.push(`remove ${id}`);
        return this.removeResult;
    }
}
