import { Observable, of } from 'rxjs';
import { SiteSummary, SiteSummaryRow, SitesSummaries, SummaryTotals } from '../summary.model';

/** Test data and a hand-written SummariesService fake shared by the specs. */
export function testSiteSummary(overrides: Partial<SiteSummary> = {}): SiteSummary {
    return {
        earned: 1157.89,
        paid: 500,
        remaining: 657.89,
        openCount: 0,
        workDayCount: 2,
        lastWorkDate: '2026-11-14',
        ...overrides,
    };
}

export function testSummaryRow(overrides: Partial<SiteSummaryRow> = {}): SiteSummaryRow {
    return { siteId: 's1', earned: 1157.89, paid: 500, remaining: 657.89, openCount: 0, ...overrides };
}

export function testTotals(overrides: Partial<SummaryTotals> = {}): SummaryTotals {
    return { earned: 1157.89, paid: 500, owed: 657.89, overpaid: 0, overpaidSiteCount: 0, openCount: 0, ...overrides };
}

export class FakeSummariesService {
    siteResult: Observable<SiteSummary> = of(testSiteSummary());
    allResult: Observable<SitesSummaries> = of({
        totals: testTotals(),
        sites: [testSummaryRow()],
    });
    calls: string[] = [];

    getSite(siteId: string): Observable<SiteSummary> {
        this.calls.push(`site ${siteId}`);
        return this.siteResult;
    }

    getAll(): Observable<SitesSummaries> {
        this.calls.push('all');
        return this.allResult;
    }
}
