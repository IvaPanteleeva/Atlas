import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { CompaniesService } from './companies.service';

describe('CompaniesService', () => {
    let service: CompaniesService;
    let http: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
        service = TestBed.inject(CompaniesService);
        http = TestBed.inject(HttpTestingController);
    });

    afterEach(() => http.verify());

    it('list GETs /api/companies', async () => {
        const result = firstValueFrom(service.list());
        http.expectOne({ method: 'GET', url: '/api/companies' }).flush([{ id: 'a', name: 'Женистрой' }]);
        expect(await result).toEqual([{ id: 'a', name: 'Женистрой' }]);
    });

    it('create POSTs the name', async () => {
        const result = firstValueFrom(service.create({ name: 'Нова' }));
        const request = http.expectOne({ method: 'POST', url: '/api/companies' });
        expect(request.request.body).toEqual({ name: 'Нова' });
        request.flush({ id: 'n', name: 'Нова' });
        expect(await result).toEqual({ id: 'n', name: 'Нова' });
    });

    it('update PUTs to /api/companies/{id}', async () => {
        const result = firstValueFrom(service.update('a', { name: 'Друга' }));
        const request = http.expectOne({ method: 'PUT', url: '/api/companies/a' });
        expect(request.request.body).toEqual({ name: 'Друга' });
        request.flush({ id: 'a', name: 'Друга' });
        expect(await result).toEqual({ id: 'a', name: 'Друга' });
    });

    it('remove DELETEs /api/companies/{id}', async () => {
        const result = firstValueFrom(service.remove('a'));
        http.expectOne({ method: 'DELETE', url: '/api/companies/a' }).flush(null, {
            status: 204,
            statusText: 'No Content',
        });
        await result;
    });
});
