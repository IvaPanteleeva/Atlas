import { Location } from '@angular/common';
import { ANIMATION_MODULE_TYPE } from '@angular/core';
import { DeferBlockBehavior, DeferBlockState, TestBed } from '@angular/core/testing';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { Observable, of } from 'rxjs';
import { routes } from '../../../app.routes';
import { AuthStore } from '../../auth/+state/auth.store';
import { Me, Role } from '../../auth/auth.model';
import { AuthService } from '../../auth/auth.service';
import { CompaniesService } from '../../companies/companies.service';
import { FakeServicesService } from '../../services/testing/fake-services.service';
import { ServicesService } from '../../services/services.service';
import { PaymentsService } from '../../payments/payments.service';
import { FakePaymentsService, testPayment } from '../../payments/testing/fake-payments.service';
import { APP_ICONS } from '../../shared/icons';
import { SitePricesService } from '../../site-prices/site-prices.service';
import { FakeSitePricesService, testSitePrice } from '../../site-prices/testing/fake-site-prices.service';
import { SummariesService } from '../../summaries/summaries.service';
import { FakeSummariesService, testSiteSummary } from '../../summaries/testing/fake-summaries.service';
import { SitesService } from '../sites.service';
import { FakeSitesService, httpError, testSite } from '../testing/fake-sites.service';
import { FakeWorkDaysService, testWorkDay } from '../../work-days/testing/fake-work-days.service';
import { WorkDaysService } from '../../work-days/work-days.service';

describe('SiteDetailPageComponent', () => {
    let fake: FakeSitesService;
    let prices: FakeSitePricesService;
    let services: FakeServicesService;
    let workDays: FakeWorkDaysService;
    let payments: FakePaymentsService;
    let summaries: FakeSummariesService;
    let role: Role;

    beforeAll(() => {
        Element.prototype.scrollTo ??= () => undefined;
    });

    beforeEach(() => {
        fake = new FakeSitesService();
        prices = new FakeSitePricesService();
        services = new FakeServicesService();
        workDays = new FakeWorkDaysService();
        payments = new FakePaymentsService();
        summaries = new FakeSummariesService();
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
                { provide: SitePricesService, useValue: prices },
                { provide: ServicesService, useValue: services },
                { provide: WorkDaysService, useValue: workDays },
                { provide: PaymentsService, useValue: payments },
                { provide: SummariesService, useValue: summaries },
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

    describe('"Цени за обекта"', () => {
        const section = (page: HTMLElement) => page.querySelector<HTMLElement>('section.prices')!;
        const rowNames = (page: HTMLElement) =>
            Array.from(section(page).querySelectorAll('.price .name')).map((n) => n.textContent?.trim());
        const priceForm = () => document.querySelector<HTMLElement>('.atlas-sheet atlas-site-price-form');

        async function waitForPriceForm() {
            return vi.waitFor(() => {
                const form = priceForm();
                expect(form).toBeTruthy();
                return form!;
            });
        }

        beforeEach(() => {
            prices.listResult = of([
                testSitePrice({ id: 'p1', serviceId: 'sv1', serviceName: 'Боядисване', rate: 6.5 }),
            ]);
        });

        it('loads the prices of the site and the services, and shows the section', async () => {
            const { page } = await render();

            expect(prices.calls).toEqual(['list s1']);
            expect(services.calls).toEqual(['list']);
            expect(section(page).querySelector('h2')?.textContent?.trim()).toBe('Цени за обекта');
            expect(rowNames(page)).toEqual(['Боядисване']);
            expect(section(page).querySelector('.price .rate')?.textContent?.trim()).toBe('6,50\u00A0€');
        });

        it('shows the empty state', async () => {
            prices.listResult = of([]);

            const { page } = await render();

            expect(section(page).textContent).toContain(
                'Няма договорени цени. При работа се ползва цената по подразбиране на услугата.',
            );
        });

        it('Crew sees the prices read-only', async () => {
            role = 'Crew';

            const { page } = await render();

            expect(rowNames(page)).toEqual(['Боядисване']);
            expect(button(section(page), 'Цена')).toBeUndefined();
            expect(section(page).querySelectorAll('button.price').length).toBe(0);
        });

        it('Owner adds a price with "+ Цена" (only free services, default price prefilled)', async () => {
            prices.createResult = of(testSitePrice({ id: 'p2', serviceId: 'sv2', serviceName: 'Шпакловка', rate: 7 }));
            const { harness, page } = await render();

            button(section(page), 'Цена')!.click();
            await harness.fixture.whenStable();
            const form = await waitForPriceForm();
            form.querySelector<HTMLElement>('nz-select#price-service .ant-select-selector')!.click();
            await harness.fixture.whenStable();
            const options = await vi.waitFor(() => {
                const found = Array.from(document.querySelectorAll<HTMLElement>('nz-option-item'));
                expect(found.length).toBeGreaterThan(0);
                return found;
            });
            // Боядисване already has a price.
            expect(options.map((o) => o.textContent?.trim())).toEqual(['Шпакловка · м²']);
            options[0].click();
            await harness.fixture.whenStable();
            const rate = form.querySelector<HTMLInputElement>('#price-rate')!;
            rate.value = '7';
            rate.dispatchEvent(new Event('input'));
            await harness.fixture.whenStable();
            button(form, 'Запази')!.click();
            await harness.fixture.whenStable();

            expect(prices.calls).toContain('create s1 sv2 7');
            await vi.waitFor(() => expect(priceForm()).toBeNull());
            expect(rowNames(page)).toEqual(['Боядисване', 'Шпакловка']);
        });

        it('Owner changes a price by tapping its row', async () => {
            const { harness, page } = await render();

            section(page).querySelector<HTMLButtonElement>('button.price')!.click();
            await harness.fixture.whenStable();
            const form = await waitForPriceForm();
            const rate = form.querySelector<HTMLInputElement>('#price-rate')!;
            rate.value = '9';
            rate.dispatchEvent(new Event('input'));
            await harness.fixture.whenStable();
            button(form, 'Запази')!.click();
            await harness.fixture.whenStable();

            expect(prices.calls).toContain('update s1 p1 9');
            await vi.waitFor(() => expect(priceForm()).toBeNull());
            expect(section(page).querySelector('.price .rate')?.textContent?.trim()).toBe('9,00\u00A0€');
        });

        it('Owner deletes a price after confirming', async () => {
            const { harness, page } = await render();

            section(page).querySelector<HTMLButtonElement>('button.price')!.click();
            await harness.fixture.whenStable();
            const form = await waitForPriceForm();
            button(form, 'Изтрий')!.click();
            await harness.fixture.whenStable();
            button(form, 'Потвърди изтриване')!.click();
            await harness.fixture.whenStable();

            expect(prices.calls).toContain('remove s1 p1');
            await vi.waitFor(() => expect(priceForm()).toBeNull());
            expect(rowNames(page)).toEqual([]);
        });
    });
    describe('"Работни дни"', () => {
        const section = (page: HTMLElement) => page.querySelector<HTMLElement>('section.work-days')!;
        const dayForm = () => document.querySelector<HTMLElement>('.atlas-sheet atlas-work-day-form');

        async function waitForDayForm() {
            return vi.waitFor(() => {
                const form = dayForm();
                expect(form).toBeTruthy();
                return form!;
            });
        }

        async function type(harness: RouterTestingHarness, input: HTMLInputElement, value: string) {
            input.value = value;
            input.dispatchEvent(new Event('input'));
            await harness.fixture.whenStable();
        }

        it('loads the days of the site and shows them above "Цени за обекта", with the count', async () => {
            workDays.listResult = of([
                testWorkDay({ id: 'd2', date: '2026-11-14', title: 'Боядисване' }),
                testWorkDay({ id: 'd1', date: '2026-11-10' }),
            ]);

            const { page } = await render();

            expect(workDays.calls).toEqual(['list s1']);
            const sections = Array.from(page.querySelectorAll('section')).map((s) => s.className);
            expect(sections.indexOf('work-days')).toBeLessThan(sections.indexOf('prices'));
            expect(section(page).querySelector('h2')?.textContent?.replace(/\s+/g, ' ').trim()).toBe('Работни дни 2');
            expect(section(page).querySelectorAll('.day').length).toBe(2);
        });

        it('shows the empty state', async () => {
            const { page } = await render();

            expect(section(page).textContent).toContain(
                'Няма записани дни. Натисни „+ Ден работа“ след всеки ден на обекта.',
            );
        });

        it('the sheet shows a loading state (placeholder and loading) until the form chunk is there', async () => {
            TestBed.configureTestingModule({ deferBlockBehavior: DeferBlockBehavior.Manual });
            const { harness, page } = await render();
            const loading = () => document.querySelector<HTMLElement>('.atlas-sheet .day-form-loading');

            button(section(page), 'Ден работа')!.click();
            await harness.fixture.whenStable();
            const block = await vi.waitFor(async () => {
                const blocks = await harness.fixture.getDeferBlocks();
                expect(blocks.length).toBe(1);
                return blocks[0];
            });

            await block.render(DeferBlockState.Placeholder);
            expect(loading()?.getAttribute('role')).toBe('status');
            expect(loading()?.textContent).toContain('Зареждане…');
            expect(dayForm()).toBeNull();

            await block.render(DeferBlockState.Loading);
            expect(loading()?.textContent).toContain('Зареждане…');

            await block.render(DeferBlockState.Complete);
            await waitForDayForm();
            expect(loading()).toBeNull();
        });

        it('Crew adds and edits days but sees no delete', async () => {
            role = 'Crew';
            workDays.listResult = of([testWorkDay()]);
            const { harness, page } = await render();

            expect(button(section(page), 'Ден работа')).toBeDefined();
            button(section(page), 'Редакция')!.click();
            await harness.fixture.whenStable();
            const form = await waitForDayForm();

            expect(button(form, 'Запази')).toBeDefined();
            expect(button(form, 'Изтрий')).toBeUndefined();
            expect(form.querySelector<HTMLInputElement>('#wd-item-0-rate')!.disabled).toBe(true);
        });

        it('a new day is saved, listed, and the site is reloaded (a lead becomes current)', async () => {
            fake.getResult = of(testSite({ status: 'Lead' }));
            const { harness, page } = await render();
            fake.getResult = of(testSite({ status: 'Active' }));

            button(section(page), 'Ден работа')!.click();
            await harness.fixture.whenStable();
            const form = await waitForDayForm();
            await type(harness, form.querySelector<HTMLInputElement>('#wd-item-0-description')!, 'Къртене');
            await type(harness, form.querySelector<HTMLInputElement>('#wd-item-0-quantity')!, '1');
            button(form, 'Запази')!.click();
            await harness.fixture.whenStable();

            expect(workDays.calls.filter((call) => call.startsWith('create s1'))).toHaveLength(1);
            expect(workDays.lastRequest?.items[0].description).toBe('Къртене');
            await vi.waitFor(() => expect(dayForm()).toBeNull());
            await vi.waitFor(() => expect(fake.calls).toEqual(['get s1', 'get s1']));
            await harness.fixture.whenStable();
            expect(page.querySelector('atlas-site-header')?.textContent).toContain('Текущ');
            expect(workDays.calls.at(-1)).toBe('list s1');
        });

        it('the money is reloaded quietly after a day is saved or deleted', async () => {
            workDays.listResult = of([testWorkDay({ id: 'd1' })]);
            const { harness, page } = await render();
            expect(summaries.calls).toEqual(['site s1']);
            summaries.siteResult = of(testSiteSummary({ earned: 1239.14, remaining: 739.14 }));

            button(section(page), 'Редакция')!.click();
            await harness.fixture.whenStable();
            const form = await waitForDayForm();
            button(form, 'Запази')!.click();
            await harness.fixture.whenStable();

            await vi.waitFor(() => expect(summaries.calls).toEqual(['site s1', 'site s1']));
            await harness.fixture.whenStable();
            expect(page.querySelector('atlas-ledger .ledger__value')?.textContent?.trim()).toBe('1\u00A0239,14\u00A0€');
            await vi.waitFor(() => expect(dayForm()).toBeNull());

            button(section(page), 'Редакция')!.click();
            await harness.fixture.whenStable();
            const again = await waitForDayForm();
            button(again, 'Изтрий')!.click();
            await harness.fixture.whenStable();
            button(again, 'Потвърди изтриване')!.click();
            await harness.fixture.whenStable();

            await vi.waitFor(() => expect(summaries.calls).toEqual(['site s1', 'site s1', 'site s1']));
        });

        it('Crew saves a day without any money requests', async () => {
            role = 'Crew';
            workDays.listResult = of([testWorkDay({ id: 'd1' })]);
            const { harness, page } = await render();

            button(section(page), 'Редакция')!.click();
            await harness.fixture.whenStable();
            const form = await waitForDayForm();
            button(form, 'Запази')!.click();
            await harness.fixture.whenStable();
            await vi.waitFor(() => expect(dayForm()).toBeNull());

            expect(workDays.calls.some((call) => call.startsWith('update s1 d1'))).toBe(true);
            expect(summaries.calls).toEqual([]);
            expect(payments.calls).toEqual([]);
        });

        it('Owner deletes a day after confirming', async () => {
            workDays.listResult = of([testWorkDay({ id: 'd1' })]);
            const { harness, page } = await render();
            workDays.listResult = of([]);

            button(section(page), 'Редакция')!.click();
            await harness.fixture.whenStable();
            const form = await waitForDayForm();
            button(form, 'Изтрий')!.click();
            await harness.fixture.whenStable();
            button(form, 'Потвърди изтриване')!.click();
            await harness.fixture.whenStable();

            expect(workDays.calls).toContain('remove s1 d1');
            await vi.waitFor(() => expect(dayForm()).toBeNull());
            await harness.fixture.whenStable();
            expect(section(page).querySelectorAll('.day').length).toBe(0);
        });
    });

    describe('money (Owner)', () => {
        const text = (node: Element | null | undefined) => node?.textContent?.replace(/\s+/g, ' ').trim();
        const ledger = (page: HTMLElement) => page.querySelector<HTMLElement>('atlas-ledger');
        const section = (page: HTMLElement) => page.querySelector<HTMLElement>('section.payments');
        const paymentForm = () => document.querySelector<HTMLElement>('.atlas-sheet atlas-payment-form');

        async function waitForPaymentForm() {
            return vi.waitFor(() => {
                const form = paymentForm();
                expect(form).toBeTruthy();
                return form!;
            });
        }

        async function type(harness: RouterTestingHarness, input: HTMLInputElement, value: string) {
            input.value = value;
            input.dispatchEvent(new Event('input'));
            await harness.fixture.whenStable();
        }

        it('Owner sees the ledger under the header, from the summary of the site', async () => {
            const { page } = await render();

            expect(summaries.calls).toEqual(['site s1']);
            expect(Array.from(ledger(page)!.querySelectorAll('.ledger__label')).map(text)).toEqual([
                'Изработено',
                'Платено',
                'Остатък',
            ]);
            expect(Array.from(ledger(page)!.querySelectorAll('.ledger__value')).map(text)).toEqual([
                '1 157,89 €',
                '500,00 €',
                '657,89 €',
            ]);
            const header = page.querySelector('atlas-site-header')!;
            expect(header.compareDocumentPosition(ledger(page)!)).toBe(Node.DOCUMENT_POSITION_FOLLOWING);
            expect(ledger(page)!.compareDocumentPosition(page.querySelector('section.work-days')!)).toBe(
                Node.DOCUMENT_POSITION_FOLLOWING,
            );
            expect(page.querySelector('.pill.q')).toBeNull();
        });

        it('shows „N позиции без цена / за уточняване“ when something is open', async () => {
            summaries.siteResult = of(testSiteSummary({ openCount: 2 }));

            const { page } = await render();

            expect(text(page.querySelector('.pill.q'))).toBe('2 позиции без цена / за уточняване');
        });

        it('one open item reads „1 позиция …“', async () => {
            summaries.siteResult = of(testSiteSummary({ openCount: 1 }));

            const { page } = await render();

            expect(text(page.querySelector('.pill.q'))).toBe('1 позиция без цена / за уточняване');
        });

        it('loads the payments and shows „Плащания“ below „Работни дни“ (above the prices)', async () => {
            payments.listResult = of([
                testPayment({ id: 'p2', date: '2026-11-14', note: null, amount: 200 }),
                testPayment({ id: 'p1', date: '2026-11-10', note: 'аванс', amount: 300 }),
            ]);

            const { page } = await render();

            expect(payments.calls).toEqual(['list s1']);
            const sections = Array.from(page.querySelectorAll('section')).map((s) => s.className);
            expect(sections).toEqual(['work-days', 'payments', 'prices']);
            expect(text(section(page)!.querySelector('h2'))).toBe('Плащания 2');
            expect(button(section(page)!, 'Плащане')).toBeDefined();
            expect(Array.from(section(page)!.querySelectorAll('button.payment .label')).map(text)).toEqual([
                'Плащане',
                'аванс',
            ]);
        });

        it('shows „Няма плащания.“ when there are none', async () => {
            const { page } = await render();

            expect(text(section(page))).toContain('Няма плащания.');
        });

        it('Crew sees neither the ledger nor the payments, and no money is requested', async () => {
            role = 'Crew';
            summaries.siteResult = of(testSiteSummary({ openCount: 2 }));

            const { page } = await render();

            expect(ledger(page)).toBeNull();
            expect(section(page)).toBeNull();
            expect(page.querySelector('.pill.q')).toBeNull();
            expect(summaries.calls).toEqual([]);
            expect(payments.calls).toEqual([]);
            expect(page.querySelector('section.work-days')).not.toBeNull();
        });

        it('„+ Плащане“ saves a payment, lists it and reloads the money quietly', async () => {
            const { harness, page } = await render();
            summaries.siteResult = of(testSiteSummary({ paid: 700, remaining: 457.89 }));
            payments.listResult = of([testPayment({ id: 'new', amount: 200 })]);

            button(section(page)!, 'Плащане')!.click();
            await harness.fixture.whenStable();
            const form = await waitForPaymentForm();
            expect(document.querySelector('.atlas-sheet .ant-modal-title')?.textContent?.trim()).toBe('Ново плащане');
            await type(harness, form.querySelector<HTMLInputElement>('#pay-amount')!, '200');
            button(form, 'Запази')!.click();
            await harness.fixture.whenStable();

            expect(payments.calls.some((call) => /^create s1 \d{4}-\d{2}-\d{2} 200$/.test(call))).toBe(true);
            await vi.waitFor(() => expect(paymentForm()).toBeNull());
            await vi.waitFor(() => expect(summaries.calls).toEqual(['site s1', 'site s1']));
            await harness.fixture.whenStable();
            expect(payments.calls.at(-1)).toBe('list s1');
            expect(text(ledger(page)!.querySelectorAll('.ledger__value')[1])).toBe('700,00 €');
            expect(section(page)!.querySelectorAll('button.payment').length).toBe(1);
        });

        it('tapping a payment edits it; delete needs a confirmation and reloads the money', async () => {
            payments.listResult = of([testPayment({ id: 'p1', amount: 500 })]);
            const { harness, page } = await render();

            section(page)!.querySelector<HTMLButtonElement>('button.payment')!.click();
            await harness.fixture.whenStable();
            const form = await waitForPaymentForm();
            expect(form.querySelector<HTMLInputElement>('#pay-amount')!.value).toBe('500,00');
            await type(harness, form.querySelector<HTMLInputElement>('#pay-amount')!, '650');
            button(form, 'Запази')!.click();
            await harness.fixture.whenStable();
            expect(payments.lastRequest).toEqual({ date: '2026-11-16', amount: 650, note: 'аванс' });
            await vi.waitFor(() => expect(paymentForm()).toBeNull());
            await vi.waitFor(() => expect(summaries.calls).toEqual(['site s1', 'site s1']));

            payments.listResult = of([]);
            section(page)!.querySelector<HTMLButtonElement>('button.payment')!.click();
            await harness.fixture.whenStable();
            const again = await waitForPaymentForm();
            button(again, 'Изтрий')!.click();
            await harness.fixture.whenStable();
            expect(payments.calls).not.toContain('remove s1 p1');
            button(again, 'Потвърди изтриване')!.click();
            await harness.fixture.whenStable();

            expect(payments.calls).toContain('remove s1 p1');
            await vi.waitFor(() => expect(paymentForm()).toBeNull());
            await vi.waitFor(() => expect(summaries.calls).toEqual(['site s1', 'site s1', 'site s1']));
            await harness.fixture.whenStable();
            expect(text(section(page))).toContain('Няма плащания.');
        });

        it('a 400 keeps the payment form open with the message under the field', async () => {
            payments.createResult = httpError(400, { errors: { Date: ['Датата не може да е в бъдещето'] } });
            const { harness, page } = await render();

            button(section(page)!, 'Плащане')!.click();
            await harness.fixture.whenStable();
            const form = await waitForPaymentForm();
            await type(harness, form.querySelector<HTMLInputElement>('#pay-amount')!, '200');
            button(form, 'Запази')!.click();
            await harness.fixture.whenStable();

            expect(paymentForm()).not.toBeNull();
            expect(text(form.querySelector('#pay-date')!.closest('nz-form-item'))).toContain(
                'Датата не може да е в бъдещето',
            );
            expect(summaries.calls).toEqual(['site s1']);
        });
    });
});
