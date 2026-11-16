import { HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { SaveServiceRequest, Service } from '../service.model';

/** Test data and a hand-written ServicesService fake shared by the specs. */
export function testService(overrides: Partial<Service> = {}): Service {
    return { id: 'sv1', name: 'Шпакловка', unit: 'SquareMeter', defaultRate: 6, ...overrides };
}

export function httpError(status: number, error: unknown = null): Observable<never> {
    return throwError(() => new HttpErrorResponse({ status, error }));
}

export class FakeServicesService {
    listResult: Observable<Service[]> = of([
        testService({ id: 'sv1', name: 'Боядисване', unit: 'SquareMeter', defaultRate: 6 }),
        testService({ id: 'sv2', name: 'Шпакловка', unit: 'SquareMeter', defaultRate: null }),
    ]);
    createResult: Observable<Service> | null = null;
    updateResult: Observable<Service> | null = null;
    removeResult: Observable<void> = of(undefined);
    calls: string[] = [];
    lastRequest: SaveServiceRequest | null = null;

    list(): Observable<Service[]> {
        this.calls.push('list');
        return this.listResult;
    }

    /** Echoes the request back as the created service unless createResult is set. */
    create(request: SaveServiceRequest): Observable<Service> {
        this.calls.push(`create ${request.name}`);
        this.lastRequest = request;
        return this.createResult ?? of({ id: 'new', ...request });
    }

    update(id: string, request: SaveServiceRequest): Observable<Service> {
        this.calls.push(`update ${id} ${request.name}`);
        this.lastRequest = request;
        return this.updateResult ?? of({ id, ...request });
    }

    remove(id: string): Observable<void> {
        this.calls.push(`remove ${id}`);
        return this.removeResult;
    }
}
