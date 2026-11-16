import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LedgerComponent } from './ledger.component';

describe('LedgerComponent', () => {
    let fixture: ComponentFixture<LedgerComponent>;
    let element: HTMLElement;

    const text = (node: Element | null | undefined) => node?.textContent?.replace(/\s+/g, ' ').trim();
    const columns = () => Array.from(element.querySelectorAll<HTMLElement>('.ledger__col'));
    const labels = () => columns().map((col) => text(col.querySelector('.ledger__label')));
    const values = () => columns().map((col) => text(col.querySelector('.ledger__value')));

    async function render(earned: number, paid: number, remaining: number, lastLabel?: string) {
        fixture.componentRef.setInput('earned', earned);
        fixture.componentRef.setInput('paid', paid);
        fixture.componentRef.setInput('remaining', remaining);
        if (lastLabel) {
            fixture.componentRef.setInput('lastLabel', lastLabel);
        }
        await fixture.whenStable();
    }

    beforeEach(() => {
        fixture = TestBed.createComponent(LedgerComponent);
        element = fixture.nativeElement as HTMLElement;
    });

    it('shows Изработено, Платено and Остатък with the amounts in EUR (mono)', async () => {
        await render(1157.89, 500, 657.89);

        expect(labels()).toEqual(['Изработено', 'Платено', 'Остатък']);
        expect(values()).toEqual(['1 157,89 €', '500,00 €', '657,89 €']);
        expect(columns().every((col) => col.querySelector('.ledger__value')!.classList.contains('num'))).toBe(true);
    });

    it('the remaining amount is red while something is owed', async () => {
        await render(1157.89, 500, 657.89);

        expect(columns()[2].classList).toContain('ledger__col--warn');
        expect(columns()[2].classList).not.toContain('ledger__col--good');
    });

    it('the remaining amount is green when everything is paid', async () => {
        await render(500, 500, 0);

        expect(values()[2]).toBe('0,00 €');
        expect(columns()[2].classList).toContain('ledger__col--good');
        expect(columns()[2].classList).not.toContain('ledger__col--warn');
    });

    it('a negative remaining reads „Надплатено“ with the amount, green', async () => {
        await render(400, 500, -100);

        expect(labels()[2]).toBe('Надплатено');
        expect(values()[2]).toBe('100,00 €');
        expect(columns()[2].classList).toContain('ledger__col--good');
    });

    it('the third label can be „Дължат ни“ (sites list)', async () => {
        await render(1300, 500, 800, 'Дължат ни');

        expect(labels()).toEqual(['Изработено', 'Платено', 'Дължат ни']);

        await render(400, 500, -100, 'Дължат ни');
        expect(labels()[2]).toBe('Надплатено');
    });

    it('marks five-digit amounts as long (smaller digits on the phone)', async () => {
        await render(9999.99, 0, 9999.99);
        expect(element.querySelector('.ledger')!.classList).not.toContain('ledger--long');

        await render(13557.89, 12400, 1157.89);
        expect(element.querySelector('.ledger')!.classList).toContain('ledger--long');

        await render(0, 10000, -10000);
        expect(element.querySelector('.ledger')!.classList).toContain('ledger--long');
    });
});
