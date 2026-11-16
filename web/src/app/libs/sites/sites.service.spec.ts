import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { SaveSiteRequest, Site } from './site.model';
import { SitesService } from './sites.service';

const site: Site = {
    id: 's1',
    name: 'Професора',
    status: 'Lead',
    companyId: null,
    companyName: null,
    endCustomer: 'Професорът',
    address: null,
    mapUrl: null,
    note: null,
};

const request: SaveSiteRequest = {
    name: 'Професора',
    status: 'Lead',
    companyId: null,
    endCustomer: 'Професорът',
    address: null,
    mapUrl: null,
    note: null,
};

describe('SitesService', () => {
    let service: SitesService;
    let http: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
        service = TestBed.inject(SitesService);
        http = TestBed.inject(HttpTestingController);
    });

    afterEach(() => http.verify());

    it('list GETs /api/sites', async () => {
        const result = firstValueFrom(service.list());
        http.expectOne({ method: 'GET', url: '/api/sites' }).flush([]);
        expect(await result).toEqual([]);
    });

    it('get GETs /api/sites/{id}', async () => {
        const result = firstValueFrom(service.get('s1'));
        http.expectOne({ method: 'GET', url: '/api/sites/s1' }).flush(site);
        expect(await result).toEqual(site);
    });

    it('create POSTs the request', async () => {
        const result = firstValueFrom(service.create(request));
        const call = http.expectOne({ method: 'POST', url: '/api/sites' });
        expect(call.request.body).toEqual(request);
        call.flush(site);
        expect(await result).toEqual(site);
    });

    it('update PUTs to /api/sites/{id}', async () => {
        const result = firstValueFrom(service.update('s1', request));
        const call = http.expectOne({ method: 'PUT', url: '/api/sites/s1' });
        expect(call.request.body).toEqual(request);
        call.flush(site);
        expect(await result).toEqual(site);
    });

    it('remove DELETEs /api/sites/{id}', async () => {
        const result = firstValueFrom(service.remove('s1'));
        http.expectOne({ method: 'DELETE', url: '/api/sites/s1' }).flush(null, {
            status: 204,
            statusText: 'No Content',
        });
        await result;
    });
});
