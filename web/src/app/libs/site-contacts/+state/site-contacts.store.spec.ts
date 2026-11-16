import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { SaveSiteContactRequest } from '../site-contact.model';
import { SiteContactsService } from '../site-contacts.service';
import { FakeSiteContactsService, httpError, testSiteContact } from '../testing/fake-site-contacts.service';
import { SiteContactsStore } from './site-contacts.store';

const request: SaveSiteContactRequest = { name: 'Баба Мара', role: 'съсед', phone: null, note: 'Има ключ' };

describe('SiteContactsStore', () => {
    let fake: FakeSiteContactsService;
    let store: InstanceType<typeof SiteContactsStore>;

    const names = () => store.contacts().map((c) => c.name);

    beforeEach(() => {
        fake = new FakeSiteContactsService();
        fake.listResult = of([
            testSiteContact({ id: 'sc1', name: 'Професорът' }),
            testSiteContact({ id: 'sc2', name: 'Шофьор', role: null }),
        ]);
        TestBed.configureTestingModule({ providers: [{ provide: SiteContactsService, useValue: fake }] });
        store = TestBed.inject(SiteContactsStore);
    });

    it('starts empty', () => {
        expect(store.siteId()).toBeNull();
        expect(store.contacts()).toEqual([]);
        expect(store.loading()).toBe(false);
        expect(store.saving()).toBe(false);
        expect(store.error()).toBeNull();
    });

    it('load fills the contacts of the site', async () => {
        await store.load('s1');

        expect(fake.calls).toEqual(['list s1']);
        expect(store.siteId()).toBe('s1');
        expect(names()).toEqual(['Професорът', 'Шофьор']);
    });

    it('another site drops the old contacts first; a load error sets a message', async () => {
        await store.load('s1');
        fake.listResult = httpError(500);

        const pending = store.load('s2');
        expect(store.contacts()).toEqual([]);
        await pending;

        expect(store.error()).toBe('Грешка при зареждане на контактите');
    });

    it('create adds the contact in name order (Bulgarian)', async () => {
        await store.load('s1');

        expect((await store.create('s1', request))?.id).toBe('new');

        expect(fake.calls).toContain('create s1 Баба Мара');
        expect(names()).toEqual(['Баба Мара', 'Професорът', 'Шофьор']);
        expect(store.saving()).toBe(false);
    });

    it('create 400 keeps the field messages', async () => {
        fake.createResult = httpError(400, { errors: { Role: ['Най-много 50 знака'] } });

        expect(await store.create('s1', request)).toBeNull();

        expect(store.fieldErrors()).toEqual({ role: 'Най-много 50 знака' });
    });

    it('update replaces the contact; 404 gives a Bulgarian message', async () => {
        await store.load('s1');

        await store.update('s1', 'sc2', { ...request, name: 'Ангел шофьор' });
        expect(fake.calls).toContain('update s1 sc2 Ангел шофьор');
        expect(names()).toEqual(['Ангел шофьор', 'Професорът']);

        fake.updateResult = httpError(404, { title: 'Not Found' });
        expect(await store.update('s1', 'sc1', request)).toBeNull();
        expect(store.error()).toBe('Контактът вече го няма');
    });

    it('remove drops the contact; a failure keeps it', async () => {
        await store.load('s1');

        expect(await store.remove('s1', 'sc1')).toBe(true);
        expect(names()).toEqual(['Шофьор']);

        fake.removeResult = httpError(403);
        expect(await store.remove('s1', 'sc2')).toBe(false);
        expect(names()).toEqual(['Шофьор']);
        expect(store.error()).toBe('Грешка при записа');
    });

    it('clearErrors clears the messages', async () => {
        fake.createResult = httpError(500);
        await store.create('s1', request);

        store.clearErrors();

        expect(store.error()).toBeNull();
    });
});
