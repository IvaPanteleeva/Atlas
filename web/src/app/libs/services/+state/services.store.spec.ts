import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { SaveServiceRequest } from '../service.model';
import { ServicesService } from '../services.service';
import { FakeServicesService, httpError, testService } from '../testing/fake-services.service';
import { ServicesStore } from './services.store';

const request: SaveServiceRequest = { name: 'Грундиране', unit: 'SquareMeter', defaultRate: 2.5 };

describe('ServicesStore', () => {
    let fake: FakeServicesService;
    let store: InstanceType<typeof ServicesStore>;

    const names = () => store.services().map((s) => s.name);

    beforeEach(() => {
        fake = new FakeServicesService();
        TestBed.configureTestingModule({ providers: [{ provide: ServicesService, useValue: fake }] });
        store = TestBed.inject(ServicesStore);
    });

    it('starts empty', () => {
        expect(store.services()).toEqual([]);
        expect(store.loading()).toBe(false);
        expect(store.saving()).toBe(false);
        expect(store.error()).toBeNull();
        expect(store.fieldErrors()).toEqual({});
    });

    it('load fills the list in the API order', async () => {
        await store.load();

        expect(fake.calls).toEqual(['list']);
        expect(names()).toEqual(['Боядисване', 'Шпакловка']);
        expect(store.loading()).toBe(false);
    });

    it('load error sets a message', async () => {
        fake.listResult = httpError(500);

        await store.load();

        expect(store.error()).toBe('Грешка при зареждане на услугите');
        expect(store.loading()).toBe(false);
    });

    it('create returns the service and keeps the list sorted by name (Bulgarian)', async () => {
        await store.load();

        const created = await store.create(request);

        expect(created).toEqual({ id: 'new', ...request });
        expect(fake.calls).toContain('create Грундиране');
        expect(names()).toEqual(['Боядисване', 'Грундиране', 'Шпакловка']);
        expect(store.saving()).toBe(false);
    });

    it('create 400 keeps the first message per field (camelCase) and returns null', async () => {
        fake.createResult = httpError(400, {
            errors: { Name: ['Въведи име', 'Друго'], defaultRate: ['Цената трябва да е положителна'] },
        });

        const created = await store.create(request);

        expect(created).toBeNull();
        expect(store.fieldErrors()).toEqual({ name: 'Въведи име', defaultRate: 'Цената трябва да е положителна' });
        expect(store.error()).toBeNull();
        expect(store.saving()).toBe(false);
    });

    it('create 409 shows the ProblemDetails title', async () => {
        fake.createResult = httpError(409, { title: 'Тази услуга вече я има' });

        expect(await store.create(request)).toBeNull();

        expect(store.error()).toBe('Тази услуга вече я има');
        expect(store.fieldErrors()).toEqual({});
    });

    it('an error without details falls back to "Грешка при записа"', async () => {
        fake.createResult = httpError(500);

        await store.create(request);

        expect(store.error()).toBe('Грешка при записа');
    });

    it('update replaces the service and moves it to its place', async () => {
        await store.load();

        const updated = await store.update('sv2', { name: 'Ащарване', unit: 'Day', defaultRate: null });

        expect(updated?.name).toBe('Ащарване');
        expect(fake.calls).toContain('update sv2 Ащарване');
        expect(store.services()).toEqual([
            { id: 'sv2', name: 'Ащарване', unit: 'Day', defaultRate: null },
            testService({ id: 'sv1', name: 'Боядисване', unit: 'SquareMeter', defaultRate: 6 }),
        ]);
    });

    it('update 404 shows a Bulgarian message (not the English title) and returns null', async () => {
        await store.load();
        fake.updateResult = httpError(404, { title: 'Not Found' });

        expect(await store.update('sv1', request)).toBeNull();

        expect(store.error()).toBe('Услугата вече я няма');
        expect(names()).toEqual(['Боядисване', 'Шпакловка']);
    });

    it('remove drops the service', async () => {
        await store.load();

        expect(await store.remove('sv1')).toBe(true);

        expect(fake.calls).toContain('remove sv1');
        expect(names()).toEqual(['Шпакловка']);
    });

    it('remove 409 keeps the service and shows the reason', async () => {
        await store.load();
        fake.removeResult = httpError(409, { title: 'Услугата има цени в 2 обекта' });

        expect(await store.remove('sv1')).toBe(false);

        expect(store.error()).toBe('Услугата има цени в 2 обекта');
        expect(names()).toEqual(['Боядисване', 'Шпакловка']);
    });

    it('a new save and clearErrors clear the previous errors', async () => {
        fake.createResult = httpError(400, { errors: { name: ['Въведи име'] } });
        await store.create(request);
        fake.createResult = of(testService());

        await store.create(request);
        expect(store.fieldErrors()).toEqual({});

        fake.createResult = httpError(409, { title: 'Тази услуга вече я има' });
        await store.create(request);
        store.clearErrors();
        expect(store.error()).toBeNull();
    });
});
