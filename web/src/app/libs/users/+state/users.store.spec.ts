import { TestBed } from '@angular/core/testing';
import { CreateUserRequest } from '../user.model';
import { UsersService } from '../users.service';
import { FakeUsersService, httpError } from '../testing/fake-users.service';
import { UsersStore } from './users.store';

const request: CreateUserRequest = {
    userName: 'gosho',
    password: '12345678',
    role: 'Manager',
    companyId: null,
    personId: null,
};

describe('UsersStore', () => {
    let fake: FakeUsersService;
    let store: InstanceType<typeof UsersStore>;

    const names = () => store.users().map((u) => u.userName);

    beforeEach(() => {
        fake = new FakeUsersService();
        TestBed.configureTestingModule({ providers: [{ provide: UsersService, useValue: fake }] });
        store = TestBed.inject(UsersStore);
    });

    it('starts empty', () => {
        expect(store.users()).toEqual([]);
        expect(store.loading()).toBe(false);
        expect(store.saving()).toBe(false);
        expect(store.error()).toBeNull();
        expect(store.fieldErrors()).toEqual({});
    });

    it('load fills the list', async () => {
        await store.load();

        expect(fake.calls).toEqual(['list']);
        expect(names()).toEqual(['iva', 'pesho', 'zheni']);
    });

    it('load error sets a message', async () => {
        fake.listResult = httpError(500);

        await store.load();

        expect(store.error()).toBe('Грешка при зареждане на профилите');
        expect(store.loading()).toBe(false);
    });

    it('create adds the profile in name order', async () => {
        await store.load();

        const created = await store.create(request);

        expect(created?.userName).toBe('gosho');
        expect(names()).toEqual(['gosho', 'iva', 'pesho', 'zheni']);
        expect(store.saving()).toBe(false);
    });

    it('create 409 shows the title; 400 the field messages', async () => {
        fake.createResult = httpError(409, { title: 'Има такъв потребител' });
        expect(await store.create(request)).toBeNull();
        expect(store.error()).toBe('Има такъв потребител');

        fake.createResult = httpError(400, { errors: { Password: ['Поне 8 знака'] } });
        expect(await store.create(request)).toBeNull();
        expect(store.error()).toBeNull();
        expect(store.fieldErrors()).toEqual({ password: 'Поне 8 знака' });
    });

    it('setPassword sends the password; 404 gives a Bulgarian message', async () => {
        expect(await store.setPassword('u2', '87654321')).toBe(true);
        expect(fake.calls).toContain('password u2');
        expect(fake.lastPassword).toEqual({ password: '87654321' });

        fake.setPasswordResult = httpError(404, { title: 'Not Found' });
        expect(await store.setPassword('u2', '87654321')).toBe(false);
        expect(store.error()).toBe('Профилът вече го няма');
    });

    it('remove drops the profile; 409 keeps it and shows the reason', async () => {
        await store.load();

        expect(await store.remove('u2')).toBe(true);
        expect(names()).toEqual(['iva', 'zheni']);

        fake.removeResult = httpError(409, { title: 'Трябва да остане поне един Owner.' });
        expect(await store.remove('u1')).toBe(false);
        expect(store.error()).toBe('Трябва да остане поне един Owner.');
        expect(names()).toEqual(['iva', 'zheni']);
    });

    it('linkPerson replaces the profile with the reply; 409 shows the reason', async () => {
        await store.load();

        const updated = await store.linkPerson('u2', 'p3');

        expect(fake.calls).toContain('person u2 p3');
        expect(updated?.personName).toBe('Пешо');
        expect(store.users().find((u) => u.id === 'u2')?.personId).toBe('p3');

        expect(await store.linkPerson('u2', null)).not.toBeNull();
        expect(fake.calls).toContain('person u2 null');
        expect(store.users().find((u) => u.id === 'u2')?.personId).toBeNull();

        fake.linkResult = httpError(409, { title: 'Човекът вече е свързан с друг профил' });
        expect(await store.linkPerson('u2', 'p1')).toBeNull();
        expect(store.error()).toBe('Човекът вече е свързан с друг профил');
    });

    it('clearErrors clears the messages', async () => {
        fake.createResult = httpError(409, { title: 'Има такъв потребител' });
        await store.create(request);

        store.clearErrors();

        expect(store.error()).toBeNull();
    });
});
