import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { httpError } from '../../services/testing/fake-services.service';
import { SummariesService } from '../summaries.service';
import { FakeSummariesService, testSiteSummary, testSummaryRow, testTotals } from '../testing/fake-summaries.service';
import { SummariesStore } from './summaries.store';

describe('SummariesStore', () => {
    let fake: FakeSummariesService;
    let store: InstanceType<typeof SummariesStore>;

    beforeEach(() => {
        fake = new FakeSummariesService();
        TestBed.configureTestingModule({ providers: [{ provide: SummariesService, useValue: fake }] });
        store = TestBed.inject(SummariesStore);
    });

    it('starts empty', () => {
        expect(store.site()).toBeNull();
        expect(store.totals()).toBeNull();
        expect(store.bySite()).toEqual({});
        expect(store.error()).toBeNull();
    });

    describe('one site', () => {
        it('loadSite fills the summary of the site', async () => {
            await store.loadSite('s1');

            expect(fake.calls).toEqual(['site s1']);
            expect(store.siteId()).toBe('s1');
            expect(store.site()).toEqual(testSiteSummary());
            expect(store.siteLoading()).toBe(false);
        });

        it('loadSite of another site forgets the previous summary first; an error shows a message', async () => {
            await store.loadSite('s1');
            fake.siteResult = httpError(500);

            const pending = store.loadSite('s2');
            expect(store.site()).toBeNull();
            await pending;

            expect(store.site()).toBeNull();
            expect(store.error()).toBe('Грешка при зареждане на сумите');
        });

        it('refreshSite reloads quietly: the old summary stays until the reply, a failure keeps it', async () => {
            await store.loadSite('s1');
            fake.siteResult = of(testSiteSummary({ paid: 700, remaining: 457.89 }));

            const pending = store.refreshSite('s1');
            expect(store.site()?.paid).toBe(500);
            expect(store.siteLoading()).toBe(false);
            await pending;
            expect(store.site()?.paid).toBe(700);

            fake.siteResult = httpError(500);
            await store.refreshSite('s1');
            expect(store.site()?.paid).toBe(700);
            expect(store.error()).toBeNull();
        });

        it('a late reply for a site that is no longer open is dropped', async () => {
            await store.loadSite('s1');
            await store.loadSite('s2');
            fake.siteResult = of(testSiteSummary({ earned: 1 }));

            await store.refreshSite('s1');

            expect(store.siteId()).toBe('s2');
            expect(store.site()?.earned).toBe(1157.89);
        });
    });

    describe('all sites', () => {
        it('loadAll fills the totals and the summaries by site id', async () => {
            fake.allResult = of({
                totals: testTotals({ earned: 1300, paid: 500, owed: 800, openCount: 3 }),
                sites: [
                    testSummaryRow({ siteId: 's1' }),
                    testSummaryRow({ siteId: 's2', earned: 142.11, paid: 0, remaining: 142.11, openCount: 3 }),
                ],
            });

            await store.loadAll();

            expect(fake.calls).toEqual(['all']);
            expect(store.totals()).toEqual({
                earned: 1300,
                paid: 500,
                owed: 800,
                overpaid: 0,
                overpaidSiteCount: 0,
                openCount: 3,
                workerCost: 400,
                workerOpenCount: 0,
                net: 757.89,
            });
            expect(Object.keys(store.bySite())).toEqual(['s1', 's2']);
            expect(store.bySite()['s2'].remaining).toBe(142.11);
            expect(store.allLoading()).toBe(false);
        });

        it('a reload keeps the old totals until the reply; an error shows a message and keeps them', async () => {
            await store.loadAll();
            fake.allResult = httpError(500);

            const pending = store.loadAll();
            expect(store.totals()?.earned).toBe(1157.89);
            expect(store.allLoading()).toBe(true);
            await pending;

            expect(store.totals()?.earned).toBe(1157.89);
            expect(store.error()).toBe('Грешка при зареждане на сумите');
            expect(store.allLoading()).toBe(false);
        });
    });
});
