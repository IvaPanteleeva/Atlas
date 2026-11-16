import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';
import { SaveSiteRequest, Site, SiteListItem } from '../site.model';
import { SitesService } from '../sites.service';
import { SitesStore } from './sites.store';

function httpError(status: number, error: unknown = null) {
    return throwError(() => new HttpErrorResponse({ status, error }));
}

function site(overrides: Partial<Site> = {}): Site {
    return {
        id: 's1',
        name: 'Професора',
        status: 'Lead',
        companyId: null,
        companyName: null,
        endCustomer: null,
        address: null,
        mapUrl: null,
        note: null,
        ...overrides,
    };
}

function item(overrides: Partial<SiteListItem> = {}): SiteListItem {
    return {
        id: 's1',
        name: 'Професора',
        status: 'Lead',
        companyName: null,
        endCustomer: null,
        address: null,
        ...overrides,
    };
}

const request: SaveSiteRequest = {
    name: 'Професора',
    status: 'Lead',
    companyId: null,
    endCustomer: null,
    address: null,
    mapUrl: null,
    note: null,
};

/** Hand-written fake: each test sets what the next call returns. */
class FakeSitesService {
    listResult: Observable<SiteListItem[]> = of([]);
    getResult: Observable<Site> = of(site());
    createResult: Observable<Site> = of(site());
    updateResult: Observable<Site> = of(site());
    removeResult: Observable<void> = of(undefined);
    calls: string[] = [];

    list(): Observable<SiteListItem[]> {
        this.calls.push('list');
        return this.listResult;
    }

    get(id: string): Observable<Site> {
        this.calls.push(`get ${id}`);
        return this.getResult;
    }

    create(request: SaveSiteRequest): Observable<Site> {
        this.calls.push(`create ${request.name}`);
        return this.createResult;
    }

    update(id: string, request: SaveSiteRequest): Observable<Site> {
        this.calls.push(`update ${id} ${request.name}`);
        return this.updateResult;
    }

    remove(id: string): Observable<void> {
        this.calls.push(`remove ${id}`);
        return this.removeResult;
    }
}

describe('SitesStore', () => {
    let fake: FakeSitesService;
    let store: InstanceType<typeof SitesStore>;

    const names = () => store.list().map((s) => s.name);

    beforeEach(() => {
        fake = new FakeSitesService();
        TestBed.configureTestingModule({ providers: [{ provide: SitesService, useValue: fake }] });
        store = TestBed.inject(SitesStore);
    });

    async function loaded(list: SiteListItem[]) {
        fake.listResult = of(list);
        await store.load();
    }

    it('starts empty', () => {
        expect(store.list()).toEqual([]);
        expect(store.selected()).toBeNull();
        expect(store.loading()).toBe(false);
        expect(store.saving()).toBe(false);
        expect(store.error()).toBeNull();
        expect(store.fieldErrors()).toEqual({});
        expect(store.notFound()).toBe(false);
    });

    it('load keeps the API order', async () => {
        await loaded([
            item({ id: 'b', name: 'Бета', status: 'Active' }),
            item({ id: 'a', name: 'Алфа', status: 'Lead' }),
        ]);

        expect(names()).toEqual(['Бета', 'Алфа']);
        expect(store.loading()).toBe(false);
    });

    it('load error sets a message', async () => {
        fake.listResult = httpError(500);

        await store.load();

        expect(store.error()).toBe('Грешка при зареждане на обектите');
        expect(store.loading()).toBe(false);
    });

    it('loadOne fills selected', async () => {
        fake.getResult = of(site({ id: 'x', name: 'Х' }));

        await store.loadOne('x');

        expect(fake.calls).toEqual(['get x']);
        expect(store.selected()?.name).toBe('Х');
        expect(store.notFound()).toBe(false);
        expect(store.loading()).toBe(false);
    });

    it('loadOne 404 sets notFound and clears the previous site', async () => {
        await store.loadOne('s1');
        fake.getResult = httpError(404, { title: 'Not Found' });

        await store.loadOne('missing');

        expect(store.notFound()).toBe(true);
        expect(store.selected()).toBeNull();
        expect(store.error()).toBeNull();
    });

    it('loadOne other error sets a message, not notFound', async () => {
        fake.getResult = httpError(500);

        await store.loadOne('s1');

        expect(store.notFound()).toBe(false);
        expect(store.error()).toBe('Грешка при зареждане на обекта');
    });

    it('create returns the site, selects it and adds it to the list in API order', async () => {
        await loaded([
            item({ id: 'a', name: 'Алфа', status: 'Active' }),
            item({ id: 'v', name: 'Вега', status: 'Lead' }),
            item({ id: 'd', name: 'Гама', status: 'Done' }),
        ]);
        fake.createResult = of(site({ id: 'b', name: 'Бета', status: 'Lead', endCustomer: 'Професорът' }));

        const created = await store.create({ ...request, name: 'Бета' });

        expect(created?.id).toBe('b');
        expect(fake.calls).toContain('create Бета');
        expect(store.selected()?.id).toBe('b');
        expect(names()).toEqual(['Алфа', 'Бета', 'Вега', 'Гама']);
        expect(store.list()[1]).toEqual(item({ id: 'b', name: 'Бета', status: 'Lead', endCustomer: 'Професорът' }));
        expect(store.saving()).toBe(false);
    });

    it('create 400 keeps the first message per field (camelCase) and returns null', async () => {
        fake.createResult = httpError(400, {
            title: 'One or more validation errors occurred.',
            errors: { name: ['Въведи име', 'Друго'], MapUrl: ['Линкът трябва да е http или https'] },
        });

        const created = await store.create(request);

        expect(created).toBeNull();
        expect(store.fieldErrors()).toEqual({ name: 'Въведи име', mapUrl: 'Линкът трябва да е http или https' });
        expect(store.error()).toBeNull();
        expect(store.saving()).toBe(false);
    });

    it('an error without details falls back to "Грешка при записа"', async () => {
        fake.createResult = httpError(500);

        await store.create(request);

        expect(store.error()).toBe('Грешка при записа');
        expect(store.fieldErrors()).toEqual({});
    });

    it('a new save clears the previous errors', async () => {
        fake.createResult = httpError(400, { errors: { name: ['Въведи име'] } });
        await store.create(request);
        fake.createResult = of(site());

        await store.create(request);

        expect(store.fieldErrors()).toEqual({});
        expect(store.error()).toBeNull();
    });

    it('update replaces selected and refreshes the list item (and its place)', async () => {
        await loaded([
            item({ id: 'a', name: 'Алфа', status: 'Active' }),
            item({ id: 's1', name: 'Професора', status: 'Lead' }),
        ]);
        await store.loadOne('s1');
        fake.updateResult = of(site({ id: 's1', name: 'Бункера', status: 'Done', address: 'ул. 1' }));

        const updated = await store.update('s1', { ...request, name: 'Бункера', status: 'Done' });

        expect(updated?.name).toBe('Бункера');
        expect(fake.calls).toContain('update s1 Бункера');
        expect(store.selected()?.name).toBe('Бункера');
        expect(store.list()).toEqual([
            item({ id: 'a', name: 'Алфа', status: 'Active' }),
            item({ id: 's1', name: 'Бункера', status: 'Done', address: 'ул. 1' }),
        ]);
    });

    it('update 404 sets notFound and returns null', async () => {
        fake.updateResult = httpError(404, { title: 'Not Found' });

        const updated = await store.update('s1', request);

        expect(updated).toBeNull();
        expect(store.notFound()).toBe(true);
    });

    it('remove drops the site from the list and selected', async () => {
        await loaded([item({ id: 'a', name: 'Алфа' }), item({ id: 's1' })]);
        await store.loadOne('s1');

        const ok = await store.remove('s1');

        expect(ok).toBe(true);
        expect(fake.calls).toContain('remove s1');
        expect(names()).toEqual(['Алфа']);
        expect(store.selected()).toBeNull();
    });

    it('remove 409 keeps the site and shows the ProblemDetails title', async () => {
        await loaded([item()]);
        await store.loadOne('s1');
        fake.removeResult = httpError(409, { title: 'Обектът има работни дни или плащания' });

        const ok = await store.remove('s1');

        expect(ok).toBe(false);
        expect(store.error()).toBe('Обектът има работни дни или плащания');
        expect(store.list().length).toBe(1);
        expect(store.selected()?.id).toBe('s1');
    });

    it('clearErrors hides the messages', async () => {
        fake.createResult = httpError(400, { errors: { name: ['Въведи име'] } });
        await store.create(request);

        store.clearErrors();

        expect(store.fieldErrors()).toEqual({});
        expect(store.error()).toBeNull();
    });
});
