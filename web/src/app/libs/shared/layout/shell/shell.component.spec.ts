import { Location } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { Observable, of, throwError } from 'rxjs';
import { routes } from '../../../../app.routes';
import { AuthStore } from '../../../auth/+state/auth.store';
import { Me, Role } from '../../../auth/auth.model';
import { AuthService } from '../../../auth/auth.service';
import { CompaniesService } from '../../../companies/companies.service';
import { ServicesService } from '../../../services/services.service';
import { SitesService } from '../../../sites/sites.service';
import { APP_ICONS } from '../../icons';

class FakeAuthService {
    meResult: Observable<Me> = throwError(() => new HttpErrorResponse({ status: 401 }));
    logoutCalls = 0;

    me(): Observable<Me> {
        return this.meResult;
    }

    logout(): Observable<void> {
        this.logoutCalls++;
        return of(undefined);
    }
}

describe('ShellComponent', () => {
    let fake: FakeAuthService;

    beforeEach(() => {
        fake = new FakeAuthService();
        TestBed.configureTestingModule({
            providers: [
                provideRouter(routes, withComponentInputBinding()),
                provideNzIcons(APP_ICONS),
                { provide: AuthService, useValue: fake },
                { provide: CompaniesService, useValue: { list: () => of([]) } },
                // /sites is the start page; it loads the list.
                { provide: SitesService, useValue: { list: () => of([]) } },
                { provide: ServicesService, useValue: { list: () => of([]) } },
            ],
        });
    });

    async function render(url: string, role: Role | null = 'Owner', userName = 'iva') {
        if (role) {
            fake.meResult = of({ id: '1', userName, role, companyId: null });
        }
        await TestBed.inject(AuthStore).loadMe();
        const harness = await RouterTestingHarness.create();
        await harness.navigateByUrl(url);
        return { harness, element: harness.fixture.nativeElement as HTMLElement };
    }

    const path = () => TestBed.inject(Location).path();

    it('renders the four tabs with labels and links in both navs', async () => {
        const { element } = await render('/sites');

        for (const selector of ['.tabs--top', '.tabs--bottom']) {
            const links = Array.from(element.querySelectorAll<HTMLAnchorElement>(`${selector} a`));
            expect(links.map((a) => a.textContent?.trim())).toEqual(['Обекти', 'Хора', 'Задачи', 'Услуги']);
            expect(links.map((a) => a.getAttribute('href'))).toEqual(['/sites', '/people', '/tasks', '/services']);
        }
    });

    it('marks the "Хора" tab as current after navigating to /people', async () => {
        const { element } = await render('/people');
        expect(element.querySelector('.content h1')?.textContent).toBe('Хора');

        const current = Array.from(element.querySelectorAll('.tabs--bottom a[aria-current="page"]'));
        expect(current.map((a) => a.textContent?.trim())).toEqual(['Хора']);
        const currentTop = element.querySelector('.tabs--top a[aria-current="page"]');
        expect(currentTop?.textContent?.trim()).toBe('Хора');
    });

    it('the old /rate-card link goes to /services, and the "Услуги" tab is current', async () => {
        const { element } = await render('/rate-card');

        expect(path()).toBe('/services');
        expect(element.querySelector('.tabs--bottom a[aria-current="page"]')?.textContent?.trim()).toBe('Услуги');
    });

    it('redirects / to /sites', async () => {
        await render('/');
        expect(path()).toBe('/sites');
    });

    it('shows the user name in the user menu', async () => {
        const { element } = await render('/sites', 'Crew', 'petar');

        expect(element.querySelector('.user__name')?.textContent?.trim()).toBe('petar');
    });

    /** Opens the user dropdown and returns the texts of its items once it is rendered. */
    async function openUserMenu(element: HTMLElement): Promise<HTMLElement[]> {
        element.querySelector<HTMLButtonElement>('button.user')!.click();
        // nz-dropdown opens after auditTime(150), in a CDK overlay attached to document.body.
        return vi.waitFor(() => {
            const items = Array.from(document.querySelectorAll<HTMLElement>('[nz-menu-item]'));
            expect(items.some((item) => item.textContent?.trim() === 'Изход')).toBe(true);
            return items;
        });
    }

    const itemText = (item: HTMLElement) => item.textContent?.trim();

    it('"Изход" in the user menu signs out and goes to /login', async () => {
        const { harness, element } = await render('/sites');

        const logout = (await openUserMenu(element)).find((item) => itemText(item) === 'Изход')!;
        logout.click();
        await harness.fixture.whenStable();

        expect(fake.logoutCalls).toBe(1);
        expect(TestBed.inject(AuthStore).isAuthenticated()).toBe(false);
        expect(path()).toBe('/login');
    });

    it('ClientViewer sees the portal placeholder without tabs', async () => {
        const { element } = await render('/portal', 'ClientViewer');

        expect(path()).toBe('/portal');
        expect(element.querySelector('.content h1')?.textContent).toBe('Порталът идва скоро');
        expect(element.querySelectorAll('.tabs a').length).toBe(0);
        expect(element.querySelector('.brand')?.getAttribute('href')).toBe('/portal');
    });

    it('ClientViewer on a staff route is sent to /portal', async () => {
        await render('/sites', 'ClientViewer');
        expect(path()).toBe('/portal');
    });

    it('Owner on /portal is sent to /sites', async () => {
        await render('/portal', 'Owner');
        expect(path()).toBe('/sites');
    });

    it('an anonymous user is sent to /login with returnUrl', async () => {
        await render('/people', null);
        expect(path()).toBe('/login?returnUrl=%2Fpeople');
    });

    it('Owner has "Фирми" above "Изход" in the user menu, and it opens /companies', async () => {
        const { harness, element } = await render('/sites', 'Owner');

        const items = await openUserMenu(element);
        expect(items.map(itemText)).toEqual(['Фирми', 'Изход']);

        items[0].querySelector<HTMLAnchorElement>('a')!.click();
        await harness.fixture.whenStable();

        expect(path()).toBe('/companies');
    });

    it('Crew has no "Фирми" in the user menu', async () => {
        const { element } = await render('/sites', 'Crew');

        const items = await openUserMenu(element);

        expect(items.map(itemText)).toEqual(['Изход']);
    });

    it('Crew opening /companies is sent to /sites', async () => {
        await render('/companies', 'Crew');
        expect(path()).toBe('/sites');
    });
});
