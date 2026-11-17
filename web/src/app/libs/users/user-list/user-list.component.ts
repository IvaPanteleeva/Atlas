import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { ROLE_LABELS, UserAccount } from '../user.model';

/**
 * Dumb list of login profiles: name, role, company, linked person; „Свържи с човек“ / „Смени човека“ (not for the
 * client), „Нова парола“ and a two-step „Изтрий“ (not for yourself).
 */
@Component({
    selector: 'atlas-user-list',
    imports: [NzButtonModule],
    templateUrl: './user-list.component.html',
    styleUrl: './user-list.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserListComponent {
    readonly users = input.required<UserAccount[]>();
    /** The signed-in user: marked „(ти)“ and cannot be deleted. */
    readonly currentUserId = input<string | null>(null);
    readonly saving = input(false);

    readonly newPassword = output<UserAccount>();
    readonly linkPerson = output<UserAccount>();
    readonly remove = output<UserAccount>();

    protected readonly roleLabels = ROLE_LABELS;
    /** The row whose „Изтрий“ was clicked once. */
    protected readonly confirmingId = signal<string | null>(null);

    protected requestRemove(user: UserAccount): void {
        if (this.confirmingId() === user.id) {
            this.remove.emit(user);
        } else {
            this.confirmingId.set(user.id);
        }
    }
}
