import { ChangeDetectionStrategy, Component, effect, inject, input, output, untracked } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { Person } from '../../people/person.model';
import { NewTask, TASK_TEXT_MAX } from '../task.model';

/** Dumb add row: „Нова задача…“, an optional „Кой?“ (crew) and „Добави“ / Enter. No modal. */
@Component({
    selector: 'atlas-task-add',
    imports: [ReactiveFormsModule, NzButtonModule, NzSelectModule],
    templateUrl: './task-add.component.html',
    styleUrl: './task-add.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskAddComponent {
    /** Crew people (`isCrew`). */
    readonly crew = input<Person[]>([]);
    /** Preselected person (e.g. the filter of „Задачи“); null = nobody. */
    readonly defaultAssigneeId = input<string | null>(null);
    readonly saving = input(false);
    /** Server message of the last add (text, person or site). */
    readonly error = input<string | null>(null);
    /** The page changes it after a task is added: the field is cleared. */
    readonly resetKey = input(0);

    readonly add = output<NewTask>();

    protected readonly maxLength = TASK_TEXT_MAX;

    protected readonly form = inject(NonNullableFormBuilder).group({
        text: [''],
        assigneeId: [null as string | null],
    });

    constructor() {
        effect(() => {
            this.resetKey();
            const assigneeId = this.defaultAssigneeId();
            untracked(() => this.form.reset({ text: '', assigneeId }));
        });
    }

    protected submit(): void {
        const { text, assigneeId } = this.form.getRawValue();
        if (this.saving() || !text.trim()) {
            return;
        }
        this.add.emit({ text: text.trim(), assigneeId: assigneeId ?? null });
    }
}
