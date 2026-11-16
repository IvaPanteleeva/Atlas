import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { Service } from '../../services/service.model';
import { APP_ICONS } from '../../shared/icons';
import { CreateSitePriceRequest, SitePrice } from '../site-price.model';
import { testSitePrice } from '../testing/fake-site-prices.service';
import { SitePriceFormComponent } from './site-price-form.component';

const services: Service[] = [
    { id: 'sv1', name: 'Боядисване', unit: 'SquareMeter', defaultRate: 6 },
    { id: 'sv2', name: 'Первази', unit: 'LinearMeter', defaultRate: null },
    { id: 'sv3', name: 'Шпакловка', unit: 'SquareMeter', defaultRate: 8.5 },
];

/** Боядисване already has a price on this site. */
const prices: SitePrice[] = [testSitePrice({ id: 'p1', serviceId: 'sv1', serviceName: 'Боядисване', rate: 6.5 })];

describe('SitePriceFormComponent', () => {
    beforeAll(() => {
        Element.prototype.scrollTo ??= () => undefined;
    });

    let fixture: ComponentFixture<SitePriceFormComponent>;
    let element: HTMLElement;
    let saved: CreateSitePriceRequest[];
    let deletes: number;
    let cancels: number;

    beforeEach(async () => {
        TestBed.configureTestingModule({ providers: [provideNzIcons(APP_ICONS), provideRouter([])] });
        fixture = TestBed.createComponent(SitePriceFormComponent);
        saved = [];
        deletes = 0;
        cancels = 0;
        const form = fixture.componentInstance;
        form.save.subscribe((request) => saved.push(request));
        form.delete.subscribe(() => deletes++);
        form.cancel.subscribe(() => cancels++);
        fixture.componentRef.setInput('services', services);
        fixture.componentRef.setInput('prices', prices);
        element = fixture.nativeElement as HTMLElement;
        await fixture.whenStable();
    });

    async function set(name: string, value: unknown) {
        fixture.componentRef.setInput(name, value);
        await fixture.whenStable();
    }

    const rate = () => element.querySelector<HTMLInputElement>('#price-rate')!;
    const button = (label: string) =>
        Array.from(element.querySelectorAll<HTMLButtonElement>('button')).find(
            (candidate) => candidate.textContent?.trim() === label,
        );

    async function typeRate(value: string) {
        rate().value = value;
        rate().dispatchEvent(new Event('input'));
        await fixture.whenStable();
    }

    async function submit() {
        button('Запази')!.click();
        await fixture.whenStable();
    }

    async function openServiceSelect(): Promise<HTMLElement[]> {
        element.querySelector<HTMLElement>('nz-select#price-service .ant-select-selector')!.click();
        await fixture.whenStable();
        return vi.waitFor(() => {
            const found = Array.from(document.querySelectorAll<HTMLElement>('nz-option-item'));
            expect(found.length).toBeGreaterThan(0);
            return found;
        });
    }

    async function choose(label: string) {
        const option = (await openServiceSelect()).find((o) => o.textContent?.trim() === label)!;
        option.click();
        await fixture.whenStable();
    }

    it('offers only the services without a price on this site, as "name · unit"', async () => {
        const options = await openServiceSelect();

        expect(options.map((o) => o.textContent?.trim())).toEqual(['Первази · м.л.', 'Шпакловка · м²']);
    });

    it('choosing a service prefills its default price', async () => {
        await choose('Шпакловка · м²');

        expect(rate().value).toBe('8,50');
        await submit();
        expect(saved).toEqual([{ serviceId: 'sv3', rate: 8.5 }]);
    });

    it('a service without a default price leaves the price empty, and the price is required', async () => {
        await choose('Первази · м.л.');
        expect(rate().value).toBe('');

        await submit();

        expect(saved).toEqual([]);
        expect(element.textContent).toContain('Въведи цена');

        await typeRate('3,2');
        await submit();
        expect(saved).toEqual([{ serviceId: 'sv2', rate: 3.2 }]);
    });

    it('a price typed over the default is kept when the form is submitted', async () => {
        await choose('Шпакловка · м²');
        await typeRate('9');

        await submit();

        expect(saved).toEqual([{ serviceId: 'sv3', rate: 9 }]);
    });

    it('a price that is not a number is an error and is not saved', async () => {
        await choose('Шпакловка · м²');
        await typeRate('8,5 лв');

        await submit();

        expect(saved).toEqual([]);
        expect(rate().value).toBe('8,5 лв');
        expect(rate().closest('nz-form-item')?.textContent).toContain('Въведи число');
    });

    it('requires a service', async () => {
        await typeRate('5');

        await submit();

        expect(saved).toEqual([]);
        expect(element.textContent).toContain('Избери услуга');
    });

    it('when editing, the service is read-only and only the price changes; delete needs a second click', async () => {
        await set('price', prices[0]);

        expect(element.querySelector('nz-select#price-service')).toBeNull();
        expect(element.querySelector('#price-service')?.textContent?.replace(/\s+/g, ' ').trim()).toBe(
            'Боядисване · м²',
        );
        expect(rate().value).toBe('6,50');

        await typeRate('7');
        await submit();
        expect(saved).toEqual([{ serviceId: 'sv1', rate: 7 }]);
        expect(button('Изтрий')).toBeUndefined();

        await set('canDelete', true);
        button('Изтрий')!.click();
        await fixture.whenStable();
        expect(deletes).toBe(0);
        button('Потвърди изтриване')!.click();
        expect(deletes).toBe(1);
    });

    it('a new price has no delete button; Отказ emits cancel', () => {
        expect(button('Изтрий')).toBeUndefined();

        button('Отказ')!.click();

        expect(cancels).toBe(1);
    });

    it('says so when every service already has a price', async () => {
        await set('prices', [
            ...prices,
            testSitePrice({ id: 'p2', serviceId: 'sv2' }),
            testSitePrice({ id: 'p3', serviceId: 'sv3' }),
        ]);

        expect(element.textContent).toContain('Всички услуги вече имат цена за обекта.');
        expect(element.querySelector('nz-select')).toBeNull();
        expect(button('Запази')).toBeUndefined();
        expect(button('Отказ')).toBeDefined();
    });

    it('links to /services when there are no services at all', async () => {
        await set('prices', []);
        await set('services', []);

        const link = Array.from(element.querySelectorAll('a')).find((a) => a.textContent?.trim() === 'Добави услуги');
        expect(link?.getAttribute('href')).toBe('/services');
        expect(button('Запази')).toBeUndefined();
    });

    it('shows server errors (rate under the field, others in the alert)', async () => {
        await set('fieldErrors', { rate: 'Цената е твърде голяма' });
        await set('error', 'Тази услуга вече има цена за обекта');

        expect(rate().closest('nz-form-item')?.textContent).toContain('Цената е твърде голяма');
        expect(element.querySelector('nz-alert')?.textContent).toContain('Тази услуга вече има цена за обекта');
    });
});
