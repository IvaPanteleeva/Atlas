import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { AuthStore } from '../../auth/+state/auth.store';
import { CompaniesStore } from '../../companies/+state/companies.store';
import { UsersStore } from '../../users/+state/users.store';
import { PasswordFormComponent } from '../../users/password-form/password-form.component';
import { CreateUserRequest, UserAccount } from '../../users/user.model';
import { UserFormComponent } from '../../users/user-form/user-form.component';
import { UserListComponent } from '../../users/user-list/user-list.component';
import { PeopleStore } from '../+state/people.store';
import { ContactListComponent } from '../contact-list/contact-list.component';
import { PersonCardComponent } from '../person-card/person-card.component';
import { Person, SavePersonRequest } from '../person.model';
import { PersonFormComponent } from '../person-form/person-form.component';

/**
 * Smart page „Хора“: crew cards, contacts and (Owner only) the login profiles.
 * Owner and Manager add and edit (only Owner deletes). Profiles are Owner only: Manager never requests /api/users.
 */
@Component({
    selector: 'atlas-people-page',
    imports: [
        NzAlertModule,
        NzButtonModule,
        NzIconModule,
        NzModalModule,
        PersonCardComponent,
        ContactListComponent,
        PersonFormComponent,
        UserListComponent,
        UserFormComponent,
        PasswordFormComponent,
    ],
    templateUrl: './people-page.component.html',
    styleUrl: './people-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PeoplePageComponent {
    protected readonly store = inject(PeopleStore);
    protected readonly users = inject(UsersStore);
    protected readonly companies = inject(CompaniesStore);
    protected readonly auth = inject(AuthStore);

    protected readonly personFormOpen = signal(false);
    /** The person being edited; null = a new one. */
    protected readonly editingPerson = signal<Person | null>(null);
    /** „Работи с нас“ for a new person: from „+ Човек в екипа“ (true) or „+ Контакт“ (false). */
    protected readonly newIsCrew = signal(true);

    protected readonly userFormOpen = signal(false);
    /** The profile getting a new password; null = that sheet is closed. */
    protected readonly passwordUser = signal<UserAccount | null>(null);
    protected readonly notice = signal<string | null>(null);

    constructor() {
        void this.store.load();
        if (this.auth.isOwner()) {
            void this.users.load();
        }
    }

    protected openPersonForm(person: Person | null, isCrew = true): void {
        if (!this.auth.isStaff()) {
            return;
        }
        this.store.clearErrors();
        this.editingPerson.set(person);
        this.newIsCrew.set(isCrew);
        this.personFormOpen.set(true);
    }

    protected closePersonForm(): void {
        this.personFormOpen.set(false);
    }

    protected async savePerson(request: SavePersonRequest): Promise<void> {
        const editing = this.editingPerson();
        const saved = editing ? await this.store.update(editing.id, request) : await this.store.create(request);
        if (saved) {
            this.personFormOpen.set(false);
        }
    }

    protected async removePerson(): Promise<void> {
        const editing = this.editingPerson();
        if (editing && (await this.store.remove(editing.id))) {
            this.personFormOpen.set(false);
        }
    }

    protected openUserForm(): void {
        this.users.clearErrors();
        this.notice.set(null);
        void this.companies.load();
        this.userFormOpen.set(true);
    }

    protected closeUserForm(): void {
        this.userFormOpen.set(false);
    }

    protected async createUser(request: CreateUserRequest): Promise<void> {
        if (await this.users.create(request)) {
            this.userFormOpen.set(false);
        }
    }

    protected openPasswordForm(user: UserAccount): void {
        this.users.clearErrors();
        this.notice.set(null);
        this.passwordUser.set(user);
    }

    protected closePasswordForm(): void {
        this.passwordUser.set(null);
    }

    protected async setPassword(password: string): Promise<void> {
        const user = this.passwordUser();
        if (user && (await this.users.setPassword(user.id, password))) {
            this.passwordUser.set(null);
            this.notice.set(`Паролата на ${user.userName} е сменена.`);
        }
    }

    protected async removeUser(user: UserAccount): Promise<void> {
        this.notice.set(null);
        await this.users.remove(user.id);
    }
}
