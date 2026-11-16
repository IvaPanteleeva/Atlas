import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { PhoneActionsComponent } from '../../shared/phone-actions/phone-actions.component';
import { SiteContact } from '../site-contact.model';

/** Dumb list of the contacts of a site: name, role, phone (call / copy), note and „Редакция“. */
@Component({
    selector: 'atlas-site-contact-list',
    imports: [NzButtonModule, PhoneActionsComponent],
    templateUrl: './site-contact-list.component.html',
    styleUrl: './site-contact-list.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SiteContactListComponent {
    readonly contacts = input.required<SiteContact[]>();
    readonly canEdit = input(true);
    readonly edit = output<SiteContact>();
}
