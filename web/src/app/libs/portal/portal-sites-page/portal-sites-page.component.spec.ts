import { Location } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { Observable, of, Subject } from 'rxjs';
import { routes } from '../../../app.routes';
import { AuthStore } from '../../auth/+state/auth.store';
import { Me, Role } from '../../auth/auth.model';
import { AuthService } from '../../auth/auth.service';
import { APP_ICONS } from '../../shared/icons';
import { httpError } from '../../sites/testing/fake-sites.service';
import { PortalSites } from '../portal.model';
import { PortalService } from '../portal.service';
import { FakePortalService, testPortalSiteListItem, testPortalTotals } from '../testing/fake-portal.service';

describe('PortalSitesPageComponent', () => {
    let fake: FakePortalService;
    let role: Role;

    const text = (node: Element | null | undefined) => node?.textContent?.replace(/\s+/g, ' ').trim();
    const path = () => TestBed.inject(Location).path();

    beforeEach(() => {
        fake = new FakePortalService();
        role = 'ClientViewer';
        TestBed.configureTestingModule({
            providers: [
                provideRouter(routes, withComponentInputBinding()),
                provideNzIcons(APP_ICONS),
                {
                    provide: AuthService,
                    useValue: {
                        me: (): Observable<Me> =>
                            of({ id: '1', userName: 'zheni', role, companyId: 'c1', personId: null }),
                        login: (request: { userName: string }): Observable<Me> =>
                            of({ id: '2', userName: request.userName, role, companyId: 'c2', personId: null }),
                        logout: (): Observable<void> => of(undefined),
                    },
                },
                { provide: PortalService, useValue: fake },
            ],
        });
    });

    async function render(url = '/portal') {
        await TestBed.inject(AuthStore).loadMe();
        const harness = await RouterTestingHarness.create();
        await harness.navigateByUrl(url);
        await harness.fixture.whenStable();
        const element = harness.fixture.nativeElement as HTMLElement;
        return { harness, element, page: element.querySelector<HTMLElement>('atlas-portal-sites-page')! };
    }

    it('shows „Дължите X €“ above the list of the client sites, inside the shell without tabs', async () => {
        const { element, page } = await render();

        expect(fake.calls).toEqual(['sites']);
        expect(document.title).toBe('Обекти · Атлас');
        expect(text(page.querySelector('h1'))).toBe('Обекти');
        const owed = page.querySelector('.owed')!;
        expect(text(owed.querySelector('.owed__label'))).toBe('Дължите');
        expect(text(owed.querySelector('.owed__value'))).toBe('657,89 €');
        expect(owed.classList).toContain('owed--warn');
        expect(owed.compareDocumentPosition(page.querySelector('atlas-portal-site-list')!)).toBe(
            Node.DOCUMENT_POSITION_FOLLOWING,
        );
        expect(Array.from(page.querySelectorAll('.site .name')).map(text)).toEqual(['Бункера', 'Професора']);
        expect(element.querySelectorAll('.tabs a').length).toBe(0);
    });

    it('„Надплатено X € по N обекта“ when some sites are overpaid', async () => {
        fake.sitesResult = of({
            sites: [testPortalSiteListItem({ remaining: -40.5 })],
            totals: testPortalTotals({ owed: 0, overpaid: 40.5, overpaidSiteCount: 2 }),
        });

        const { page } = await render();

        expect(text(page.querySelector('.owed__value'))).toBe('0,00 €');
        expect(page.querySelector('.owed')!.classList).toContain('owed--good');
        expect(text(page.querySelector('.pill.good'))).toBe('Надплатено 40,50 € по 2 обекта');
    });

    it('no overpaid pill when nothing is overpaid', async () => {
        const { page } = await render();

        expect(page.querySelector('.pill.good')).toBeNull();
    });

    it('shows a load error', async () => {
        fake.sitesResult = httpError(500);

        const { page } = await render();

        expect(text(page.querySelector('nz-alert'))).toContain('Грешка при зареждане на обектите');
        expect(page.querySelector('.owed')).toBeNull();
    });

    it('a site row opens the site in the portal', async () => {
        const { harness, page } = await render();

        page.querySelectorAll<HTMLAnchorElement>('a.site')[1].click();
        await harness.fixture.whenStable();

        expect(path()).toBe('/portal/s2');
    });

    it('is read only: no buttons on the page (no „Нов обект“)', async () => {
        const { page } = await render();

        expect(page.querySelectorAll('button').length).toBe(0);
        expect(page.textContent).not.toContain('Нов обект');
    });

    it('Owner is sent from /portal to /sites', async () => {
        role = 'Owner';

        await render();

        expect(path()).toBe('/sites');
        expect(fake.calls).toEqual([]);
    });
    it("client A signs out, client B signs in in the same tab: nothing of A is shown before B's answer", async () => {
        const { harness, page } = await render();
        expect(Array.from(page.querySelectorAll('.site .name')).map(text)).toEqual(['Бункера', 'Професора']);
        expect(text(page.querySelector('.owed__value'))).toBe('657,89 €');

        const auth = TestBed.inject(AuthStore);
        await auth.logout();
        await harness.navigateByUrl('/login');
        await auth.login({ userName: 'stroy', password: 'x' });

        const answerOfB = new Subject<PortalSites>();
        fake.sitesResult = answerOfB;
        await harness.navigateByUrl('/portal');
        await harness.fixture.whenStable();

        const element = harness.fixture.nativeElement as HTMLElement;
        const pageOfB = element.querySelector<HTMLElement>('atlas-portal-sites-page')!;
        expect(pageOfB.textContent).not.toContain('Бункера');
        expect(pageOfB.textContent).not.toContain('Професора');
        expect(pageOfB.textContent).not.toContain('657,89');
        expect(pageOfB.querySelector('.owed')).toBeNull();
        expect(pageOfB.querySelector('atlas-portal-site-list')).toBeNull();
        expect(text(pageOfB.querySelector('.note'))).toBe('Зареждане…');

        answerOfB.next({
            sites: [testPortalSiteListItem({ id: 'b1', name: 'Гаража', remaining: 10 })],
            totals: testPortalTotals({ owed: 10 }),
        });
        answerOfB.complete();
        await harness.fixture.whenStable();

        await vi.waitFor(() =>
            expect(Array.from(pageOfB.querySelectorAll('.site .name')).map(text)).toEqual(['Гаража']),
        );
        expect(text(pageOfB.querySelector('.owed__value'))).toBe('10,00 €');
    });
});
