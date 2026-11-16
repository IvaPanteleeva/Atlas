import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { PhoneActionsComponent } from '../../shared/phone-actions/phone-actions.component';
import { Person } from '../person.model';

/** Dumb list of contacts (people who are not crew): name, phone with „Копирай“; note and „Редакция“ for Owner. */
@Component({
    selector: 'atlas-contact-list',
    imports: [NzButtonModule, PhoneActionsComponent],
    templateUrl: './contact-list.component.html',
    styleUrl: './contact-list.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ContactListComponent {
    readonly people = input.required<Person[]>();
    readonly showNotes = input(false);
    readonly canEdit = input(false);
    readonly edit = output<Person>();
}
