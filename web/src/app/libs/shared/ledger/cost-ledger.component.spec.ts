import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CostLedgerComponent } from './cost-ledger.component';

describe('CostLedgerComponent', () => {
    let fixture: ComponentFixture<CostLedgerComponent>;
    let element: HTMLElement;

    const text = (node: Element | null | undefined) => node?.textContent?.replace(/\s+/g, ' ').trim();
    const columns = () => Array.from(element.querySelectorAll<HTMLElement>('.ledger__col'));
    const labels = () => columns().map((col) => text(col.querySelector('.ledger__label')));
    const values = () => columns().map((col) => text(col.querySelector('.ledger__value')));

    async function render(workerCost: number, net: number) {
        fixture.componentRef.setInput('workerCost', workerCost);
        fixture.componentRef.setInput('net', net);
        await fixture.whenStable();
    }

    beforeEach(() => {
        fixture = TestBed.createComponent(CostLedgerComponent);
        element = fixture.nativeElement as HTMLElement;
    });

    it('shows „Разход за хора“ and „Остава за нас“ in two columns (mono, EUR)', async () => {
        await render(400, 757.89);

        expect(element.querySelector('.ledger')!.classList).toContain('ledger--two');
        expect(labels()).toEqual(['Разход за хора', 'Остава за нас']);
        expect(values()).toEqual(['400,00 €', '757,89 €']);
        expect(columns().every((col) => col.querySelector('.ledger__value')!.classList.contains('num'))).toBe(true);
    });

    it('„Остава за нас“ is green above 0, red below 0 (with the minus) and neutral at 0', async () => {
        await render(400, 757.89);
        expect(columns()[1].classList).toContain('ledger__col--good');
        expect(columns()[1].classList).not.toContain('ledger__col--warn');

        await render(1200, -42.11);
        expect(values()[1]).toBe('-42,11 €');
        expect(columns()[1].classList).toContain('ledger__col--warn');
        expect(columns()[1].classList).not.toContain('ledger__col--good');

        await render(0, 0);
        expect(columns()[1].classList).not.toContain('ledger__col--warn');
        expect(columns()[1].classList).not.toContain('ledger__col--good');
    });

    it('the cost itself has no color', async () => {
        await render(400, 757.89);

        expect(columns()[0].className).not.toMatch(/--(good|warn)/);
    });

    it('marks five-digit amounts as long', async () => {
        await render(12000, 1157.89);

        expect(element.querySelector('.ledger')!.classList).toContain('ledger--long');
    });
});
