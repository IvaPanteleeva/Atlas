import { TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { SiteTask } from '../task.model';
import { TasksService } from '../tasks.service';
import { FakeTasksService, httpError, testTask } from '../testing/fake-tasks.service';
import { TasksStore } from './tasks.store';

describe('TasksStore', () => {
    let fake: FakeTasksService;
    let store: InstanceType<typeof TasksStore>;

    const ids = () => store.tasks().map((task) => task.id);

    beforeEach(() => {
        fake = new FakeTasksService();
        fake.tasks = [
            testTask({ id: 'a', text: 'Лепило', createdAt: '2026-11-10T08:00:00Z' }),
            testTask({ id: 'b', text: 'Плочки', createdAt: '2026-11-12T08:00:00Z', assigneeId: 'p1' }),
            testTask({
                id: 'c',
                text: 'Кофи',
                done: true,
                createdAt: '2026-11-14T08:00:00Z',
                doneAt: '2026-11-15T08:00:00Z',
            }),
        ];
        TestBed.configureTestingModule({ providers: [{ provide: TasksService, useValue: fake }] });
        store = TestBed.inject(TasksStore);
    });

    it('starts empty', () => {
        expect(store.query()).toBeNull();
        expect(store.tasks()).toEqual([]);
        expect(store.loading()).toBe(false);
        expect(store.saving()).toBe(false);
        expect(store.error()).toBeNull();
        expect(store.fieldError()).toBeNull();
    });

    it('load sends the filter and keeps the server order', async () => {
        await store.load({ siteId: 's1' });

        expect(fake.calls).toEqual(['list siteId=s1']);
        expect(ids()).toEqual(['a', 'b', 'c']);
        expect(store.openCount()).toBe(2);

        await store.load({});
        expect(fake.calls).toEqual(['list siteId=s1', 'list']);
    });

    it('another filter drops the old tasks first; a load error sets a message', async () => {
        await store.load({ siteId: 's1' });
        fake.listResult = httpError(500);

        const pending = store.load({ siteId: 's2' });
        expect(store.tasks()).toEqual([]);
        await pending;

        expect(store.error()).toBe('Грешка при зареждане на задачите');
    });

    it('a late answer for an old filter is ignored', async () => {
        const slow = new Subject<SiteTask[]>();
        fake.listResult = slow;
        const first = store.load({ siteId: 's1' });
        fake.listResult = null;
        fake.tasks = [testTask({ id: 'x', siteId: 's2' })];
        await store.load({ siteId: 's2' });

        slow.next([testTask({ id: 'old' })]);
        slow.complete();
        await first;

        expect(ids()).toEqual(['x']);
    });

    it('create adds the task last among the open ones and sends the site and the person', async () => {
        await store.load({ siteId: 's1' });

        const created = await store.create({ text: 'Мазилка', siteId: 's1', assigneeId: 'p1' });

        expect(created?.id).toBe('new');
        expect(fake.lastCreate).toEqual({ text: 'Мазилка', siteId: 's1', assigneeId: 'p1' });
        expect(ids()).toEqual(['a', 'b', 'new', 'c']);
        expect(store.saving()).toBe(false);
    });

    it('create keeps out a task the filter does not show', async () => {
        await store.load({ assigneeId: 'p1' });

        await store.create({ text: 'Чужда', siteId: null, assigneeId: 'p2' });
        await store.create({ text: 'Моя', siteId: null, assigneeId: 'p1' });

        expect(store.tasks().map((task) => task.text)).toEqual(['Лепило', 'Плочки', 'Моя', 'Кофи']);
    });

    it('create 400 keeps the field message', async () => {
        fake.createResult = httpError(400, { errors: { Text: ['Въведи текст'] } });

        expect(await store.create({ text: ' ', siteId: null, assigneeId: null })).toBeNull();

        expect(store.fieldError()).toBe('Въведи текст');
        expect(store.error()).toBeNull();
    });

    it('update replaces the task; a 400 becomes the general message', async () => {
        await store.load({ siteId: 's1' });

        await store.update('a', { text: 'Лепило за плочки', assigneeId: 'p2' });
        expect(fake.calls).toContain('update a Лепило за плочки');
        expect(store.tasks()[0]).toMatchObject({ id: 'a', text: 'Лепило за плочки', assigneeId: 'p2' });

        fake.updateResult = httpError(400, { errors: { AssigneeId: ['Човекът не е от екипа'] } });
        expect(await store.update('a', { text: 'x', assigneeId: 'p9' })).toBeNull();
        expect(store.error()).toBe('Човекът не е от екипа');
        expect(store.fieldError()).toBeNull();
    });

    it('update drops a task the filter no longer shows', async () => {
        await store.load({ assigneeId: 'p1' });

        await store.update('b', { text: 'Плочки', assigneeId: null });

        expect(ids()).toEqual(['a', 'c']);
    });

    it('setDone moves the task at once (done: last done first; open again: by creation)', async () => {
        await store.load({ siteId: 's1' });

        const pending = store.setDone('a', true);
        expect(ids()).toEqual(['b', 'a', 'c']);
        expect(store.tasks()[1].done).toBe(true);
        await pending;
        expect(fake.calls).toContain('done a true');
        expect(store.tasks()[1].doneAt).toBe('2026-11-17T10:00:00Z');

        await store.setDone('c', false);
        expect(ids()).toEqual(['b', 'c', 'a']);
        expect(store.tasks()[1]).toMatchObject({ done: false, doneAt: null });
    });

    it('setDone failure puts the task back and shows a message; 404 says it is gone', async () => {
        await store.load({ siteId: 's1' });
        fake.setDoneResult = httpError(500);

        expect(await store.setDone('a', true)).toBe(false);
        expect(ids()).toEqual(['a', 'b', 'c']);
        expect(store.tasks()[0].done).toBe(false);
        expect(store.error()).toBe('Грешка при записа');

        fake.setDoneResult = httpError(404, { title: 'Not Found' });
        await store.setDone('a', true);
        expect(store.error()).toBe('Задачата вече я няма');
    });

    it('remove drops the task; a failure keeps it', async () => {
        await store.load({ siteId: 's1' });

        expect(await store.remove('a')).toBe(true);
        expect(ids()).toEqual(['b', 'c']);

        fake.removeResult = httpError(403);
        expect(await store.remove('b')).toBe(false);
        expect(ids()).toEqual(['b', 'c']);
        expect(store.error()).toBe('Грешка при записа');
    });

    it('clearErrors clears the messages', async () => {
        fake.createResult = httpError(400, { errors: { Text: ['Въведи текст'] } });
        await store.create({ text: '', siteId: null, assigneeId: null });

        store.clearErrors();

        expect(store.fieldError()).toBeNull();
        expect(store.error()).toBeNull();
    });
});
