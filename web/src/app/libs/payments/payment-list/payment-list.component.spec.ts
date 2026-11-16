import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Payment } from '../payment.model';
import { testPayment } from '../testing/fake-payments.service';
import { PaymentListComponent } from './payment-list.component';

describe('PaymentListComponent', () => {
    let fixture: ComponentFixture<PaymentListComponent>;
    let element: HTMLElement;
    let edited: Payment[];

    const text = (node: Element | null | undefined) => node?.textContent?.replace(/\s+/g, ' ').trim();
    const rows = () => Array.from(element.querySelectorAll<HTMLButtonElement>('button.payment'));

    async function render(list: Payment[]) {
        fixture.componentRef.setInput('payments', list);
        await fixture.whenStable();
    }

    beforeEach(() => {
        fixture = TestBed.createComponent(PaymentListComponent);
        element = fixture.nativeElement as HTMLElement;
        edited = [];
        fixture.componentInstance.edit.subscribe((payment) => edited.push(payment));
    });

    it('shows the empty state', async () => {
        await render([]);

        expect(text(element)).toBe('Няма плащания.');
    });

    it('shows date "dd.MM", the note (or „Плащане“) and the amount in EUR', async () => {
        await render([
            testPayment({ id: 'a', date: '2026-11-16', note: 'аванс', amount: 1157.89 }),
            testPayment({ id: 'b', date: '2026-11-03', note: null, amount: 200 }),
        ]);

        expect(rows().map((row) => text(row.querySelector('.date')))).toEqual(['16.11', '03.11']);
        expect(rows().map((row) => text(row.querySelector('.label')))).toEqual(['аванс', 'Плащане']);
        expect(rows().map((row) => text(row.querySelector('.amount')))).toEqual(['1 157,89 €', '200,00 €']);
        expect(rows()[1].querySelector('.label')!.classList).toContain('label--plain');
    });

    it('tapping a row emits the payment for editing', async () => {
        const payment = testPayment({ id: 'b' });
        await render([testPayment({ id: 'a' }), payment]);

        rows()[1].click();

        expect(edited).toEqual([payment]);
    });
});
