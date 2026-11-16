import { HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { SaveSiteRequest, Site, SiteListItem } from '../site.model';

/** Test data and a hand-written SitesService fake shared by the page specs. */
export function testSite(overrides: Partial<Site> = {}): Site {
    return {
        id: 's1',
        name: 'Бункера',
        status: 'Active',
        companyId: 'c1',
        companyName: 'Женистрой',
        endCustomer: null,
        address: 'ул. Шипка 5',
        mapUrl: null,
        note: null,
        ...overrides,
    };
}

export function httpError(status: number, error: unknown = null): Observable<never> {
    return throwError(() => new HttpErrorResponse({ status, error }));
}

export class FakeSitesService {
    listResult: Observable<SiteListItem[]> = of([
        {
            id: 's1',
            name: 'Бункера',
            status: 'Active',
            companyName: 'Женистрой',
            endCustomer: null,
            address: 'ул. Шипка 5',
            lastWorkDate: '2026-11-14',
        },
        {
            id: 's2',
            name: 'Професора',
            status: 'Lead',
            companyName: null,
            endCustomer: 'Професорът',
            address: null,
            lastWorkDate: null,
        },
    ]);
    getResult: Observable<Site> = of(testSite());
    createResult: Observable<Site> = of(
        testSite({ id: 'new', name: 'Нов', status: 'Lead', companyId: null, companyName: null }),
    );
    updateResult: Observable<Site> = of(testSite({ name: 'Бункера 2' }));
    removeResult: Observable<void> = of(undefined);
    calls: string[] = [];
    lastRequest: SaveSiteRequest | null = null;

    list(): Observable<SiteListItem[]> {
        this.calls.push('list');
        return this.listResult;
    }

    get(id: string): Observable<Site> {
        this.calls.push(`get ${id}`);
        return this.getResult;
    }

    create(request: SaveSiteRequest): Observable<Site> {
        this.calls.push(`create ${request.name}`);
        this.lastRequest = request;
        return this.createResult;
    }

    update(id: string, request: SaveSiteRequest): Observable<Site> {
        this.calls.push(`update ${id} ${request.name}`);
        this.lastRequest = request;
        return this.updateResult;
    }

    remove(id: string): Observable<void> {
        this.calls.push(`remove ${id}`);
        return this.removeResult;
    }
}
