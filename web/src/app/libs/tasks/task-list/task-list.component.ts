import { NgTemplateOutlet } from '@angular/common';
import {
    afterNextRender,
    ChangeDetectionStrategy,
    Component,
    computed,
    ElementRef,
    Injector,
    inject,
    input,
    output,
    signal,
    viewChild,
} from '@angular/core';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCheckboxModule } from 'ng-zorro-antd/checkbox';
import { NzDropdownModule } from 'ng-zorro-antd/dropdown';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzMenuModule } from 'ng-zorro-antd/menu';
import { Person } from '../../people/person.model';
import { DONE_SHOWN, SiteTask, TASK_TEXT_MAX } from '../task.model';

export interface TaskToggle {
    task: SiteTask;
    done: boolean;
}

export interface TaskRename {
    task: SiteTask;
    text: string;
}

export interface TaskAssignment {
    task: SiteTask;
    assigneeId: string | null;
}

interface AssigneeOption {
    id: string;
    name: string;
}

/**
 * Dumb list of tasks: the open ones, the projected add row (`<ng-content>`), then the last 5 done ones
 * (crossed out) and „Покажи всички завършени (N)“. Checkbox, text edited in place (Enter saves, Esc cancels),
 * person label with a small menu, and a two-step ✕ (Owner).
 */
@Component({
    selector: 'atlas-task-list',
    imports: [NgTemplateOutlet, NzButtonModule, NzCheckboxModule, NzDropdownModule, NzIconModule, NzMenuModule],
    templateUrl: './task-list.component.html',
    styleUrl: './task-list.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskListComponent {
    /** In the API order: open ones (oldest first), then done ones (last done first). */
    readonly tasks = input.required<SiteTask[]>();
    /** Crew people (`isCrew`) offered in the person menu. */
    readonly crew = input<Person[]>([]);
    /** Owner only. */
    readonly canDelete = input(false);

    readonly toggle = output<TaskToggle>();
    readonly rename = output<TaskRename>();
    readonly assign = output<TaskAssignment>();
    readonly remove = output<SiteTask>();

    protected readonly maxLength = TASK_TEXT_MAX;

    protected readonly open = computed(() => this.tasks().filter((task) => !task.done));
    protected readonly done = computed(() => this.tasks().filter((task) => task.done));
    protected readonly showAllDone = signal(false);
    protected readonly shownDone = computed(() =>
        this.showAllDone() ? this.done() : this.done().slice(0, DONE_SHOWN),
    );

    /** The task whose text is a field. */
    protected readonly editingId = signal<string | null>(null);
    protected readonly draft = signal('');
    /** The task whose ✕ was tapped once. */
    protected readonly confirmingId = signal<string | null>(null);

    private readonly editInput = viewChild<ElementRef<HTMLInputElement>>('editInput');
    private readonly injector = inject(Injector);

    /** The crew, plus the current person when they are no longer in it. */
    protected options(task: SiteTask): AssigneeOption[] {
        const crew: AssigneeOption[] = this.crew().map(({ id, name }) => ({ id, name }));
        if (task.assigneeId && !crew.some((person) => person.id === task.assigneeId)) {
            crew.push({ id: task.assigneeId, name: task.assigneeName ?? '—' });
        }
        return crew;
    }

    protected startEdit(task: SiteTask): void {
        this.confirmingId.set(null);
        this.draft.set(task.text);
        this.editingId.set(task.id);
        afterNextRender(() => this.editInput()?.nativeElement.focus(), { injector: this.injector });
    }

    /** Enter (and leaving the field): saves a changed, non-empty text. */
    protected finishEdit(task: SiteTask): void {
        if (this.editingId() !== task.id) {
            return;
        }
        this.editingId.set(null);
        const text = this.draft().trim();
        if (text && text !== task.text) {
            this.rename.emit({ task, text });
        }
    }

    protected cancelEdit(): void {
        this.editingId.set(null);
    }

    protected pick(task: SiteTask, assigneeId: string | null): void {
        if (assigneeId !== task.assigneeId) {
            this.assign.emit({ task, assigneeId });
        }
    }

    protected requestRemove(task: SiteTask): void {
        if (this.confirmingId() === task.id) {
            this.confirmingId.set(null);
            this.remove.emit(task);
        } else {
            this.confirmingId.set(task.id);
        }
    }
}
