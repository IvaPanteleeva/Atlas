import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PortalSiteListItem } from '../portal.model';
import { testPortalSiteListItem } from '../testing/fake-portal.service';
import { PortalSiteListComponent } from './portal-site-list.component';

describe('PortalSiteListComponent', () => {
    let fixture: ComponentFixture<PortalSiteListComponent>;
    let element: HTMLElement;

    const text = (node: Element | null | undefined) => node?.textContent?.replace(/\s+/g, ' ').trim();
    const rows = () => Array.from(element.querySelectorAll<HTMLAnchorElement>('a.site'));

    beforeEach(() => {
        TestBed.configureTestingModule({ providers: [provideRouter([])] });
        fixture = TestBed.createComponent(PortalSiteListComponent);
        element = fixture.nativeElement as HTMLElement;
    });

    async function render(sites: PortalSiteListItem[]) {
        fixture.componentRef.setInput('sites', sites);
        await fixture.whenStable();
    }

    it('shows the empty state', async () => {
        await render([]);

        expect(text(element)).toBe('Още няма обекти.');
    });

    it('a row per site with name, status, end customer and a link to the portal site', async () => {
        await render([
            testPortalSiteListItem(),
            testPortalSiteListItem({ id: 's2', name: 'Гаража', endCustomer: null, status: 'Done' }),
        ]);

        expect(rows().map((row) => text(row.querySelector('.name')))).toEqual(['Бункера', 'Гаража']);
        expect(rows().map((row) => text(row.querySelector('.meta')) ?? null)).toEqual(['Професорът', null]);
        expect(rows().map((row) => text(row.querySelector('atlas-site-status-tag')))).toEqual(['Текущ', 'Приключен']);
        expect(rows().map((row) => row.getAttribute('href'))).toEqual(['/portal/s1', '/portal/s2']);
    });

    it('earned and „остават X €“ while something is owed', async () => {
        await render([testPortalSiteListItem({ earned: 1157.89, paid: 500, remaining: 657.89 })]);

        expect(text(rows()[0].querySelector('.money__earned'))).toBe('1 157,89 €');
        const rest = rows()[0].querySelector('.money__rest')!;
        expect(text(rest)).toBe('остават 657,89 €');
        expect(rest.classList).toContain('money__rest--warn');
    });

    it('„изплатено“ and „надплатено X €“ in green', async () => {
        await render([
            testPortalSiteListItem({ id: 'a', earned: 500, paid: 500, remaining: 0 }),
            testPortalSiteListItem({ id: 'b', earned: 400, paid: 500, remaining: -100 }),
        ]);

        expect(rows().map((row) => text(row.querySelector('.money__rest')))).toEqual([
            'изплатено',
            'надплатено 100,00 €',
        ]);
        expect(rows()[1].querySelector('.money__rest')!.classList).toContain('money__rest--good');
    });

    it('no money for a site without work and payments', async () => {
        await render([testPortalSiteListItem({ earned: 0, paid: 0, remaining: 0 })]);

        expect(rows()[0].querySelector('.money')).toBeNull();
    });

    it('„N ?“ when items are still to be clarified', async () => {
        await render([
            testPortalSiteListItem({ id: 'a', openCount: 3 }),
            testPortalSiteListItem({ id: 'b', openCount: 0 }),
        ]);

        expect(text(rows()[0].querySelector('.pill.q'))).toBe('3 ?');
        expect(rows()[1].querySelector('.pill.q')).toBeNull();
    });
});
