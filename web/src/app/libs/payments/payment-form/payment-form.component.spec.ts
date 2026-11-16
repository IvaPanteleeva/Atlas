import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SavePaymentRequest } from '../payment.model';
import { testPayment } from '../testing/fake-payments.service';
import { PaymentFormComponent } from './payment-form.component';

describe('PaymentFormComponent', () => {
    let fixture: ComponentFixture<PaymentFormComponent>;
    let element: HTMLElement;
    let saved: SavePaymentRequest[];
    let deletes: number;
    let cancels: number;

    beforeAll(() => {
        Element.prototype.scrollTo ??= () => undefined;
    });

    beforeEach(async () => {
        // Only Date is faked: "today" is Monday 16.11.2026 (timers stay real for whenStable).
        vi.useFakeTimers({ toFake: ['Date'] });
        vi.setSystemTime(new Date(2026, 10, 16, 10, 30));
        fixture = TestBed.createComponent(PaymentFormComponent);
        saved = [];
        deletes = 0;
        cancels = 0;
        const form = fixture.componentInstance;
        form.save.subscribe((request) => saved.push(request));
        form.delete.subscribe(() => deletes++);
        form.cancel.subscribe(() => cancels++);
        element = fixture.nativeElement as HTMLElement;
        await fixture.whenStable();
    });

    afterEach(() => {
        vi.useRealTimers();
        document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => (overlay.innerHTML = ''));
    });

    async function set(name: string, value: unknown) {
        fixture.componentRef.setInput(name, value);
        await fixture.whenStable();
    }

    const text = (node: Element | null | undefined) => node?.textContent?.replace(/\s+/g, ' ').trim();
    const amount = () => element.querySelector<HTMLInputElement>('#pay-amount')!;
    const date = () => element.querySelector<HTMLInputElement>('#pay-date')!;
    const note = () => element.querySelector<HTMLInputElement>('#pay-note')!;
    const item = (input: Element) => text(input.closest('nz-form-item'));
    const button = (label: string) =>
        Array.from(element.querySelectorAll<HTMLButtonElement>('button')).find(
            (candidate) => text(candidate) === label,
        );

    async function type(input: HTMLInputElement, value: string) {
        input.value = value;
        input.dispatchEvent(new Event('input'));
        await fixture.whenStable();
    }

    async function submit() {
        button('Запази')!.click();
        await fixture.whenStable();
    }

    it('starts empty with today, the € suffix and the note placeholder; no delete for a new payment', () => {
        expect(amount().value).toBe('');
        expect(text(amount().closest('nz-input-wrapper'))).toContain('€');
        expect(date().value).toBe('16.11.2026');
        expect(note().getAttribute('placeholder')).toBe('напр. аванс, в брой');
        expect(button('Изтрий')).toBeUndefined();
    });

    it('does not allow days after today', async () => {
        date().click();
        await fixture.whenStable();
        const cells = await vi.waitFor(() => {
            const found = Array.from(document.querySelectorAll<HTMLElement>('td.ant-picker-cell-in-view'));
            expect(found.length).toBeGreaterThan(0);
            return found;
        });
        const disabled = (day: string) =>
            cells.find((cell) => text(cell) === day)!.classList.contains('ant-picker-cell-disabled');

        expect(disabled('16')).toBe(false);
        expect(disabled('1')).toBe(false);
        expect(disabled('17')).toBe(true);
    });

    it('saves amount, date as yyyy-MM-dd and a trimmed note (empty note = null)', async () => {
        await type(amount(), '1157,89');
        await type(note(), '  аванс ');
        await submit();

        await type(note(), '   ');
        await submit();

        expect(saved).toEqual([
            { date: '2026-11-16', amount: 1157.89, note: 'аванс' },
            { date: '2026-11-16', amount: 1157.89, note: null },
        ]);
    });

    it('shows money with two decimals after leaving the field', async () => {
        await type(amount(), '500');
        amount().dispatchEvent(new Event('blur'));
        await fixture.whenStable();

        expect(amount().value).toBe('500,00');
    });

    it('the amount is required; the messages show on submit, also for an untouched field', async () => {
        expect(item(amount())).not.toContain('Въведи сума');

        await submit();

        expect(saved).toEqual([]);
        expect(item(amount())).toContain('Въведи сума');
    });

    it('text that is not a number shows "Въведи число" and is not saved', async () => {
        await type(amount(), '500 лв');
        await submit();

        expect(saved).toEqual([]);
        expect(item(amount())).toContain('Въведи число');
    });

    it('the amount must be above 0 and at most 1 000 000 €', async () => {
        await type(amount(), '0');
        await submit();
        expect(item(amount())).toContain('Сумата трябва да е по-голяма от 0');

        await type(amount(), '1000000,01');
        await submit();
        expect(item(amount())).toContain('Сумата може да е най-много 1 000 000 €');

        await type(amount(), '1000000');
        await submit();
        expect(saved.map((request) => request.amount)).toEqual([1_000_000]);
    });

    it('the note is at most 200 characters', async () => {
        expect(note().maxLength).toBe(200);
    });

    it('edits a payment: fills the fields and saves the changes', async () => {
        await set('payment', testPayment({ date: '2026-11-10', amount: 500, note: 'аванс' }));

        expect(amount().value).toBe('500,00');
        expect(date().value).toBe('10.11.2026');
        expect(note().value).toBe('аванс');

        await type(amount(), '650');
        await submit();

        expect(saved).toEqual([{ date: '2026-11-10', amount: 650, note: 'аванс' }]);
    });

    it('shows server errors under the fields, and others in the alert', async () => {
        await set('fieldErrors', {
            amount: 'Сумата може да е до 2 знака след запетаята',
            date: 'Датата не може да е в бъдещето',
            note: 'Бележката е твърде дълга',
            other: 'Нещо друго',
        });
        await set('error', 'Грешка при записа');

        expect(item(amount())).toContain('Сумата може да е до 2 знака след запетаята');
        expect(item(date())).toContain('Датата не може да е в бъдещето');
        expect(item(note())).toContain('Бележката е твърде дълга');
        const alert = text(element.querySelector('nz-alert'));
        expect(alert).toContain('Грешка при записа');
        expect(alert).toContain('Нещо друго');
        expect(alert).not.toContain('Сумата');
    });

    it('delete is only shown with canDelete (Owner)', async () => {
        await set('payment', testPayment());

        expect(button('Изтрий')).toBeUndefined();
    });

    it('delete needs a second click when editing; a new payment resets it; Отказ emits cancel', async () => {
        await set('payment', testPayment());
        await set('canDelete', true);

        button('Изтрий')!.click();
        await fixture.whenStable();
        expect(deletes).toBe(0);
        button('Потвърди изтриване')!.click();
        expect(deletes).toBe(1);

        await set('payment', testPayment({ id: 'pay2' }));
        expect(button('Изтрий')).toBeDefined();

        button('Отказ')!.click();
        expect(cancels).toBe(1);
    });

    it('does not save twice while saving', async () => {
        await type(amount(), '100');
        await set('saving', true);

        await submit();

        expect(saved).toEqual([]);
    });
});
