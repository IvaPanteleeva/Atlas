import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { Service } from '../../services/service.model';
import { APP_ICONS } from '../../shared/icons';
import { SitePrice } from '../../site-prices/site-price.model';
import { testSitePrice } from '../../site-prices/testing/fake-site-prices.service';
import { SaveWorkDayRequest } from '../work-day.model';
import { testWorkDay, testWorkItem } from '../testing/fake-work-days.service';
import { WorkDayFormComponent } from './work-day-form.component';

const services: Service[] = [
    { id: 'sv1', name: 'Боядисване', unit: 'SquareMeter', defaultRate: 6 },
    { id: 'sv2', name: 'Первази', unit: 'LinearMeter', defaultRate: null },
    { id: 'sv3', name: 'Шпакловка', unit: 'SquareMeter', defaultRate: 8.5 },
];

/** Боядисване has an agreed price on this site (6,50 instead of the default 6). */
const sitePrices: SitePrice[] = [testSitePrice({ id: 'p1', serviceId: 'sv1', serviceName: 'Боядисване', rate: 6.5 })];

describe('WorkDayFormComponent', () => {
    let fixture: ComponentFixture<WorkDayFormComponent>;
    let element: HTMLElement;
    let saved: SaveWorkDayRequest[];
    let deletes: number;
    let cancels: number;

    beforeAll(() => {
        Element.prototype.scrollTo ??= () => undefined;
    });

    beforeEach(async () => {
        // Only Date is faked: "today" is Monday 16.11.2026 (timers stay real for whenStable).
        vi.useFakeTimers({ toFake: ['Date'] });
        vi.setSystemTime(new Date(2026, 10, 16, 10, 30));
        TestBed.configureTestingModule({ providers: [provideNzIcons(APP_ICONS)] });
        fixture = TestBed.createComponent(WorkDayFormComponent);
        saved = [];
        deletes = 0;
        cancels = 0;
        const form = fixture.componentInstance;
        form.save.subscribe((request) => saved.push(request));
        form.delete.subscribe(() => deletes++);
        form.cancel.subscribe(() => cancels++);
        fixture.componentRef.setInput('services', services);
        fixture.componentRef.setInput('sitePrices', sitePrices);
        fixture.componentRef.setInput('canEditPrices', true);
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
    const cards = () => Array.from(element.querySelectorAll<HTMLElement>('.item-card'));
    const field = (index: number, name: string) =>
        element.querySelector<HTMLInputElement>(`#wd-item-${index}-${name}`)!;
    const button = (label: string, root: ParentNode = element) =>
        Array.from(root.querySelectorAll<HTMLButtonElement>('button')).find(
            (candidate) => text(candidate) === label || candidate.getAttribute('aria-label') === label,
        );

    async function type(input: HTMLInputElement | HTMLTextAreaElement, value: string) {
        input.value = value;
        input.dispatchEvent(new Event('input'));
        await fixture.whenStable();
    }

    async function submit() {
        button('Запази')!.click();
        await fixture.whenStable();
    }

    async function chooseService(index: number, label: string) {
        cards()[index].querySelector<HTMLElement>('nz-select.item-service .ant-select-selector')!.click();
        await fixture.whenStable();
        const options = await vi.waitFor(() => {
            const found = Array.from(document.querySelectorAll<HTMLElement>('nz-option-item'));
            expect(found.length).toBeGreaterThan(0);
            return found;
        });
        options.find((option) => text(option) === label)!.click();
        await fixture.whenStable();
    }

    async function fillFreeText(index: number, description: string, quantity: string, rate = '') {
        await type(field(index, 'description'), description);
        await type(field(index, 'quantity'), quantity);
        if (rate) {
            await type(field(index, 'rate'), rate);
        }
    }

    it('starts with today and one empty free-text item', () => {
        expect(field(0, 'description').value).toBe('');
        expect(element.querySelector<HTMLInputElement>('#wd-date')!.value).toBe('16.11.2026');
        expect(cards().length).toBe(1);
        expect(text(cards()[0].querySelector('nz-select.item-service .ant-select-selection-item'))).toBe(
            'Свободен текст',
        );
        expect(button('Изтрий')).toBeUndefined();
    });

    it('does not allow days after today', async () => {
        element.querySelector<HTMLElement>('#wd-date')!.click();
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
        expect(disabled('30')).toBe(true);
    });

    it('offers „Свободен текст“ and the services as "name · unit"', async () => {
        cards()[0].querySelector<HTMLElement>('nz-select.item-service .ant-select-selector')!.click();
        await fixture.whenStable();

        const options = await vi.waitFor(() => {
            const found = Array.from(document.querySelectorAll<HTMLElement>('nz-option-item'));
            expect(found.length).toBeGreaterThan(0);
            return found;
        });

        expect(options.map((o) => text(o))).toEqual([
            'Свободен текст',
            'Боядисване · м²',
            'Первази · м.л.',
            'Шпакловка · м²',
        ]);
    });

    it('a service fills description, unit and the price for the site (then the default); free text clears it', async () => {
        await chooseService(0, 'Боядисване · м²');
        expect(field(0, 'description').value).toBe('Боядисване');
        expect(text(cards()[0].querySelector('nz-select.item-unit .ant-select-selection-item'))).toBe('м²');
        expect(field(0, 'rate').value).toBe('6,50');

        await chooseService(0, 'Первази · м.л.');
        expect(field(0, 'description').value).toBe('Первази');
        expect(text(cards()[0].querySelector('nz-select.item-unit .ant-select-selection-item'))).toBe('м.л.');
        expect(field(0, 'rate').value).toBe('');

        await chooseService(0, 'Шпакловка · м²');
        expect(field(0, 'rate').value).toBe('8,50');

        await chooseService(0, 'Свободен текст');
        expect(field(0, 'rate').value).toBe('');
        await type(field(0, 'quantity'), '12,5');
        await submit();

        expect(saved[0].items).toEqual([
            {
                id: null,
                serviceId: null,
                description: 'Шпакловка',
                quantity: 12.5,
                unit: 'SquareMeter',
                rate: null,
                needsReview: false,
            },
        ]);
    });

    it('saves a new day with a trimmed title and note, date as yyyy-MM-dd', async () => {
        await type(element.querySelector<HTMLInputElement>('#wd-title')!, '  Шпакловка финал ');
        await type(element.querySelector<HTMLTextAreaElement>('#wd-note')!, ' Ключ при съседа ');
        await chooseService(0, 'Боядисване · м²');
        await type(field(0, 'quantity'), '12,5');
        cards()[0].querySelector<HTMLInputElement>('label.item-review input')!.click();
        await fixture.whenStable();

        await submit();

        expect(saved).toEqual([
            {
                date: '2026-11-16',
                title: 'Шпакловка финал',
                note: 'Ключ при съседа',
                items: [
                    {
                        id: null,
                        serviceId: 'sv1',
                        description: 'Боядисване',
                        quantity: 12.5,
                        unit: 'SquareMeter',
                        rate: 6.5,
                        needsReview: true,
                    },
                ],
            },
        ]);
    });

    it('„+ Позиция“ adds a card; ✕ removes one but never the last', async () => {
        expect(button('Махни позицията')!.disabled).toBe(true);

        button('Позиция')!.click();
        await fixture.whenStable();
        expect(cards().length).toBe(2);
        await fillFreeText(0, 'Първа', '1');
        await fillFreeText(1, 'Втора', '2');

        button('Махни позицията', cards()[0])!.click();
        await fixture.whenStable();

        expect(cards().length).toBe(1);
        expect(field(0, 'description').value).toBe('Втора');
        expect(button('Махни позицията')!.disabled).toBe(true);
    });

    it('shows the day total while typing, with „+N без цена“', async () => {
        await fillFreeText(0, 'Боя', '2', '6,5');
        button('Позиция')!.click();
        await fixture.whenStable();
        await fillFreeText(1, 'Первази', '3');

        const footer = element.querySelector('.day-total')!;
        expect(text(footer.querySelector('.day-total__sum'))).toBe('13,00 €');
        expect(text(footer.querySelector('.pill'))).toBe('+1 без цена');

        await type(field(1, 'rate'), '1,25');
        expect(text(footer.querySelector('.day-total__sum'))).toBe('16,75 €');
        expect(footer.querySelector('.pill')).toBeNull();
    });

    it('shows quantities with a decimal comma after leaving the field, never "10.500"', async () => {
        await type(field(0, 'quantity'), '10.5');
        field(0, 'quantity').dispatchEvent(new Event('blur'));
        await fixture.whenStable();
        expect(field(0, 'quantity').value).toBe('10,5');

        await type(field(0, 'quantity'), '2');
        field(0, 'quantity').dispatchEvent(new Event('blur'));
        await fixture.whenStable();
        expect(field(0, 'quantity').value).toBe('2');
    });

    it('a quantity that is not a number shows "Въведи число" and is not saved', async () => {
        await fillFreeText(0, 'Боя', '12,5 м');

        await submit();

        expect(saved).toEqual([]);
        expect(text(field(0, 'quantity').closest('nz-form-item'))).toContain('Въведи число');
    });

    it('shows the required messages on submit, also for untouched fields', async () => {
        await submit();

        expect(saved).toEqual([]);
        expect(text(field(0, 'description').closest('nz-form-item'))).toContain('Въведи описание');
        expect(text(field(0, 'quantity').closest('nz-form-item'))).toContain('Въведи количество');
    });

    it('Crew cannot change the price: it shows the resolved price with a hint and sends null', async () => {
        await set('canEditPrices', false);

        await chooseService(0, 'Боядисване · м²');
        expect(field(0, 'rate').disabled).toBe(true);
        expect(field(0, 'rate').value).toBe('6,50');
        expect(text(cards()[0])).toContain('Цената се попълва автоматично');
        expect(text(element.querySelector('.day-total__sum'))).toBe('0,00 €');

        await type(field(0, 'quantity'), '2');
        expect(text(element.querySelector('.day-total__sum'))).toBe('13,00 €');
        await submit();

        expect(saved[0].items[0].rate).toBeNull();
        expect(saved[0].items[0].serviceId).toBe('sv1');
    });

    it('edits a day: fills the fields and sends the ids of existing items', async () => {
        await set(
            'day',
            testWorkDay({
                id: 'd1',
                date: '2026-11-10',
                title: 'Шпакловка',
                note: 'Бележка',
                items: [
                    testWorkItem({ id: 'i1', serviceId: 'sv3', description: 'Шпакловка', quantity: 12.5, rate: 7 }),
                    testWorkItem({
                        id: 'i2',
                        serviceId: null,
                        description: 'Извозване',
                        quantity: 1,
                        unit: 'Piece',
                        rate: null,
                    }),
                ],
            }),
        );

        expect(element.querySelector<HTMLInputElement>('#wd-date')!.value).toBe('10.11.2026');
        expect(element.querySelector<HTMLInputElement>('#wd-title')!.value).toBe('Шпакловка');
        expect(cards().length).toBe(2);
        expect(field(0, 'rate').value).toBe('7,00');
        expect(field(0, 'quantity').value).toBe('12,5');
        button('Позиция')!.click();
        await fixture.whenStable();
        await fillFreeText(2, 'Ново', '1');

        await submit();

        expect(saved[0].date).toBe('2026-11-10');
        expect(saved[0].items.map((item) => [item.id, item.description, item.rate])).toEqual([
            ['i1', 'Шпакловка', 7],
            ['i2', 'Извозване', null],
            [null, 'Ново', null],
        ]);
    });

    it('shows server errors under the matching item field, and others in the alert', async () => {
        button('Позиция')!.click();
        await fixture.whenStable();
        await set('fieldErrors', {
            'items[1].quantity': 'Количеството трябва да е до 3 знака след запетаята',
            date: 'Датата не може да е в бъдещето',
            items: 'Най-много 50 позиции',
            other: 'Нещо друго',
        });
        await set('error', 'Грешка при записа');

        expect(text(field(1, 'quantity').closest('nz-form-item'))).toContain(
            'Количеството трябва да е до 3 знака след запетаята',
        );
        expect(text(field(0, 'quantity').closest('nz-form-item'))).not.toContain('Количеството');
        expect(text(element.querySelector('#wd-date')!.closest('nz-form-item'))).toContain(
            'Датата не може да е в бъдещето',
        );
        expect(text(element.querySelector('.items'))).toContain('Най-много 50 позиции');
        const alert = text(element.querySelector('nz-alert'));
        expect(alert).toContain('Грешка при записа');
        expect(alert).toContain('Нещо друго');
    });

    it('delete needs a second click and only with canDelete when editing; Отказ emits cancel', async () => {
        await set('day', testWorkDay());
        expect(button('Изтрий')).toBeUndefined();

        await set('canDelete', true);
        button('Изтрий')!.click();
        await fixture.whenStable();
        expect(deletes).toBe(0);
        button('Потвърди изтриване')!.click();
        expect(deletes).toBe(1);

        button('Отказ')!.click();
        expect(cancels).toBe(1);
    });
});
