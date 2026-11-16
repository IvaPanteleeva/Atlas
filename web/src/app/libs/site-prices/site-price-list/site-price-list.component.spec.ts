import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SitePrice } from '../site-price.model';
import { testSitePrice } from '../testing/fake-site-prices.service';
import { SitePriceListComponent } from './site-price-list.component';

const prices: SitePrice[] = [
    testSitePrice({ id: 'p1', serviceName: 'Боядисване', unit: 'SquareMeter', rate: 1157.89 }),
    testSitePrice({ id: 'p2', serviceName: 'Первази', unit: 'LinearMeter', rate: 6.5 }),
];

describe('SitePriceListComponent', () => {
    let fixture: ComponentFixture<SitePriceListComponent>;
    let element: HTMLElement;
    let opened: SitePrice[];

    beforeEach(() => {
        fixture = TestBed.createComponent(SitePriceListComponent);
        opened = [];
        fixture.componentInstance.open.subscribe((price) => opened.push(price));
        element = fixture.nativeElement as HTMLElement;
    });

    async function render(list: SitePrice[], canEdit: boolean) {
        fixture.componentRef.setInput('prices', list);
        fixture.componentRef.setInput('canEdit', canEdit);
        await fixture.whenStable();
    }

    const text = (node: Element | null | undefined) => node?.textContent?.replace(/\s+/g, ' ').trim();

    it('shows the empty state', async () => {
        await render([], true);

        expect(text(element.querySelector('.empty'))).toBe(
            'Няма договорени цени. При работа се ползва цената по подразбиране на услугата.',
        );
    });

    it('shows "service · unit" on the left and the rate in EUR on the right', async () => {
        await render(prices, false);

        const rows = Array.from(element.querySelectorAll('.price'));
        expect(rows.map((row) => text(row.querySelector('.name')))).toEqual(['Боядисване', 'Первази']);
        expect(rows.map((row) => text(row.querySelector('.unit')))).toEqual(['· м²', '· м.л.']);
        expect(rows.map((row) => row.querySelector('.rate')?.textContent?.trim())).toEqual(['1 157,89 €', '6,50 €']);
    });

    it('rows are clickable only when canEdit', async () => {
        await render(prices, false);
        expect(element.querySelectorAll('button').length).toBe(0);

        await render(prices, true);
        element.querySelectorAll<HTMLButtonElement>('button.price')[1].click();

        expect(opened).toEqual([prices[1]]);
    });
});
