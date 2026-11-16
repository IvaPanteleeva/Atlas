import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { httpError } from '../../services/testing/fake-services.service';
import { SitePricesService } from '../site-prices.service';
import { FakeSitePricesService, testSitePrice } from '../testing/fake-site-prices.service';
import { SitePricesStore } from './site-prices.store';

describe('SitePricesStore', () => {
    let fake: FakeSitePricesService;
    let store: InstanceType<typeof SitePricesStore>;

    const names = () => store.prices().map((p) => p.serviceName);

    beforeEach(() => {
        fake = new FakeSitePricesService();
        fake.listResult = of([
            testSitePrice({ id: 'p1', serviceId: 'sv1', serviceName: 'Боядисване' }),
            testSitePrice({ id: 'p2', serviceId: 'sv2', serviceName: 'Шпакловка', rate: 8 }),
        ]);
        TestBed.configureTestingModule({ providers: [{ provide: SitePricesService, useValue: fake }] });
        store = TestBed.inject(SitePricesStore);
    });

    it('starts empty', () => {
        expect(store.prices()).toEqual([]);
        expect(store.loading()).toBe(false);
        expect(store.saving()).toBe(false);
        expect(store.error()).toBeNull();
        expect(store.fieldErrors()).toEqual({});
    });

    it('load fills the prices of the site', async () => {
        await store.load('s1');

        expect(fake.calls).toEqual(['list s1']);
        expect(names()).toEqual(['Боядисване', 'Шпакловка']);
        expect(store.loading()).toBe(false);
    });

    it('load of another site forgets the previous prices first', async () => {
        await store.load('s1');
        fake.listResult = httpError(500);

        await store.load('s2');

        expect(store.prices()).toEqual([]);
        expect(store.error()).toBe('Грешка при зареждане на цените');
    });

    it('create adds the price sorted by service name (Bulgarian)', async () => {
        await store.load('s1');
        fake.createResult = of(testSitePrice({ id: 'p3', serviceId: 'sv3', serviceName: 'Грундиране', rate: 2 }));

        const created = await store.create('s1', { serviceId: 'sv3', rate: 2 });

        expect(created?.id).toBe('p3');
        expect(fake.calls).toContain('create s1 sv3 2');
        expect(names()).toEqual(['Боядисване', 'Грундиране', 'Шпакловка']);
        expect(store.saving()).toBe(false);
    });

    it('create 400 fills fieldErrors; 409 shows the title', async () => {
        fake.createResult = httpError(400, { errors: { Rate: ['Цената трябва да е положителна'] } });
        expect(await store.create('s1', { serviceId: 'sv3', rate: 0 })).toBeNull();
        expect(store.fieldErrors()).toEqual({ rate: 'Цената трябва да е положителна' });

        fake.createResult = httpError(409, { title: 'Тази услуга вече има цена за обекта' });
        expect(await store.create('s1', { serviceId: 'sv1', rate: 2 })).toBeNull();
        expect(store.error()).toBe('Тази услуга вече има цена за обекта');
        expect(store.fieldErrors()).toEqual({});
    });

    it('update replaces the price', async () => {
        await store.load('s1');

        const updated = await store.update('s1', 'p2', { rate: 9.25 });

        expect(updated?.rate).toBe(9.25);
        expect(fake.calls).toContain('update s1 p2 9.25');
        expect(store.prices().find((p) => p.id === 'p2')?.rate).toBe(9.25);
    });

    it('update error keeps the old price', async () => {
        await store.load('s1');
        fake.updateResult = httpError(500);

        expect(await store.update('s1', 'p2', { rate: 9 })).toBeNull();

        expect(store.error()).toBe('Грешка при записа');
        expect(store.prices().find((p) => p.id === 'p2')?.rate).toBe(8);
    });

    it('remove drops the price', async () => {
        await store.load('s1');

        expect(await store.remove('s1', 'p1')).toBe(true);

        expect(fake.calls).toContain('remove s1 p1');
        expect(names()).toEqual(['Шпакловка']);
    });

    it('remove 404 keeps the list and shows a message', async () => {
        await store.load('s1');
        fake.removeResult = httpError(404, { title: 'Not Found' });

        expect(await store.remove('s1', 'p1')).toBe(false);

        expect(store.error()).toBe('Цената вече я няма');
        expect(names()).toEqual(['Боядисване', 'Шпакловка']);
    });

    it('clearErrors hides the messages', async () => {
        fake.createResult = httpError(400, { errors: { rate: ['Въведи цена'] } });
        await store.create('s1', { serviceId: 'sv1', rate: 0 });

        store.clearErrors();

        expect(store.fieldErrors()).toEqual({});
        expect(store.error()).toBeNull();
    });
});
