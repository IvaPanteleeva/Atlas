import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { PortalService } from './portal.service';
import { testPortalSite, testPortalSiteListItem, testPortalTotals } from './testing/fake-portal.service';

describe('PortalService', () => {
    let service: PortalService;
    let controller: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
        service = TestBed.inject(PortalService);
        controller = TestBed.inject(HttpTestingController);
    });

    afterEach(() => controller.verify());

    it('GET /portal/sites', async () => {
        const result = firstValueFrom(service.sites());
        const body = { sites: [testPortalSiteListItem()], totals: testPortalTotals() };
        const request = controller.expectOne('/portal/sites');
        expect(request.request.method).toBe('GET');
        request.flush(body);

        expect(await result).toEqual(body);
    });

    it('GET /portal/sites/{id}', async () => {
        const result = firstValueFrom(service.site('s1'));
        const request = controller.expectOne('/portal/sites/s1');
        expect(request.request.method).toBe('GET');
        request.flush(testPortalSite());

        expect(await result).toEqual(testPortalSite());
    });

    it('drops fields the portal does not know, even if the server sent them by mistake', async () => {
        const result = firstValueFrom(service.site('s1'));
        const site = testPortalSite();
        controller.expectOne('/portal/sites/s1').flush({
            ...site,
            note: 'вътрешна бележка',
            companyName: 'Женистрой',
            summary: { ...site.summary, workerCost: 100, net: 50 },
            workDays: site.workDays.map((day) => ({
                ...day,
                note: 'вътрешна',
                workers: [{ id: 'p1', name: 'Иван', pay: 80 }],
                items: day.items.map((item) => ({ ...item, id: 'i1', serviceId: 'sv1' })),
            })),
            payments: site.payments.map((payment) => ({ ...payment, id: 'p1', note: 'кеш' })),
        });

        expect(await result).toEqual(testPortalSite());
    });

    it('the list also keeps only the known fields', async () => {
        const result = firstValueFrom(service.sites());
        controller.expectOne('/portal/sites').flush({
            sites: [{ ...testPortalSiteListItem(), workerCost: 10, net: 5, address: 'x' }],
            totals: { ...testPortalTotals(), workerCost: 10 },
        });

        expect(await result).toEqual({ sites: [testPortalSiteListItem()], totals: testPortalTotals() });
    });
});
