import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { AuthStore } from '../../auth/+state/auth.store';
import { PeopleStore } from '../../people/+state/people.store';
import { SitesStore } from '../../sites/+state/sites.store';
import { TasksStore } from '../+state/tasks.store';
import { groupOpenBySite } from '../task-groups';
import { NewTask, SiteTask, TaskQuery } from '../task.model';
import { TaskAddComponent } from '../task-add/task-add.component';
import { TaskAssignment, TaskListComponent, TaskRename, TaskToggle } from '../task-list/task-list.component';

/** „Всички“, „Мои“ or one crew person (by id). */
type TaskFilter = { kind: 'all' } | { kind: 'mine' } | { kind: 'person'; personId: string };

/**
 * Smart page „Задачи“: filter chips, „Фирмата“ (tasks without a site, with the add row and the done ones), then
 * the open tasks of each site in the order of the sites list. Owner and Manager write; only Owner deletes.
 */
@Component({
    selector: 'atlas-tasks-page',
    imports: [RouterLink, NzAlertModule, TaskListComponent, TaskAddComponent],
    templateUrl: './tasks-page.component.html',
    styleUrl: './tasks-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TasksPageComponent {
    protected readonly store = inject(TasksStore);
    protected readonly people = inject(PeopleStore);
    protected readonly sites = inject(SitesStore);
    protected readonly auth = inject(AuthStore);

    protected readonly filter = signal<TaskFilter>({ kind: 'all' });
    /** Changed after each added task: the add row clears its field. */
    protected readonly added = signal(0);

    /** The person linked to my profile; null = no „Мои“. */
    protected readonly myPersonId = computed(() => this.auth.user()?.personId ?? null);
    /** Crew chips; my own person is „Мои“. */
    protected readonly crewChips = computed(() =>
        this.people.crew().filter((person) => person.id !== this.myPersonId()),
    );
    /** The person of the filter (preselected in „Кой?“); null for „Всички“. */
    protected readonly filterPersonId = computed(() => {
        const filter = this.filter();
        if (filter.kind === 'person') {
            return filter.personId;
        }
        return filter.kind === 'mine' ? this.myPersonId() : null;
    });

    protected readonly companyTasks = computed(() => this.store.tasks().filter((task) => task.siteId === null));
    protected readonly siteGroups = computed(() => groupOpenBySite(this.store.tasks(), this.sites.list()));

    constructor() {
        void this.people.load();
        void this.sites.load();
        effect(() => {
            const assigneeId = this.filterPersonId();
            const query: TaskQuery = assigneeId ? { assigneeId } : {};
            untracked(() => void this.store.load(query));
        });
    }

    protected isPerson(personId: string): boolean {
        const filter = this.filter();
        return filter.kind === 'person' && filter.personId === personId;
    }

    protected async addCompanyTask(task: NewTask): Promise<void> {
        if (await this.store.create({ ...task, siteId: null })) {
            this.added.update((count) => count + 1);
        }
    }

    protected toggle({ task, done }: TaskToggle): void {
        void this.store.setDone(task.id, done);
    }

    protected rename({ task, text }: TaskRename): void {
        void this.store.update(task.id, { text, assigneeId: task.assigneeId });
    }

    protected assign({ task, assigneeId }: TaskAssignment): void {
        void this.store.update(task.id, { text: task.text, assigneeId });
    }

    protected remove(task: SiteTask): void {
        void this.store.remove(task.id);
    }
}
