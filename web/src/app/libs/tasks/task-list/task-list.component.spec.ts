import { Component, signal } from '@angular/core';
import { ANIMATION_MODULE_TYPE } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { Person } from '../../people/person.model';
import { testPerson } from '../../people/testing/fake-people.service';
import { APP_ICONS } from '../../shared/icons';
import { SiteTask } from '../task.model';
import { testTask } from '../testing/fake-tasks.service';
import { TaskAssignment, TaskListComponent, TaskRename, TaskToggle } from './task-list.component';

@Component({
    imports: [TaskListComponent],
    template: `
        <atlas-task-list
            [tasks]="tasks()"
            [crew]="crew()"
            [canDelete]="canDelete()"
            (toggle)="toggled.push($event)"
            (rename)="renamed.push($event)"
            (assign)="assigned.push($event)"
            (remove)="removed.push($event)"
        >
            <p class="projected">add row</p>
        </atlas-task-list>
    `,
})
class HostComponent {
    readonly tasks = signal<SiteTask[]>([]);
    readonly crew = signal<Person[]>([]);
    readonly canDelete = signal(true);
    toggled: TaskToggle[] = [];
    renamed: TaskRename[] = [];
    assigned: TaskAssignment[] = [];
    removed: SiteTask[] = [];
}

describe('TaskListComponent', () => {
    let fixture: ComponentFixture<HostComponent>;
    let host: HostComponent;
    let element: HTMLElement;

    const text = (node: Element | null | undefined) => node?.textContent?.replace(/\s+/g, ' ').trim();
    const rows = () => Array.from(element.querySelectorAll<HTMLElement>('li.task'));
    const rowTexts = () => rows().map((row) => text(row.querySelector('.text')));
    const button = (root: ParentNode, label: string) =>
        Array.from(root.querySelectorAll<HTMLButtonElement>('button')).find(
            (candidate) => text(candidate) === label || candidate.getAttribute('aria-label') === label,
        );

    const pesho = testPerson({ id: 'p1', name: 'Пешо' });
    const gosho = testPerson({ id: 'p2', name: 'Гошо' });

    async function render(tasks: SiteTask[], canDelete = true) {
        host.tasks.set(tasks);
        host.canDelete.set(canDelete);
        await fixture.whenStable();
    }

    function doneTasks(count: number): SiteTask[] {
        return Array.from({ length: count }, (_, index) =>
            testTask({ id: `d${index}`, text: `Готова ${index}`, done: true, doneAt: '2026-11-15T08:00:00Z' }),
        );
    }

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [provideNzIcons(APP_ICONS), { provide: ANIMATION_MODULE_TYPE, useValue: 'NoopAnimations' }],
        });
        fixture = TestBed.createComponent(HostComponent);
        host = fixture.componentInstance;
        host.crew.set([gosho, pesho]);
        element = fixture.nativeElement as HTMLElement;
    });

    afterEach(() => {
        document.querySelectorAll('.cdk-overlay-container').forEach((node) => (node.innerHTML = ''));
    });

    it('shows the empty state and still shows the add row', async () => {
        await render([]);

        expect(text(element.querySelector('.empty'))).toBe('Няма задачи.');
        expect(element.querySelector('.projected')).toBeTruthy();
    });

    it('shows the open tasks, then the add row, then the done ones (crossed out)', async () => {
        await render([testTask({ id: 'a', text: 'Лепило' }), testTask({ id: 'b', text: 'Кофи', done: true })]);

        expect(rowTexts()).toEqual(['Лепило', 'Кофи']);
        const order = Array.from(element.querySelectorAll('li.task, .projected')).map((node) => text(node));
        expect(order[1]).toBe('add row');
        expect(rows()[0].classList.contains('done')).toBe(false);
        expect(rows()[1].classList.contains('done')).toBe(true);
        expect(element.querySelector('.empty')).toBeNull();
    });

    it('shows the last 5 done tasks and „Покажи всички завършени (N)“', async () => {
        await render([testTask({ id: 'a' }), ...doneTasks(7)]);

        expect(rows()).toHaveLength(6);
        const more = button(element, 'Покажи всички завършени (7)')!;
        expect(more).toBeTruthy();

        more.click();
        await fixture.whenStable();

        expect(rows()).toHaveLength(8);
        expect(button(element, 'Покажи всички завършени (7)')).toBeUndefined();
    });

    it('5 done tasks need no „Покажи всички“', async () => {
        await render(doneTasks(5));

        expect(rows()).toHaveLength(5);
        expect(element.querySelector('button.more')).toBeNull();
    });

    it('the checkbox emits the new state', async () => {
        const open = testTask({ id: 'a' });
        const done = testTask({ id: 'b', done: true });
        await render([open, done]);

        rows()[0].querySelector<HTMLElement>('[nz-checkbox]')!.click();
        rows()[1].querySelector<HTMLElement>('[nz-checkbox]')!.click();

        expect(host.toggled).toEqual([
            { task: open, done: true },
            { task: done, done: false },
        ]);
    });

    it('tapping the text makes it a field: Enter saves the trimmed text, Esc cancels', async () => {
        const task = testTask({ id: 'a', text: 'Лепило' });
        await render([task]);

        rows()[0].querySelector<HTMLButtonElement>('button.text')!.click();
        await fixture.whenStable();
        const input = rows()[0].querySelector<HTMLInputElement>('input.edit')!;
        expect(input.value).toBe('Лепило');
        expect(input.maxLength).toBe(500);

        input.value = '  Лепило за плочки ';
        input.dispatchEvent(new Event('input'));
        input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
        await fixture.whenStable();

        expect(host.renamed).toEqual([{ task, text: 'Лепило за плочки' }]);
        expect(rows()[0].querySelector('input.edit')).toBeNull();

        rows()[0].querySelector<HTMLButtonElement>('button.text')!.click();
        await fixture.whenStable();
        const again = rows()[0].querySelector<HTMLInputElement>('input.edit')!;
        again.value = 'Друго';
        again.dispatchEvent(new Event('input'));
        again.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
        await fixture.whenStable();

        expect(host.renamed).toHaveLength(1);
        expect(rows()[0].querySelector('input.edit')).toBeNull();
    });

    it('an empty or unchanged text saves nothing', async () => {
        await render([testTask({ id: 'a', text: 'Лепило' })]);

        for (const value of ['   ', 'Лепило']) {
            rows()[0].querySelector<HTMLButtonElement>('button.text')!.click();
            await fixture.whenStable();
            const input = rows()[0].querySelector<HTMLInputElement>('input.edit')!;
            input.value = value;
            input.dispatchEvent(new Event('input'));
            input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
            await fixture.whenStable();
        }

        expect(host.renamed).toEqual([]);
    });

    it('shows the person; the menu offers the crew, the current person and „Без отговорник“', async () => {
        const task = testTask({ id: 'a', assigneeId: 'p9', assigneeName: 'Стария' });
        await render([task]);

        const label = rows()[0].querySelector<HTMLButtonElement>('button.assignee')!;
        expect(text(label)).toBe('Стария');
        label.click();
        const items = await vi.waitFor(() => {
            const found = Array.from(document.querySelectorAll<HTMLElement>('[nz-menu-item]'));
            expect(found.length).toBeGreaterThan(0);
            return found;
        });

        expect(items.map((item) => text(item))).toEqual(['Гошо', 'Пешо', 'Стария', 'Без отговорник']);

        items[1].click();
        items[3].click();
        expect(host.assigned).toEqual([
            { task, assigneeId: 'p1' },
            { task, assigneeId: null },
        ]);
    });

    it('a task without a person has a muted „Кой?“; done ones without a person have nothing', async () => {
        await render([testTask({ id: 'a' }), testTask({ id: 'b', done: true })]);

        expect(text(rows()[0].querySelector('button.assignee'))).toBe('Кой?');
        expect(rows()[0].querySelector('button.assignee')?.classList.contains('none')).toBe(true);
        expect(rows()[1].querySelector('button.assignee')).toBeNull();
    });

    it('✕ asks once more, then emits; hidden without canDelete', async () => {
        const task = testTask({ id: 'a' });
        await render([task]);

        button(rows()[0], 'Изтрий задачата')!.click();
        await fixture.whenStable();
        expect(host.removed).toEqual([]);

        button(rows()[0], 'Потвърди изтриване')!.click();
        expect(host.removed).toEqual([task]);

        await render([task], false);
        expect(button(rows()[0], 'Изтрий задачата')).toBeUndefined();
    });
});
