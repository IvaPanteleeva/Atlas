import { Location } from '@angular/common';
import { ANIMATION_MODULE_TYPE } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { Observable, of } from 'rxjs';
import { routes } from '../../../app.routes';
import { AuthStore } from '../../auth/+state/auth.store';
import { Me, Role } from '../../auth/auth.model';
import { AuthService } from '../../auth/auth.service';
import { CompaniesService } from '../../companies/companies.service';
import { Company } from '../../companies/company.model';
import { APP_ICONS } from '../../shared/icons';
import { SitesService } from '../sites.service';
import { FakeSitesService, httpError } from '../testing/fake-sites.service';

class FakeCompaniesService {
    calls: string[] = [];
    list(): Observable<Company[]> {
        this.calls.push('list');
        return of([{ id: 'c1', name: 'Женистрой' }]);
    }
    create(request: { name: string }): Observable<Company> {
        this.calls.push(`create ${request.name}`);
        return of({ id: 'c9', name: request.name });
    }
}

describe('SitesPageComponent', () => {
    let fake: FakeSitesService;
    let companies: FakeCompaniesService;
    let role: Role;

    beforeAll(() => {
        Element.prototype.scrollTo ??= () => undefined;
    });

    beforeEach(() => {
        fake = new FakeSitesService();
        companies = new FakeCompaniesService();
        role = 'Owner';
        TestBed.configureTestingModule({
            providers: [
                provideRouter(routes, withComponentInputBinding()),
                provideNzIcons(APP_ICONS),
                // jsdom fires no animationend, so the modal would never finish closing.
                { provide: ANIMATION_MODULE_TYPE, useValue: 'NoopAnimations' },
                {
                    provide: AuthService,
                    useValue: { me: (): Observable<Me> => of({ id: '1', userName: 'iva', role, companyId: null }) },
                },
                { provide: SitesService, useValue: fake },
                { provide: CompaniesService, useValue: companies },
            ],
        });
    });

    const path = () => TestBed.inject(Location).path();
    const button = (root: ParentNode, label: string) =>
        Array.from(root.querySelectorAll<HTMLButtonElement>('button')).find(
            (candidate) => candidate.textContent?.trim() === label || candidate.getAttribute('aria-label') === label,
        );
    const sheet = () => document.querySelector<HTMLElement>('.atlas-sheet');

    async function render() {
        await TestBed.inject(AuthStore).loadMe();
        const harness = await RouterTestingHarness.create();
        await harness.navigateByUrl('/sites');
        await harness.fixture.whenStable();
        const element = harness.fixture.nativeElement as HTMLElement;
        return { harness, element, page: element.querySelector<HTMLElement>('atlas-sites-page')! };
    }

    async function openForm(harness: RouterTestingHarness, page: HTMLElement) {
        button(page, 'Нов обект')!.click();
        await harness.fixture.whenStable();
        return vi.waitFor(() => {
            const form = sheet()?.querySelector<HTMLElement>('atlas-site-form');
            expect(form).toBeTruthy();
            return form!;
        });
    }

    function type(root: ParentNode, selector: string, value: string) {
        const input = root.querySelector<HTMLInputElement>(selector)!;
        input.value = value;
        input.dispatchEvent(new Event('input'));
    }

    it('shows the heading, the button and the list inside the shell', async () => {
        const { element, page } = await render();

        expect(element.querySelector('atlas-shell')).not.toBeNull();
        expect(page.querySelector('h1')?.textContent?.trim()).toBe('Обекти');
        expect(button(page, 'Нов обект')).toBeDefined();
        expect(fake.calls).toEqual(['list']);
        expect(Array.from(page.querySelectorAll('.site .name')).map((n) => n.textContent?.trim())).toEqual([
            'Бункера',
            'Професора',
        ]);
        expect(document.title).toBe('Обекти · Атлас');
    });

    it('shows a load error', async () => {
        fake.listResult = httpError(500);

        const { page } = await render();

        expect(page.querySelector('nz-alert')?.textContent).toContain('Грешка при зареждане на обектите');
    });

    it('clicking a site opens its detail page', async () => {
        const { harness, page } = await render();

        page.querySelectorAll<HTMLButtonElement>('button.site')[1].click();
        await harness.fixture.whenStable();

        expect(path()).toBe('/sites/s2');
    });

    it('"Нов обект" opens the form (new = Оглед / оферта), loads companies, saves and goes to the new site', async () => {
        const { harness, page } = await render();

        const form = await openForm(harness, page);
        expect(sheet()?.textContent).toContain('Нов обект');
        expect(companies.calls).toContain('list');
        expect(form.querySelector('nz-select#site-status .ant-select-selection-item')?.textContent?.trim()).toBe(
            'Оглед / оферта',
        );
        expect(button(form, 'Изтрий')).toBeUndefined();

        type(form, '#site-name', 'Нов');
        await harness.fixture.whenStable();
        button(form, 'Запази')!.click();
        await harness.fixture.whenStable();

        expect(fake.calls).toContain('create Нов');
        expect(fake.lastRequest?.status).toBe('Lead');
        await vi.waitFor(() => expect(path()).toBe('/sites/new'));
    });

    it('a 400 keeps the form open and shows the message under the field', async () => {
        fake.createResult = httpError(400, { errors: { name: ['Името е твърде дълго'] } });
        const { harness, page } = await render();
        const form = await openForm(harness, page);

        type(form, '#site-name', 'Нов');
        await harness.fixture.whenStable();
        button(form, 'Запази')!.click();
        await harness.fixture.whenStable();

        expect(path()).toBe('/sites');
        expect(form.querySelector('.server-error')?.textContent).toContain('Името е твърде дълго');
    });

    it('Owner adds a company from the form and it gets selected', async () => {
        const { harness, page } = await render();
        const form = await openForm(harness, page);

        form.querySelector<HTMLElement>('nz-select#site-company .ant-select-selector')!.click();
        await harness.fixture.whenStable();
        const option = await vi.waitFor(() => {
            const found = Array.from(document.querySelectorAll<HTMLElement>('nz-option-item')).find(
                (candidate) => candidate.textContent?.trim() === '+ Нова фирма',
            );
            expect(found).toBeDefined();
            return found!;
        });
        option.click();
        await harness.fixture.whenStable();
        type(form, '#site-new-company', 'Строй ООД');
        await harness.fixture.whenStable();
        button(form, 'Добави')!.click();
        await harness.fixture.whenStable();

        expect(companies.calls).toContain('create Строй ООД');
        await vi.waitFor(() =>
            expect(form.querySelector('nz-select#site-company .ant-select-selection-item')?.textContent?.trim()).toBe(
                'Строй ООД',
            ),
        );
    });

    it('Crew is not offered "+ Нова фирма"', async () => {
        role = 'Crew';
        const { harness, page } = await render();
        const form = await openForm(harness, page);

        form.querySelector<HTMLElement>('nz-select#site-company .ant-select-selector')!.click();
        await harness.fixture.whenStable();
        const labels = await vi.waitFor(() => {
            const found = Array.from(document.querySelectorAll('nz-option-item')).map((o) => o.textContent?.trim());
            expect(found).toContain('Женистрой');
            return found;
        });

        expect(labels).not.toContain('+ Нова фирма');
    });

    it('Отказ closes the form without saving', async () => {
        const { harness, page } = await render();
        const form = await openForm(harness, page);

        button(form, 'Отказ')!.click();
        await harness.fixture.whenStable();

        await vi.waitFor(() => expect(document.querySelector('.atlas-sheet atlas-site-form')).toBeNull());
        expect(fake.calls).toEqual(['list']);
    });
});
