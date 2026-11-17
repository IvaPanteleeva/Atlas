import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { httpError } from '../../sites/testing/fake-sites.service';
import { PortalService } from '../portal.service';
import { PortalSites } from '../portal.model';
import {
    FakePortalService,
    testPortalSite,
    testPortalSiteListItem,
    testPortalTotals,
} from '../testing/fake-portal.service';
import { PortalStore } from './portal.store';

describe('PortalStore', () => {
    let fake: FakePortalService;
    let store: InstanceType<typeof PortalStore>;

    beforeEach(() => {
        fake = new FakePortalService();
        // Not in root: each portal page provides its own store (see the pages).
        TestBed.configureTestingModule({ providers: [PortalStore, { provide: PortalService, useValue: fake }] });
        store = TestBed.inject(PortalStore);
    });

    it('loads the sites and the totals', async () => {
        await store.loadSites();

        expect(fake.calls).toEqual(['sites']);
        expect(store.sites().map((site) => site.name)).toEqual(['Бункера', 'Професора']);
        expect(store.totals()).toEqual(testPortalTotals());
        expect(store.loading()).toBe(false);
        expect(store.error()).toBeNull();
    });

    it('a failed list load keeps nothing and shows a message', async () => {
        fake.sitesResult = httpError(500);

        await store.loadSites();

        expect(store.sites()).toEqual([]);
        expect(store.totals()).toBeNull();
        expect(store.error()).toBe('Грешка при зареждане на обектите');
    });

    it('loads one site', async () => {
        await store.loadSite('s1');

        expect(fake.calls).toEqual(['site s1']);
        expect(store.selected()).toEqual(testPortalSite());
        expect(store.notFound()).toBe(false);
    });

    it('a 404 (also a site of another company) sets notFound', async () => {
        fake.siteResult = httpError(404, { title: 'Няма такъв обект.' });

        await store.loadSite('x');

        expect(store.selected()).toBeNull();
        expect(store.notFound()).toBe(true);
        expect(store.error()).toBeNull();
    });

    it('another error on one site shows the title from the server or a fallback', async () => {
        fake.siteResult = httpError(403, { title: 'Нямате достъп.' });
        await store.loadSite('s1');
        expect(store.error()).toBe('Нямате достъп.');

        fake.siteResult = httpError(500);
        await store.loadSite('s1');
        expect(store.error()).toBe('Грешка при зареждане на обекта');
    });

    it('opening another site first forgets the previous one', async () => {
        await store.loadSite('s1');
        fake.siteResult = of(testPortalSite({ id: 's2', name: 'Професора' }));

        const pending = store.loadSite('s2');
        expect(store.selected()).toBeNull();
        await pending;

        expect(store.selected()?.name).toBe('Професора');
    });
    it('a new list load drops the old sites, totals and error at once, before the answer comes', async () => {
        fake.sitesResult = httpError(500);
        await store.loadSites();
        fake.sitesResult = of({ sites: [testPortalSiteListItem()], totals: testPortalTotals() });
        await store.loadSites();
        expect(store.sites().length).toBe(1);

        const pending = new Subject<PortalSites>();
        fake.sitesResult = pending;
        const load = store.loadSites();

        expect(store.sites()).toEqual([]);
        expect(store.totals()).toBeNull();
        expect(store.error()).toBeNull();
        expect(store.loading()).toBe(true);

        pending.next({ sites: [testPortalSiteListItem({ id: 'b1', name: 'Гаража' })], totals: testPortalTotals() });
        pending.complete();
        await load;
        expect(store.sites().map((site) => site.name)).toEqual(['Гаража']);
    });

    it('is not a root singleton: without a provider (a page) there is no store', () => {
        TestBed.resetTestingModule();
        TestBed.configureTestingModule({ providers: [{ provide: PortalService, useValue: fake }] });

        expect(() => TestBed.inject(PortalStore)).toThrow(/No provider/);
    });
});
