import { Observable, of } from 'rxjs';
import { PortalSite, PortalSiteListItem, PortalSites, PortalTotals } from '../portal.model';

/** Test data and a hand-written PortalService fake shared by the portal specs. */
export function testPortalSiteListItem(overrides: Partial<PortalSiteListItem> = {}): PortalSiteListItem {
    return {
        id: 's1',
        name: 'Бункера',
        endCustomer: 'Професорът',
        status: 'Active',
        earned: 1157.89,
        paid: 500,
        remaining: 657.89,
        openCount: 0,
        ...overrides,
    };
}

export function testPortalTotals(overrides: Partial<PortalTotals> = {}): PortalTotals {
    return { owed: 657.89, overpaid: 0, overpaidSiteCount: 0, ...overrides };
}

export function testPortalSite(overrides: Partial<PortalSite> = {}): PortalSite {
    return {
        id: 's1',
        name: 'Бункера',
        endCustomer: 'Професорът',
        address: 'ул. Шипка 5',
        mapUrl: 'https://maps.example/x',
        status: 'Active',
        summary: { earned: 1157.89, paid: 500, remaining: 657.89, openCount: 2 },
        workDays: [
            {
                id: 'd1',
                date: '2026-11-16',
                title: 'Шпакловка финал',
                total: 1157.89,
                items: [
                    {
                        description: 'Шпакловка',
                        quantity: 12.5,
                        unit: 'SquareMeter',
                        rate: 6.5,
                        needsReview: false,
                        amount: 81.25,
                    },
                    {
                        description: 'Первази',
                        quantity: 3,
                        unit: 'LinearMeter',
                        rate: null,
                        needsReview: false,
                        amount: null,
                    },
                    {
                        description: 'Боя',
                        quantity: 2,
                        unit: 'Piece',
                        rate: 538.32,
                        needsReview: true,
                        amount: 1076.64,
                    },
                ],
            },
        ],
        payments: [
            { date: '2026-11-03', amount: 300 },
            { date: '2026-11-10', amount: 200 },
        ],
        ...overrides,
    };
}

export class FakePortalService {
    sitesResult: Observable<PortalSites> = of({
        sites: [
            testPortalSiteListItem(),
            testPortalSiteListItem({
                id: 's2',
                name: 'Професора',
                endCustomer: null,
                status: 'Lead',
                earned: 0,
                paid: 0,
                remaining: 0,
                openCount: 0,
            }),
        ],
        totals: testPortalTotals(),
    });
    siteResult: Observable<PortalSite> = of(testPortalSite());
    calls: string[] = [];

    sites(): Observable<PortalSites> {
        this.calls.push('sites');
        return this.sitesResult;
    }

    site(id: string): Observable<PortalSite> {
        this.calls.push(`site ${id}`);
        return this.siteResult;
    }
}
