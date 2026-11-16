import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { APP_ICONS } from '../../shared/icons';
import { SaveServiceRequest, Service } from '../service.model';
import { ServiceFormComponent } from './service-form.component';

const service: Service = { id: 'sv1', name: 'Шпакловка', unit: 'LinearMeter', defaultRate: 6.5 };

describe('ServiceFormComponent', () => {
    // jsdom has no Element.scrollTo; the nz-select dropdown (CDK virtual scroll) calls it.
    beforeAll(() => {
        Element.prototype.scrollTo ??= () => undefined;
    });

    let fixture: ComponentFixture<ServiceFormComponent>;
    let element: HTMLElement;
    let saved: SaveServiceRequest[];
    let deletes: number;
    let cancels: number;

    beforeEach(async () => {
        TestBed.configureTestingModule({ providers: [provideNzIcons(APP_ICONS)] });
        fixture = TestBed.createComponent(ServiceFormComponent);
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

    async function set(name: string, value: unknown) {
        fixture.componentRef.setInput(name, value);
        await fixture.whenStable();
    }

    const field = (id: string) => element.querySelector<HTMLInputElement>(`#${id}`)!;
    const button = (label: string) =>
        Array.from(element.querySelectorAll<HTMLButtonElement>('button')).find(
            (candidate) => candidate.textContent?.trim() === label,
        );
    const selectedUnit = () =>
        element.querySelector('nz-select#service-unit .ant-select-selection-item')?.textContent?.trim() ?? null;

    async function type(id: string, value: string) {
        const input = field(id);
        input.value = value;
        input.dispatchEvent(new Event('input'));
        await fixture.whenStable();
    }

    async function submit() {
        button('Запази')!.click();
        await fixture.whenStable();
    }

    it('has the fields, the placeholder, the € suffix and the price hint', () => {
        expect(field('service-name').placeholder).toBe('напр. Шпакловка');
        expect(field('service-name').maxLength).toBe(200);
        expect(field('service-rate')).toBeTruthy();
        expect(element.querySelector('nz-input-wrapper')?.textContent).toContain('€');
        expect(element.textContent).toContain('По избор. Ползва се, когато обектът няма собствена цена.');
    });

    it('a new service defaults to м² and has no delete button', () => {
        expect(selectedUnit()).toBe('м²');
        expect(button('Изтрий')).toBeUndefined();
    });

    it('requires a name (blank does not count)', async () => {
        await type('service-name', '   ');

        await submit();

        expect(saved).toEqual([]);
        expect(element.textContent).toContain('Въведи име на услугата');
    });

    it('requires a unit', async () => {
        await type('service-name', 'Шпакловка');
        fixture.componentInstance['form'].controls.unit.setValue(null);

        await submit();

        expect(saved).toEqual([]);
        expect(element.textContent).toContain('Избери мярка');
    });

    it('the default price is optional (sent as null) and the name is trimmed', async () => {
        await type('service-name', '  Шпакловка ');

        await submit();

        expect(saved).toEqual([{ name: 'Шпакловка', unit: 'SquareMeter', defaultRate: null }]);
    });

    it('sends the typed price; a decimal comma works too', async () => {
        await type('service-name', 'Шпакловка');
        await type('service-rate', '6,5');

        await submit();

        expect(saved).toEqual([{ name: 'Шпакловка', unit: 'SquareMeter', defaultRate: 6.5 }]);
    });

    it('the unit is chosen from the select (м.л., м², ден, бр.)', async () => {
        await type('service-name', 'Первази');
        element.querySelector<HTMLElement>('nz-select#service-unit .ant-select-selector')!.click();
        await fixture.whenStable();
        const options = await vi.waitFor(() => {
            const found = Array.from(document.querySelectorAll<HTMLElement>('nz-option-item'));
            expect(found.length).toBe(4);
            return found;
        });
        expect(options.map((o) => o.textContent?.trim())).toEqual(['м.л.', 'м²', 'ден', 'бр.']);

        options[0].click();
        await fixture.whenStable();
        await submit();

        expect(saved[0].unit).toBe('LinearMeter');
    });

    it('fills the fields when editing and keeps them on save', async () => {
        await set('service', service);

        expect(field('service-name').value).toBe('Шпакловка');
        expect(selectedUnit()).toBe('м.л.');
        expect(field('service-rate').value).toBe('6,50');

        await submit();

        expect(saved).toEqual([{ name: 'Шпакловка', unit: 'LinearMeter', defaultRate: 6.5 }]);
    });

    it('delete is shown when editing and needs a second click', async () => {
        await set('service', service);

        button('Изтрий')!.click();
        await fixture.whenStable();
        expect(deletes).toBe(0);
        expect(button('Изтрий')).toBeUndefined();

        button('Потвърди изтриване')!.click();

        expect(deletes).toBe(1);
    });

    it('Отказ emits cancel', () => {
        button('Отказ')!.click();

        expect(cancels).toBe(1);
    });

    it('shows server field errors under their fields and the general error in an alert', async () => {
        await set('fieldErrors', { name: 'Името е твърде дълго', defaultRate: 'Невалидна цена', other: 'Друго' });
        await set('error', 'Тази услуга вече я има');

        expect(field('service-name').closest('nz-form-item')?.textContent).toContain('Името е твърде дълго');
        expect(element.querySelector('nz-input-wrapper')?.closest('nz-form-item')?.textContent).toContain(
            'Невалидна цена',
        );
        const alert = element.querySelector('nz-alert')?.textContent;
        expect(alert).toContain('Тази услуга вече я има');
        expect(alert).toContain('Друго');
        expect(alert).not.toContain('Невалидна цена');
    });
});
