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
import { APP_ICONS } from '../../shared/icons';
import { SitesService } from '../sites.service';
import { FakeSitesService, httpError, testSite } from '../testing/fake-sites.service';

describe('SiteDetailPageComponent', () => {
    let fake: FakeSitesService;
    let role: Role;

    beforeEach(() => {
        fake = new FakeSitesService();
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
                { provide: CompaniesService, useValue: { list: () => of([{ id: 'c1', name: 'Женистрой' }]) } },
            ],
        });
    });

    const path = () => TestBed.inject(Location).path();
    const button = (root: ParentNode, label: string) =>
        Array.from(root.querySelectorAll<HTMLButtonElement>('button')).find(
            (candidate) => candidate.textContent?.trim() === label || candidate.getAttribute('aria-label') === label,
        );

    async function render(url = '/sites/s1') {
        await TestBed.inject(AuthStore).loadMe();
        const harness = await RouterTestingHarness.create();
        await harness.navigateByUrl(url);
        await harness.fixture.whenStable();
        const element = harness.fixture.nativeElement as HTMLElement;
        return { harness, page: element.querySelector<HTMLElement>('atlas-site-detail-page')! };
    }

    async function openForm(harness: RouterTestingHarness, page: HTMLElement) {
        button(page, 'Данни за обекта')!.click();
        await harness.fixture.whenStable();
        return vi.waitFor(() => {
            const form = document.querySelector<HTMLElement>('.atlas-sheet atlas-site-form');
            expect(form).toBeTruthy();
            return form!;
        });
    }

    it('loads the site by id and shows its header', async () => {
        fake.getResult = of(testSite({ note: 'Ключ при съседа' }));

        const { page } = await render();

        expect(fake.calls).toEqual(['get s1']);
        expect(page.querySelector('atlas-site-header h1')?.textContent?.trim()).toBe('Бункера');
        expect(page.textContent).toContain('Ключ при съседа');
        expect(document.title).toBe('Обект · Атлас');
    });

    it('"Всички обекти" goes back to the list', async () => {
        const { harness, page } = await render();

        page.querySelector<HTMLAnchorElement>('a.back')!.click();
        await harness.fixture.whenStable();

        expect(path()).toBe('/sites');
    });

    it('404 shows "Няма такъв обект." with a link to the list', async () => {
        fake.getResult = httpError(404, { title: 'Not Found' });

        const { page } = await render('/sites/missing');

        expect(page.textContent).toContain('Няма такъв обект.');
        expect(page.querySelector('atlas-site-header')).toBeNull();
        expect(page.querySelector('.not-found a')?.getAttribute('href')).toBe('/sites');
    });

    it('"Данни за обекта" edits the site and updates the header', async () => {
        const { harness, page } = await render();
        const form = await openForm(harness, page);
        expect(form.querySelector<HTMLInputElement>('#site-name')?.value).toBe('Бункера');

        const input = form.querySelector<HTMLInputElement>('#site-name')!;
        input.value = 'Бункера 2';
        input.dispatchEvent(new Event('input'));
        await harness.fixture.whenStable();
        button(form, 'Запази')!.click();
        await harness.fixture.whenStable();

        expect(fake.calls).toContain('update s1 Бункера 2');
        expect(page.querySelector('atlas-site-header h1')?.textContent?.trim()).toBe('Бункера 2');
        await vi.waitFor(() => expect(document.querySelector('.atlas-sheet atlas-site-form')).toBeNull());
    });

    it('Crew sees no delete button', async () => {
        role = 'Crew';
        const { harness, page } = await render();

        const form = await openForm(harness, page);

        expect(button(form, 'Изтрий')).toBeUndefined();
        expect(button(form, 'Запази')).toBeDefined();
    });

    it('Owner deletes after confirming and goes to /sites', async () => {
        const { harness, page } = await render();
        const form = await openForm(harness, page);

        button(form, 'Изтрий')!.click();
        await harness.fixture.whenStable();
        expect(fake.calls).not.toContain('remove s1');
        button(form, 'Потвърди изтриване')!.click();
        await harness.fixture.whenStable();

        expect(fake.calls).toContain('remove s1');
        await vi.waitFor(() => expect(path()).toBe('/sites'));
    });

    it('a refused delete (409) stays and shows the reason in the form', async () => {
        fake.removeResult = httpError(409, { title: 'Обектът има работни дни или плащания' });
        const { harness, page } = await render();
        const form = await openForm(harness, page);

        button(form, 'Изтрий')!.click();
        await harness.fixture.whenStable();
        button(form, 'Потвърди изтриване')!.click();
        await harness.fixture.whenStable();

        expect(path()).toBe('/sites/s1');
        expect(form.querySelector('nz-alert')?.textContent).toContain('Обектът има работни дни или плащания');
    });
});
