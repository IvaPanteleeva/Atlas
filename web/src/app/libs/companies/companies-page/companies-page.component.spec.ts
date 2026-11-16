import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { Observable, of, throwError } from 'rxjs';
import { routes } from '../../../app.routes';
import { AuthStore } from '../../auth/+state/auth.store';
import { Me, Role } from '../../auth/auth.model';
import { AuthService } from '../../auth/auth.service';
import { APP_ICONS } from '../../shared/icons';
import { CompaniesService } from '../companies.service';
import { Company, SaveCompanyRequest } from '../company.model';

class FakeAuthService {
    role: Role = 'Owner';

    me(): Observable<Me> {
        return of({ id: '1', userName: 'iva', role: this.role, companyId: null });
    }
}

class FakeCompaniesService {
    listResult: Observable<Company[]> = of([
        { id: 'a', name: 'Алфа' },
        { id: 'v', name: 'Вега' },
    ]);
    createResult: Observable<Company> = of({ id: 'b', name: 'Бета' });
    updateResult: Observable<Company> = of({ id: 'a', name: 'Гама' });
    removeResult: Observable<void> = of(undefined);
    calls: string[] = [];

    list(): Observable<Company[]> {
        this.calls.push('list');
        return this.listResult;
    }

    create(request: SaveCompanyRequest): Observable<Company> {
        this.calls.push(`create ${request.name}`);
        return this.createResult;
    }

    update(id: string, request: SaveCompanyRequest): Observable<Company> {
        this.calls.push(`update ${id} ${request.name}`);
        return this.updateResult;
    }

    remove(id: string): Observable<void> {
        this.calls.push(`remove ${id}`);
        return this.removeResult;
    }
}

describe('CompaniesPageComponent', () => {
    let fake: FakeCompaniesService;
    let auth: FakeAuthService;

    beforeEach(async () => {
        fake = new FakeCompaniesService();
        auth = new FakeAuthService();
        TestBed.configureTestingModule({
            providers: [
                provideRouter(routes, withComponentInputBinding()),
                provideNzIcons(APP_ICONS),
                { provide: AuthService, useValue: auth },
                { provide: CompaniesService, useValue: fake },
            ],
        });
        await TestBed.inject(AuthStore).loadMe();
    });

    async function render() {
        const harness = await RouterTestingHarness.create();
        await harness.navigateByUrl('/companies');
        await harness.fixture.whenStable();
        const element = harness.fixture.nativeElement as HTMLElement;
        const page = element.querySelector<HTMLElement>('atlas-companies-page')!;
        return { harness, element, page };
    }

    const names = (page: HTMLElement) =>
        Array.from(page.querySelectorAll('atlas-company-row .name')).map((name) => name.textContent?.trim());
    const button = (root: HTMLElement, label: string) =>
        Array.from(root.querySelectorAll<HTMLButtonElement>('button')).find(
            (candidate) => candidate.getAttribute('aria-label') === label || candidate.textContent?.trim() === label,
        )!;
    function type(input: HTMLInputElement, value: string) {
        input.value = value;
        input.dispatchEvent(new Event('input'));
    }

    it('loads and shows the companies with heading and intro, inside the shell', async () => {
        const { element, page } = await render();

        expect(element.querySelector('atlas-shell')).not.toBeNull();
        expect(page.querySelector('h1')?.textContent?.trim()).toBe('Фирми');
        expect(page.textContent).toContain('Фирмите, които ни възлагат работа. Използват се в обектите и в портала.');
        expect(fake.calls).toEqual(['list']);
        expect(names(page)).toEqual(['Алфа', 'Вега']);
        expect(document.title).toBe('Фирми · Атлас');
    });

    it('Manager opens the page, adds and renames, but has no delete buttons', async () => {
        auth.role = 'Manager';
        await TestBed.inject(AuthStore).loadMe();

        const { page } = await render();

        expect(names(page)).toEqual(['Алфа', 'Вега']);
        expect(button(page, 'Преименувай')).toBeDefined();
        expect(button(page, 'Изтрий')).toBeUndefined();
        expect(page.querySelector('atlas-company-add')).not.toBeNull();
    });

    it('shows "Няма фирми." for an empty list', async () => {
        fake.listResult = of([]);
        const { page } = await render();

        expect(page.textContent).toContain('Няма фирми.');
        expect(page.querySelectorAll('atlas-company-row').length).toBe(0);
    });

    it('adds a company, keeps the list sorted and clears the input', async () => {
        const { harness, page } = await render();
        const input = page.querySelector<HTMLInputElement>('atlas-company-add input')!;
        type(input, 'Бета');
        await harness.fixture.whenStable();

        button(page, 'Добави').click();
        await harness.fixture.whenStable();

        expect(fake.calls).toContain('create Бета');
        expect(names(page)).toEqual(['Алфа', 'Бета', 'Вега']);
        expect(input.value).toBe('');
    });

    it('shows a 409 in an alert above the list and keeps the typed name', async () => {
        fake.createResult = throwError(
            () => new HttpErrorResponse({ status: 409, error: { title: 'Фирма с това име вече има' } }),
        );
        const { harness, page } = await render();
        const input = page.querySelector<HTMLInputElement>('atlas-company-add input')!;
        type(input, 'Алфа');
        await harness.fixture.whenStable();

        button(page, 'Добави').click();
        await harness.fixture.whenStable();

        const alert = page.querySelector('nz-alert');
        expect(alert?.textContent).toContain('Фирма с това име вече има');
        expect(alert!.compareDocumentPosition(page.querySelector('.panel')!)).toBe(Node.DOCUMENT_POSITION_FOLLOWING);
        expect(input.value).toBe('Алфа');
    });

    it('renames a company inline', async () => {
        const { harness, page } = await render();

        button(page, 'Преименувай').click();
        await harness.fixture.whenStable();
        const input = page.querySelector<HTMLInputElement>('atlas-company-row input')!;
        expect(input.value).toBe('Алфа');
        type(input, 'Гама');
        await harness.fixture.whenStable();
        button(page, 'Запази').click();
        await harness.fixture.whenStable();

        expect(fake.calls).toContain('update a Гама');
        expect(names(page)).toEqual(['Вега', 'Гама']);
        expect(page.querySelector('atlas-company-row input')).toBeNull();
    });

    it('a failed rename stays in edit mode with the error', async () => {
        fake.updateResult = throwError(
            () => new HttpErrorResponse({ status: 409, error: { title: 'Фирма с това име вече има' } }),
        );
        const { harness, page } = await render();

        button(page, 'Преименувай').click();
        await harness.fixture.whenStable();
        type(page.querySelector<HTMLInputElement>('atlas-company-row input')!, 'Вега');
        await harness.fixture.whenStable();
        button(page, 'Запази').click();
        await harness.fixture.whenStable();

        expect(page.querySelector('nz-alert')?.textContent).toContain('Фирма с това име вече има');
        expect(page.querySelector<HTMLInputElement>('atlas-company-row input')?.value).toBe('Вега');
    });

    it('Отказ leaves edit mode without saving', async () => {
        const { harness, page } = await render();

        button(page, 'Преименувай').click();
        await harness.fixture.whenStable();
        button(page, 'Отказ').click();
        await harness.fixture.whenStable();

        expect(page.querySelector('atlas-company-row input')).toBeNull();
        expect(fake.calls).toEqual(['list']);
    });

    it('confirmed delete removes the company; 409 shows "Фирмата се използва"', async () => {
        fake.removeResult = throwError(
            () => new HttpErrorResponse({ status: 409, error: { title: 'Фирмата се използва' } }),
        );
        const { harness, page } = await render();

        button(page, 'Изтрий').click();
        await harness.fixture.whenStable();
        const ok = await vi.waitFor(() => {
            const found = Array.from(document.querySelectorAll<HTMLButtonElement>('.ant-popover button')).find(
                (candidate) => candidate.textContent?.trim() === 'Изтрий',
            );
            expect(found).toBeDefined();
            return found!;
        });
        ok.click();
        await harness.fixture.whenStable();

        expect(fake.calls).toContain('remove a');
        expect(page.querySelector('nz-alert')?.textContent).toContain('Фирмата се използва');
        expect(names(page)).toEqual(['Алфа', 'Вега']);
    });
});
