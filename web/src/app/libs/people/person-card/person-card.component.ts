import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { EurPipe } from '../../shared/eur.pipe';
import { PhoneActionsComponent } from '../../shared/phone-actions/phone-actions.component';
import { Person } from '../person.model';

const eur = new EurPipe();

/**
 * „12 дни · изкарал 1 440,00 € · последно 16.11“ once there is pay; before that „12 дни на обекти · последно 16.11“,
 * „1 ден …“ or „Няма записани дни“. The numbers come from the server.
 */
export function daysLabel(person: Pick<Person, 'dayCount' | 'lastWorkDate' | 'earned'>): string {
    if (!person.dayCount) {
        return 'Няма записани дни';
    }
    const count = `${person.dayCount} ${person.dayCount === 1 ? 'ден' : 'дни'}`;
    const days = person.earned > 0 ? `${count} · изкарал ${eur.transform(person.earned)}` : `${count} на обекти`;
    if (!person.lastWorkDate) {
        return days;
    }
    const [, month, day] = person.lastWorkDate.split('-');
    return `${days} · последно ${day}.${month}`;
}

/** Dumb card of a crew member: name, phone (call / copy), days on sites; note and „Редакция“ for Owner. */
@Component({
    selector: 'atlas-person-card',
    imports: [NzButtonModule, PhoneActionsComponent],
    templateUrl: './person-card.component.html',
    styleUrl: './person-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PersonCardComponent {
    readonly person = input.required<Person>();
    /** The internal note (Owner and Manager). */
    readonly showNote = input(false);
    readonly canEdit = input(false);
    readonly edit = output<Person>();

    protected readonly days = computed(() => daysLabel(this.person()));
}
