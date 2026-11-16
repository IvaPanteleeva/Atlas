import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SummariesBySite } from '../../summaries/summary.model';
import { testSummaryRow } from '../../summaries/testing/fake-summaries.service';
import { SiteListItem } from '../site.model';
import { SiteListComponent } from './site-list.component';

const sites: SiteListItem[] = [
    {
        id: 'a',
        name: 'Бункера',
        status: 'Active',
        companyName: 'Женистрой',
        endCustomer: 'Иван',
        address: 'ул. Шипка 5',
        lastWorkDate: '2026-11-14',
    },
    {
        id: 'b',
        name: 'Професора',
        status: 'Lead',
        companyName: null,
        endCustomer: 'Професорът',
        address: null,
        lastWorkDate: null,
    },
    {
        id: 'c',
        name: 'Гаража',
        status: 'Done',
        companyName: null,
        endCustomer: null,
        address: null,
        lastWorkDate: null,
    },
];

describe('SiteListComponent', () => {
    let fixture: ComponentFixture<SiteListComponent>;
    let element: HTMLElement;
    let opened: string[];

    beforeEach(() => {
        fixture = TestBed.createComponent(SiteListComponent);
        opened = [];
        fixture.componentInstance.open.subscribe((id) => opened.push(id));
        element = fixture.nativeElement as HTMLElement;
    });

    async function render(list: SiteListItem[]) {
        fixture.componentRef.setInput('sites', list);
        await fixture.whenStable();
    }

    const rows = () => Array.from(element.querySelectorAll<HTMLButtonElement>('button.site'));

    it('shows the empty state for no sites', async () => {
        await render([]);

        expect(rows().length).toBe(0);
        expect(element.textContent).toContain('Още няма обекти. Добави първия с „+ Нов обект“.');
    });

    it('shows one row per site with name, status and meta (company, else end customer, then address)', async () => {
        await render(sites);

        expect(rows().map((row) => row.querySelector('.name')?.textContent?.trim())).toEqual([
            'Бункера',
            'Професора',
            'Гаража',
        ]);
        expect(rows().map((row) => row.querySelector('.meta')?.textContent?.trim() ?? null)).toEqual([
            'Женистрой · ул. Шипка 5',
            'Професорът',
            null,
        ]);
        expect(rows()[0].querySelector('atlas-site-status-tag')?.textContent?.trim()).toBe('Текущ');
    });

    it('clicking a row emits open with its id', async () => {
        await render(sites);

        rows()[1].click();

        expect(opened).toEqual(['b']);
    });

    describe('money (Owner)', () => {
        const money = (row: HTMLElement) => row.querySelector<HTMLElement>('.money');
        const text = (node: Element | null | undefined) => node?.textContent?.replace(/\s+/g, ' ').trim();

        async function renderWithMoney(summaries: SummariesBySite) {
            fixture.componentRef.setInput('summaries', summaries);
            await render(sites);
        }

        it('shows no money without summaries', async () => {
            await render(sites);

            expect(rows().map(money)).toEqual([null, null, null]);
        });

        it('shows what was earned and „остават X €“ in warn color while something is owed', async () => {
            await renderWithMoney({
                a: testSummaryRow({ siteId: 'a', earned: 1157.89, paid: 500, remaining: 657.89 }),
            });

            const row = money(rows()[0])!;
            expect(text(row.querySelector('.money__earned'))).toBe('1 157,89 €');
            expect(row.querySelector('.money__earned')!.classList).toContain('num');
            expect(text(row.querySelector('.money__rest'))).toBe('остават 657,89 €');
            expect(row.querySelector('.money__rest')!.classList).toContain('money__rest--warn');
        });

        it('„изплатено“ in green when everything is paid', async () => {
            await renderWithMoney({ a: testSummaryRow({ siteId: 'a', earned: 500, paid: 500, remaining: 0 }) });

            const rest = money(rows()[0])!.querySelector('.money__rest')!;
            expect(text(rest)).toBe('изплатено');
            expect(rest.classList).toContain('money__rest--good');
        });

        it('„надплатено X €“ in green when the client paid more', async () => {
            await renderWithMoney({ a: testSummaryRow({ siteId: 'a', earned: 400, paid: 500, remaining: -100 }) });

            const rest = money(rows()[0])!.querySelector('.money__rest')!;
            expect(text(rest)).toBe('надплатено 100,00 €');
            expect(rest.classList).toContain('money__rest--good');
        });

        it('nothing for a site without work and payments, or without a summary', async () => {
            await renderWithMoney({ b: testSummaryRow({ siteId: 'b', earned: 0, paid: 0, remaining: 0 }) });

            expect(rows().map(money)).toEqual([null, null, null]);
        });

        it('a payment before any work shows 0,00 € and „надплатено X €“', async () => {
            await renderWithMoney({ b: testSummaryRow({ siteId: 'b', earned: 0, paid: 200, remaining: -200 }) });

            const row = money(rows()[1])!;
            expect(text(row.querySelector('.money__earned'))).toBe('0,00 €');
            expect(text(row.querySelector('.money__rest'))).toBe('надплатено 200,00 €');
        });
    });
});
