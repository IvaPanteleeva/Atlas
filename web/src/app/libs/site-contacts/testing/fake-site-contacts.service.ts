import { HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { SaveSiteContactRequest, SiteContact } from '../site-contact.model';

/** Test data and a hand-written SiteContactsService fake shared by the specs. */
export function testSiteContact(overrides: Partial<SiteContact> = {}): SiteContact {
    return {
        id: 'sc1',
        siteId: 's1',
        name: 'Професорът',
        role: 'собственик',
        phone: '0888 555 666',
        note: null,
        ...overrides,
    };
}

export function httpError(status: number, error: unknown = null): Observable<never> {
    return throwError(() => new HttpErrorResponse({ status, error }));
}

export class FakeSiteContactsService {
    listResult: Observable<SiteContact[]> = of([]);
    createResult: Observable<SiteContact> | null = null;
    updateResult: Observable<SiteContact> | null = null;
    removeResult: Observable<void> = of(undefined);
    calls: string[] = [];
    lastRequest: SaveSiteContactRequest | null = null;

    list(siteId: string): Observable<SiteContact[]> {
        this.calls.push(`list ${siteId}`);
        return this.listResult;
    }

    create(siteId: string, request: SaveSiteContactRequest): Observable<SiteContact> {
        this.calls.push(`create ${siteId} ${request.name}`);
        this.lastRequest = request;
        return this.createResult ?? of(testSiteContact({ id: 'new', siteId, ...request }));
    }

    update(siteId: string, id: string, request: SaveSiteContactRequest): Observable<SiteContact> {
        this.calls.push(`update ${siteId} ${id} ${request.name}`);
        this.lastRequest = request;
        return this.updateResult ?? of(testSiteContact({ id, siteId, ...request }));
    }

    remove(siteId: string, id: string): Observable<void> {
        this.calls.push(`remove ${siteId} ${id}`);
        return this.removeResult;
    }
}
