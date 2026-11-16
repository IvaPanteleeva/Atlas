import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { httpError } from '../../services/testing/fake-services.service';
import { SavePaymentRequest } from '../payment.model';
import { PaymentsService } from '../payments.service';
import { FakePaymentsService, testPayment } from '../testing/fake-payments.service';
import { PaymentsStore } from './payments.store';

describe('PaymentsStore', () => {
    let fake: FakePaymentsService;
    let store: InstanceType<typeof PaymentsStore>;

    const ids = () => store.payments().map((p) => p.id);
    const request = (date: string, amount = 100): SavePaymentRequest => ({ date, amount, note: null });

    beforeEach(() => {
        fake = new FakePaymentsService();
        fake.listResult = of([
            testPayment({ id: 'p2', date: '2026-11-14' }),
            testPayment({ id: 'p1', date: '2026-11-10' }),
        ]);
        TestBed.configureTestingModule({ providers: [{ provide: PaymentsService, useValue: fake }] });
        store = TestBed.inject(PaymentsStore);
    });

    it('starts empty', () => {
        expect(store.payments()).toEqual([]);
        expect(store.loading()).toBe(false);
        expect(store.saving()).toBe(false);
        expect(store.error()).toBeNull();
        expect(store.fieldErrors()).toEqual({});
    });

    it('load fills the payments of the site in the API order (newest first)', async () => {
        await store.load('s1');

        expect(fake.calls).toEqual(['list s1']);
        expect(ids()).toEqual(['p2', 'p1']);
        expect(store.loading()).toBe(false);
    });

    it('load of another site forgets the previous payments first; an error shows a message', async () => {
        await store.load('s1');
        fake.listResult = httpError(500);

        await store.load('s2');

        expect(store.payments()).toEqual([]);
        expect(store.error()).toBe('Грешка при зареждане на плащанията');
    });

    it('reloading the same site keeps the payments while it loads', async () => {
        await store.load('s1');
        fake.listResult = of([testPayment({ id: 'p3' })]);

        const pending = store.load('s1');
        expect(ids()).toEqual(['p2', 'p1']);
        await pending;

        expect(ids()).toEqual(['p3']);
    });

    it('create puts the new payment by date, newest first (and first among the same date)', async () => {
        await store.load('s1');

        const created = await store.create('s1', request('2026-11-12'));
        expect(created?.id).toBe('new');
        expect(fake.calls).toContain('create s1 2026-11-12 100');
        expect(ids()).toEqual(['p2', 'new', 'p1']);

        fake.createResult = of(testPayment({ id: 'same', date: '2026-11-14' }));
        await store.create('s1', request('2026-11-14'));
        expect(ids()).toEqual(['same', 'p2', 'new', 'p1']);
        expect(store.saving()).toBe(false);
    });

    it('create 400 fills fieldErrors (camelCase) and returns null', async () => {
        fake.createResult = httpError(400, {
            errors: { Amount: ['Сумата трябва да е > 0'], Date: ['Датата не може да е в бъдещето'] },
        });

        expect(await store.create('s1', request('2030-01-01'))).toBeNull();

        expect(store.fieldErrors()).toEqual({
            amount: 'Сумата трябва да е > 0',
            date: 'Датата не може да е в бъдещето',
        });
        expect(store.error()).toBeNull();
        expect(store.saving()).toBe(false);
    });

    it('update replaces the payment and re-sorts by date', async () => {
        await store.load('s1');

        const updated = await store.update('s1', 'p1', request('2026-11-15', 250));

        expect(updated?.amount).toBe(250);
        expect(fake.calls).toContain('update s1 p1 2026-11-15 250');
        expect(ids()).toEqual(['p1', 'p2']);
    });

    it('update 404 shows that the payment is gone', async () => {
        await store.load('s1');
        fake.updateResult = httpError(404, { title: 'Not Found' });

        expect(await store.update('s1', 'p1', request('2026-11-10'))).toBeNull();

        expect(store.error()).toBe('Плащането вече го няма');
    });

    it('remove drops the payment; an error keeps it', async () => {
        await store.load('s1');

        expect(await store.remove('s1', 'p2')).toBe(true);
        expect(fake.calls).toContain('remove s1 p2');
        expect(ids()).toEqual(['p1']);

        fake.removeResult = httpError(500);
        expect(await store.remove('s1', 'p1')).toBe(false);
        expect(ids()).toEqual(['p1']);
        expect(store.error()).toBe('Грешка при записа');
    });

    it('clearErrors hides the messages', async () => {
        fake.createResult = httpError(400, { errors: { note: ['Твърде дълго'] } });
        await store.create('s1', request('2026-11-10'));

        store.clearErrors();

        expect(store.fieldErrors()).toEqual({});
        expect(store.error()).toBeNull();
    });
});
