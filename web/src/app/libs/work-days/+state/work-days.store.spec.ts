import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { httpError } from '../../services/testing/fake-services.service';
import { SaveWorkDayRequest } from '../work-day.model';
import { WorkDaysService } from '../work-days.service';
import { FakeWorkDaysService, testWorkDay, testWorkItem } from '../testing/fake-work-days.service';
import { WorkDaysStore } from './work-days.store';

describe('WorkDaysStore', () => {
    let fake: FakeWorkDaysService;
    let store: InstanceType<typeof WorkDaysStore>;

    const ids = () => store.days().map((d) => d.id);
    const request = (date: string): SaveWorkDayRequest => ({
        date,
        title: null,
        note: null,
        workers: [],
        items: [
            {
                id: null,
                serviceId: null,
                description: 'Къртене',
                quantity: 1,
                unit: 'Day',
                rate: null,
                needsReview: false,
            },
        ],
    });

    beforeEach(() => {
        fake = new FakeWorkDaysService();
        fake.listResult = of([
            testWorkDay({ id: 'd2', date: '2026-11-14' }),
            testWorkDay({ id: 'd1', date: '2026-11-10' }),
        ]);
        TestBed.configureTestingModule({ providers: [{ provide: WorkDaysService, useValue: fake }] });
        store = TestBed.inject(WorkDaysStore);
    });

    it('starts empty', () => {
        expect(store.days()).toEqual([]);
        expect(store.loading()).toBe(false);
        expect(store.saving()).toBe(false);
        expect(store.error()).toBeNull();
        expect(store.fieldErrors()).toEqual({});
    });

    it('load fills the days of the site in the API order (newest first)', async () => {
        await store.load('s1');

        expect(fake.calls).toEqual(['list s1']);
        expect(ids()).toEqual(['d2', 'd1']);
        expect(store.loading()).toBe(false);
    });

    it('load of another site forgets the previous days first; an error shows a message', async () => {
        await store.load('s1');
        fake.listResult = httpError(500);

        await store.load('s2');

        expect(store.days()).toEqual([]);
        expect(store.error()).toBe('Грешка при зареждане на работните дни');
    });

    it('reloading the same site keeps the days while it loads', async () => {
        await store.load('s1');
        fake.listResult = of([testWorkDay({ id: 'd3', date: '2026-11-15' })]);

        const pending = store.load('s1');
        expect(ids()).toEqual(['d2', 'd1']);
        await pending;

        expect(ids()).toEqual(['d3']);
    });

    it('create puts the new day by date, newest first (and first among the same date)', async () => {
        await store.load('s1');

        const created = await store.create('s1', request('2026-11-12'));
        expect(created?.id).toBe('new');
        expect(ids()).toEqual(['d2', 'new', 'd1']);

        fake.createResult = of(testWorkDay({ id: 'same', date: '2026-11-14' }));
        await store.create('s1', request('2026-11-14'));
        expect(ids()).toEqual(['same', 'd2', 'new', 'd1']);
        expect(store.saving()).toBe(false);
    });

    it('create 400 fills fieldErrors, also for items (camelCase per segment)', async () => {
        fake.createResult = httpError(400, {
            errors: { Date: ['Датата не може да е в бъдещето'], 'Items[0].Quantity': ['Количеството трябва да е > 0'] },
        });

        expect(await store.create('s1', request('2030-01-01'))).toBeNull();

        expect(store.fieldErrors()).toEqual({
            date: 'Датата не може да е в бъдещето',
            'items[0].quantity': 'Количеството трябва да е > 0',
        });
        expect(store.error()).toBeNull();
    });

    it('update replaces the day and re-sorts by date', async () => {
        await store.load('s1');
        fake.updateResult = of(
            testWorkDay({ id: 'd1', date: '2026-11-15', items: [testWorkItem({ id: 'i9', description: 'Нова' })] }),
        );

        const updated = await store.update('s1', 'd1', request('2026-11-15'));

        expect(updated?.date).toBe('2026-11-15');
        expect(fake.calls).toContain('update s1 d1 2026-11-15');
        expect(ids()).toEqual(['d1', 'd2']);
        expect(store.days()[0].items[0].description).toBe('Нова');
    });

    it('update 404 shows that the day is gone', async () => {
        await store.load('s1');
        fake.updateResult = httpError(404, { title: 'Not Found' });

        expect(await store.update('s1', 'd1', request('2026-11-10'))).toBeNull();

        expect(store.error()).toBe('Денят вече го няма');
    });

    it('remove drops the day; an error keeps it', async () => {
        await store.load('s1');

        expect(await store.remove('s1', 'd2')).toBe(true);
        expect(fake.calls).toContain('remove s1 d2');
        expect(ids()).toEqual(['d1']);

        fake.removeResult = httpError(500);
        expect(await store.remove('s1', 'd1')).toBe(false);
        expect(ids()).toEqual(['d1']);
        expect(store.error()).toBe('Грешка при записа');
    });

    it('clearErrors hides the messages', async () => {
        fake.createResult = httpError(400, { errors: { title: ['Твърде дълго'] } });
        await store.create('s1', request('2026-11-10'));

        store.clearErrors();

        expect(store.fieldErrors()).toEqual({});
        expect(store.error()).toBeNull();
    });
});
