import { Location } from '@angular/common';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
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
import { PortalSite } from '../portal.model';
import { PortalService } from '../portal.service';
import { FakePortalService, testPortalSite } from '../testing/fake-portal.service';

describe('PortalSitePageComponent', () => {
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

    async function render(url = '/portal/s1') {
        await TestBed.inject(AuthStore).loadMe();
        const harness = await RouterTestingHarness.create();
        await harness.navigateByUrl(url);
        await harness.fixture.whenStable();
        const element = harness.fixture.nativeElement as HTMLElement;
        return { harness, element, page: element.querySelector<HTMLElement>('atlas-portal-site-page')! };
    }

    it('loads the site and shows „← Всички обекти“, name, status, end customer · address · карта ↗', async () => {
        const { page } = await render();

        expect(fake.calls).toEqual(['site s1']);
        expect(document.title).toBe('Обект · Атлас');
        expect(text(page.querySelector('a.back'))).toBe('Всички обекти');
        expect(page.querySelector('a.back')?.getAttribute('href')).toBe('/portal');
        expect(text(page.querySelector('atlas-site-header h1'))).toBe('Бункера');
        expect(text(page.querySelector('atlas-site-status-tag'))).toBe('Текущ');
        expect(text(page.querySelector('atlas-site-header .meta'))).toBe('Професорът · ул. Шипка 5 · карта ↗');
        const map = page.querySelector<HTMLAnchorElement>('atlas-site-header .meta a')!;
        expect(map.getAttribute('href')).toBe('https://maps.example/x');
        expect(map.getAttribute('target')).toBe('_blank');
    });

    it('shows „Изработено | Платено | Остатък“ and „N за уточняване“', async () => {
        const { page } = await render();

        const ledger = page.querySelector('atlas-ledger')!;
        expect(Array.from(ledger.querySelectorAll('.ledger__label')).map(text)).toEqual([
            'Изработено',
            'Платено',
            'Остатък',
        ]);
        expect(Array.from(ledger.querySelectorAll('.ledger__value')).map(text)).toEqual([
            '1 157,89 €',
            '500,00 €',
            '657,89 €',
        ]);
        expect(text(page.querySelector('.open .pill.q'))).toBe('2 за уточняване');
    });

    it('an overpaid site reads „Надплатено“; no open pill when nothing is open', async () => {
        fake.siteResult = of(testPortalSite({ summary: { earned: 400, paid: 500, remaining: -100, openCount: 0 } }));

        const { page } = await render();

        const last = page.querySelectorAll('atlas-ledger .ledger__col')[2];
        expect(text(last.querySelector('.ledger__label'))).toBe('Надплатено');
        expect(text(last.querySelector('.ledger__value'))).toBe('100,00 €');
        expect(page.querySelector('.open')).toBeNull();
    });

    it('shows the work days with items, „цена?“, „уточни“ and „Общо“', async () => {
        const { page } = await render();

        const section = page.querySelector('section.work-days')!;
        expect(text(section.querySelector('h2'))).toBe('Работни дни 1');
        const day = section.querySelector('.day')!;
        expect(text(day.querySelector('.date__day'))).toBe('16.11');
        expect(text(day.querySelector('.title'))).toBe('Шпакловка финал');
        const items = Array.from(day.querySelectorAll('.item'));
        expect(items.map((item) => text(item.querySelector('.desc')))).toEqual(['Шпакловка', 'Первази', 'Боя']);
        expect(items.map((item) => text(item.querySelector('.calc')))).toEqual([
            '12,5 м² × 6,50',
            '3 м.л.',
            '2 бр. × 538,32',
        ]);
        expect(items.map((item) => text(item.querySelector('.sum')))).toEqual(['81,25 €', '—', '1 076,64 €']);
        expect(items.map((item) => text(item.querySelector('.pill')) ?? null)).toEqual([null, 'цена?', 'уточни']);
        expect(Array.from(day.querySelectorAll('.total td')).map(text)).toEqual(['Общо', '1 157,89 €']);
    });

    it('shows the payments with date and amount', async () => {
        const { page } = await render();

        const section = page.querySelector('section.payments')!;
        expect(text(section.querySelector('h2'))).toBe('Плащания 2');
        const rows = Array.from(section.querySelectorAll('.payment'));
        expect(rows.map((row) => Array.from(row.children).map(text))).toEqual([
            ['03.11', 'Плащане', '300,00 €'],
            ['10.11', 'Плащане', '200,00 €'],
        ]);
    });

    it('empty texts without days and payments', async () => {
        fake.siteResult = of(testPortalSite({ workDays: [], payments: [] }));

        const { page } = await render();

        expect(text(page.querySelector('section.work-days .empty'))).toBe('Още няма работни дни.');
        expect(text(page.querySelector('section.payments .empty'))).toBe('Няма плащания.');
    });

    it('is read only: no buttons for editing and no add actions', async () => {
        const { page } = await render();

        expect(page.querySelectorAll('button').length).toBe(0);
        for (const label of ['Редакция', 'Данни за обекта', 'Ден работа', 'Плащане ', 'Изтрий', 'Цена', 'Контакт']) {
            expect(page.textContent).not.toContain(label);
        }
    });

    it('shows no people, notes, contacts, tasks, prices or worker costs, even if the server sent them', async () => {
        // The real service over a test backend: an API mistake with internal fields must not reach the screen.
        TestBed.configureTestingModule({
            providers: [
                provideHttpClient(),
                provideHttpClientTesting(),
                { provide: PortalService, useClass: PortalService },
            ],
        });
        const site = testPortalSite();
        await TestBed.inject(AuthStore).loadMe();
        const harness = await RouterTestingHarness.create();
        const navigation = harness.navigateByUrl('/portal/s1');
        await vi.waitFor(() =>
            TestBed.inject(HttpTestingController)
                .expectOne('/portal/sites/s1')
                .flush({
                    ...site,
                    note: 'Вътрешна бележка за обекта',
                    companyName: 'Женистрой',
                    summary: { ...site.summary, workerCost: 80, net: 1077.89 },
                    workDays: site.workDays.map((day) => ({
                        ...day,
                        note: 'Вътрешна бележка за деня',
                        workers: [{ id: 'p1', name: 'Иван Майстора', pay: 80, isPartner: false }],
                    })),
                }),
        );
        await navigation;
        await harness.fixture.whenStable();
        const page = (harness.fixture.nativeElement as HTMLElement).querySelector<HTMLElement>(
            'atlas-portal-site-page',
        )!;

        expect(text(page.querySelector('atlas-site-header h1'))).toBe('Бункера');
        expect(page.querySelector('.chips')).toBeNull();
        expect(page.querySelector('.note')).toBeNull();
        expect(page.querySelector('atlas-cost-ledger')).toBeNull();
        expect(page.querySelector('atlas-site-contact-list')).toBeNull();
        expect(page.querySelector('atlas-task-list')).toBeNull();
        expect(page.querySelector('atlas-site-price-list')).toBeNull();
        for (const hidden of [
            'Иван Майстора',
            'Вътрешна бележка',
            'Женистрой',
            'Разход за хора',
            'Остава за нас',
            'Контакти',
            'Задачи',
            'Цени за обекта',
        ]) {
            expect(page.textContent).not.toContain(hidden);
        }
    });

    it('a 404 (no such site or another company) shows „Няма такъв обект.“', async () => {
        fake.siteResult = httpError(404, { title: 'Няма такъв обект.' });

        const { page } = await render('/portal/x');

        expect(text(page.querySelector('.not-found p'))).toBe('Няма такъв обект.');
        expect(page.querySelector('.not-found a')?.getAttribute('href')).toBe('/portal');
        expect(page.querySelector('atlas-ledger')).toBeNull();
    });

    it('„← Всички обекти“ goes back to /portal', async () => {
        const { harness, page } = await render();

        page.querySelector<HTMLAnchorElement>('a.back')!.click();
        await harness.fixture.whenStable();

        expect(path()).toBe('/portal');
    });

    it('Manager is sent from a portal site to /sites', async () => {
        role = 'Manager';

        await render();

        expect(path()).toBe('/sites');
        expect(fake.calls).toEqual([]);
    });
    it("client A signs out, client B opens a site in the same tab: nothing of A is shown before B's answer", async () => {
        const { harness, page } = await render();
        expect(text(page.querySelector('atlas-site-header h1'))).toBe('Бункера');

        const auth = TestBed.inject(AuthStore);
        await auth.logout();
        await harness.navigateByUrl('/login');
        await auth.login({ userName: 'stroy', password: 'x' });

        const answerOfB = new Subject<PortalSite>();
        fake.siteResult = answerOfB;
        await harness.navigateByUrl('/portal/b1');
        await harness.fixture.whenStable();

        const element = harness.fixture.nativeElement as HTMLElement;
        const pageOfB = element.querySelector<HTMLElement>('atlas-portal-site-page')!;
        expect(pageOfB.textContent).not.toContain('Бункера');
        expect(pageOfB.querySelector('atlas-site-header')).toBeNull();
        expect(pageOfB.querySelector('atlas-ledger')).toBeNull();
        expect(text(pageOfB.querySelector('.note'))).toBe('Зареждане…');

        answerOfB.next(testPortalSite({ id: 'b1', name: 'Гаража' }));
        answerOfB.complete();
        await harness.fixture.whenStable();

        await vi.waitFor(() => expect(text(pageOfB.querySelector('atlas-site-header h1'))).toBe('Гаража'));
    });
});
