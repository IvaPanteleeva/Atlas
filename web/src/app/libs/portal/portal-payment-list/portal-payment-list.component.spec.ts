import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PortalPayment } from '../portal.model';
import { PortalPaymentListComponent } from './portal-payment-list.component';

describe('PortalPaymentListComponent', () => {
    let fixture: ComponentFixture<PortalPaymentListComponent>;
    let element: HTMLElement;

    const text = (node: Element | null | undefined) => node?.textContent?.replace(/\s+/g, ' ').trim();

    beforeEach(() => {
        fixture = TestBed.createComponent(PortalPaymentListComponent);
        element = fixture.nativeElement as HTMLElement;
    });

    async function render(payments: PortalPayment[]) {
        fixture.componentRef.setInput('payments', payments);
        await fixture.whenStable();
    }

    it('shows the empty state', async () => {
        await render([]);

        expect(text(element)).toBe('Няма плащания.');
    });

    it('shows date and amount per payment, in the given order, with nothing to click', async () => {
        await render([
            { date: '2026-11-03', amount: 300 },
            { date: '2026-11-10', amount: 1157.89 },
        ]);

        const rows = Array.from(element.querySelectorAll('.payment'));
        expect(rows.map((row) => text(row.querySelector('.date')))).toEqual(['03.11', '10.11']);
        expect(rows.map((row) => text(row.querySelector('.amount')))).toEqual(['300,00 €', '1 157,89 €']);
        expect(element.querySelectorAll('button, a').length).toBe(0);
    });
});
