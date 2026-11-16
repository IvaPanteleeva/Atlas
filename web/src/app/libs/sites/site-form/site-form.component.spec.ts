import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { Company } from '../../companies/company.model';
import { APP_ICONS } from '../../shared/icons';
import { SaveSiteRequest, Site } from '../site.model';
import { SiteFormComponent } from './site-form.component';

const companies: Company[] = [
    { id: 'c1', name: 'Женистрой' },
    { id: 'c2', name: 'Строй ООД' },
];

const site: Site = {
    id: 's1',
    name: 'Бункера',
    status: 'Active',
    companyId: 'c2',
    companyName: 'Строй ООД',
    endCustomer: 'Иван',
    address: 'ул. Шипка 5',
    mapUrl: 'https://maps.app.goo.gl/abc',
    note: 'Ключ при съседа',
};

describe('SiteFormComponent', () => {
    // jsdom has no Element.scrollTo; the nz-select dropdown (CDK virtual scroll) calls it.
    beforeAll(() => {
        Element.prototype.scrollTo ??= () => undefined;
    });

    let fixture: ComponentFixture<SiteFormComponent>;
    let element: HTMLElement;
    let saved: SaveSiteRequest[];
    let createdCompanies: string[];
    let deletes: number;
    let cancels: number;

    beforeEach(async () => {
        TestBed.configureTestingModule({ providers: [provideNzIcons(APP_ICONS)] });
        fixture = TestBed.createComponent(SiteFormComponent);
        saved = [];
        createdCompanies = [];
        deletes = 0;
        cancels = 0;
        const form = fixture.componentInstance;
        form.save.subscribe((request) => saved.push(request));
        form.createCompany.subscribe((name) => createdCompanies.push(name));
        form.delete.subscribe(() => deletes++);
        form.cancel.subscribe(() => cancels++);
        fixture.componentRef.setInput('companies', companies);
        element = fixture.nativeElement as HTMLElement;
        await fixture.whenStable();
    });

    async function set(name: string, value: unknown) {
        fixture.componentRef.setInput(name, value);
        await fixture.whenStable();
    }

    const field = (id: string) => element.querySelector<HTMLInputElement & HTMLTextAreaElement>(`#${id}`)!;
    const button = (label: string) =>
        Array.from(element.querySelectorAll<HTMLButtonElement>('button')).find(
            (candidate) => candidate.textContent?.trim() === label || candidate.getAttribute('aria-label') === label,
        );
    const selected = (selectId: string) =>
        element.querySelector(`nz-select#${selectId} .ant-select-selection-item`)?.textContent?.trim() ?? null;

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

    /** Opens an nz-select and clicks the option with the given label (options live in the CDK overlay). */
    async function choose(selectId: string, label: string) {
        element.querySelector<HTMLElement>(`nz-select#${selectId} .ant-select-selector`)!.click();
        await fixture.whenStable();
        const option = await vi.waitFor(() => {
            const found = Array.from(document.querySelectorAll<HTMLElement>('nz-option-item')).find(
                (candidate) => candidate.textContent?.trim() === label,
            );
            expect(found).toBeDefined();
            return found!;
        });
        option.click();
        await fixture.whenStable();
    }

    it('has the fields with placeholders and the note hint', () => {
        expect(field('site-name').placeholder).toBe('напр. Професора');
        expect(field('site-map-url').placeholder).toBe('https://maps.app.goo.gl/…');
        expect(field('site-note').tagName).toBe('TEXTAREA');
        expect(element.textContent).toContain('Само за нас, не се вижда от възложителя');
        expect(field('site-name').maxLength).toBe(200);
        expect(field('site-end-customer').maxLength).toBe(200);
        expect(field('site-address').maxLength).toBe(300);
        expect(field('site-map-url').maxLength).toBe(500);
        expect(field('site-note').maxLength).toBe(2000);
    });

    it('a new site defaults to "Оглед / оферта" and no company', () => {
        expect(selected('site-status')).toBe('Оглед / оферта');
        expect(selected('site-company')).toBe('Без фирма');
    });

    it('requires a name (blank does not count)', async () => {
        await type('site-name', '   ');

        await submit();

        expect(saved).toEqual([]);
        expect(element.textContent).toContain('Въведи име на обекта');
    });

    it('shows the name error on submit even when the field was never touched', async () => {
        await submit();

        expect(saved).toEqual([]);
        expect(element.textContent).toContain('Въведи име на обекта');
    });

    it('sends trimmed values and empty optional fields as null', async () => {
        await type('site-name', '  Професора ');
        await type('site-address', '   ');

        await submit();

        expect(saved).toEqual([
            {
                name: 'Професора',
                status: 'Lead',
                companyId: null,
                endCustomer: null,
                address: null,
                mapUrl: null,
                note: null,
            },
        ]);
    });

    it('the map link must start with http:// or https://', async () => {
        await type('site-name', 'Професора');
        await type('site-map-url', 'maps.app.goo.gl/abc');

        await submit();

        expect(saved).toEqual([]);
        expect(element.textContent).toContain('Линкът трябва да започва с http:// или https://');

        await type('site-map-url', ' https://maps.app.goo.gl/abc ');
        await submit();

        expect(saved[0].mapUrl).toBe('https://maps.app.goo.gl/abc');
    });

    it('fills the fields from the site when editing and keeps them on save', async () => {
        await set('site', site);

        expect(field('site-name').value).toBe('Бункера');
        expect(selected('site-status')).toBe('Текущ');
        expect(selected('site-company')).toBe('Строй ООД');
        expect(field('site-note').value).toBe('Ключ при съседа');

        await submit();

        expect(saved).toEqual([
            {
                name: 'Бункера',
                status: 'Active',
                companyId: 'c2',
                endCustomer: 'Иван',
                address: 'ул. Шипка 5',
                mapUrl: 'https://maps.app.goo.gl/abc',
                note: 'Ключ при съседа',
            },
        ]);
    });

    it('status and company are chosen from the selects', async () => {
        await type('site-name', 'Професора');

        await choose('site-status', 'Приключен');
        await choose('site-company', 'Женистрой');
        await submit();

        expect(saved[0].status).toBe('Done');
        expect(saved[0].companyId).toBe('c1');
    });

    it('"+ Нова фирма" is offered only when canCreateCompany', async () => {
        element.querySelector<HTMLElement>('nz-select#site-company .ant-select-selector')!.click();
        await fixture.whenStable();
        const labels = await vi.waitFor(() => {
            const found = Array.from(document.querySelectorAll('nz-option-item')).map((o) => o.textContent?.trim());
            expect(found.length).toBeGreaterThan(0);
            return found;
        });

        expect(labels).toEqual(['Без фирма', 'Женистрой', 'Строй ООД']);
    });

    it('"+ Нова фирма" shows an inline field, emits createCompany and selects the created company', async () => {
        await set('canCreateCompany', true);
        await type('site-name', 'Професора');

        await choose('site-company', '+ Нова фирма');
        expect(selected('site-company')).toBe('Без фирма');
        await type('site-new-company', '  Нова фирма ');
        button('Добави')!.click();
        await fixture.whenStable();

        expect(createdCompanies).toEqual(['Нова фирма']);

        // The page creates it and passes it back.
        await set('companies', [...companies, { id: 'c3', name: 'Нова фирма' }]);
        await set('createdCompany', { id: 'c3', name: 'Нова фирма' });

        expect(field('site-new-company')).toBeNull();
        expect(selected('site-company')).toBe('Нова фирма');
        await submit();
        expect(saved[0].companyId).toBe('c3');
    });

    it('shows the company error under the inline field', async () => {
        await set('canCreateCompany', true);
        await choose('site-company', '+ Нова фирма');

        await set('companyError', 'Фирма с това име вече има');

        expect(element.querySelector('.new-company')?.textContent).toContain('Фирма с това име вече има');
    });

    it('delete is shown only when canDelete and needs a second click', async () => {
        expect(button('Изтрий')).toBeUndefined();

        await set('canDelete', true);
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

    it('shows server field errors under their fields and other errors in the alert', async () => {
        await set('fieldErrors', { name: 'Името е твърде дълго', mapUrl: 'Невалиден линк', other: 'Друга грешка' });
        await set('error', null);

        expect(field('site-name').closest('nz-form-item')?.textContent).toContain('Името е твърде дълго');
        expect(field('site-map-url').closest('nz-form-item')?.textContent).toContain('Невалиден линк');
        expect(element.querySelector('nz-alert')?.textContent).toContain('Друга грешка');
        expect(element.querySelector('nz-alert')?.textContent).not.toContain('Невалиден линк');
    });

    it('shows the general error in an alert', async () => {
        await set('error', 'Обектът има работни дни');

        expect(element.querySelector('nz-alert')?.textContent).toContain('Обектът има работни дни');
    });
});
