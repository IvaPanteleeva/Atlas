import { TestBed } from '@angular/core/testing';
import { PeopleService } from '../people.service';
import { SavePersonRequest } from '../person.model';
import { FakePeopleService, httpError } from '../testing/fake-people.service';
import { PeopleStore } from './people.store';

const request: SavePersonRequest = {
    name: 'Владо',
    phone: '0877 000 111',
    note: null,
    isCrew: true,
    dailyRate: null,
};

describe('PeopleStore', () => {
    let fake: FakePeopleService;
    let store: InstanceType<typeof PeopleStore>;

    const names = () => store.people().map((p) => p.name);

    beforeEach(() => {
        fake = new FakePeopleService();
        TestBed.configureTestingModule({ providers: [{ provide: PeopleService, useValue: fake }] });
        store = TestBed.inject(PeopleStore);
    });

    it('starts empty', () => {
        expect(store.people()).toEqual([]);
        expect(store.crew()).toEqual([]);
        expect(store.contacts()).toEqual([]);
        expect(store.loading()).toBe(false);
        expect(store.saving()).toBe(false);
        expect(store.error()).toBeNull();
        expect(store.fieldErrors()).toEqual({});
    });

    it('load fills the list; crew and contacts are split by isCrew', async () => {
        await store.load();

        expect(fake.calls).toEqual(['list']);
        expect(names()).toEqual(['Гошо', 'Иван бояджия', 'Пешо']);
        expect(store.crew().map((p) => p.name)).toEqual(['Гошо', 'Пешо']);
        expect(store.contacts().map((p) => p.name)).toEqual(['Иван бояджия']);
        expect(store.loading()).toBe(false);
    });

    it('load error sets a message', async () => {
        fake.listResult = httpError(500);

        await store.load();

        expect(store.error()).toBe('Грешка при зареждане на хората');
        expect(store.loading()).toBe(false);
    });

    it('create adds the person in name order (Bulgarian)', async () => {
        await store.load();

        const created = await store.create(request);

        expect(created?.id).toBe('new');
        expect(fake.calls).toContain('create Владо');
        expect(names()).toEqual(['Владо', 'Гошо', 'Иван бояджия', 'Пешо']);
        expect(store.crew().map((p) => p.name)).toEqual(['Владо', 'Гошо', 'Пешо']);
        expect(store.saving()).toBe(false);
    });

    it('create 400 keeps the first message per field and returns null', async () => {
        fake.createResult = httpError(400, { errors: { Name: ['Въведи име'], phone: ['Твърде дълъг'] } });

        expect(await store.create(request)).toBeNull();

        expect(store.fieldErrors()).toEqual({ name: 'Въведи име', phone: 'Твърде дълъг' });
        expect(store.error()).toBeNull();
        expect(store.saving()).toBe(false);
    });

    it('update replaces the person (moving between crew and contacts)', async () => {
        await store.load();

        const updated = await store.update('p3', {
            name: 'Пешо',
            phone: null,
            note: null,
            isCrew: false,
            dailyRate: null,
        });

        expect(updated?.isCrew).toBe(false);
        expect(fake.calls).toContain('update p3 Пешо');
        expect(store.crew().map((p) => p.name)).toEqual(['Гошо']);
        expect(store.contacts().map((p) => p.name)).toEqual(['Иван бояджия', 'Пешо']);
    });

    it('update 404 shows a Bulgarian message', async () => {
        await store.load();
        fake.updateResult = httpError(404, { title: 'Not Found' });

        expect(await store.update('p1', request)).toBeNull();

        expect(store.error()).toBe('Човекът вече го няма');
    });

    it('remove drops the person', async () => {
        await store.load();

        expect(await store.remove('p2')).toBe(true);

        expect(fake.calls).toContain('remove p2');
        expect(names()).toEqual(['Гошо', 'Пешо']);
    });

    it('remove 409 keeps the person and shows the reason', async () => {
        await store.load();
        fake.removeResult = httpError(409, { title: 'Човекът има работни дни' });

        expect(await store.remove('p1')).toBe(false);

        expect(store.error()).toBe('Човекът има работни дни');
        expect(names()).toEqual(['Гошо', 'Иван бояджия', 'Пешо']);
    });

    it('clearErrors clears the messages', async () => {
        fake.createResult = httpError(409, { title: 'Нещо' });
        await store.create(request);

        store.clearErrors();

        expect(store.error()).toBeNull();
        expect(store.fieldErrors()).toEqual({});
    });
});
