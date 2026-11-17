import { ChangeDetectionStrategy, Component, effect, inject, input, untracked } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { LedgerComponent } from '../../shared/ledger/ledger.component';
import { SiteHeaderComponent } from '../../sites/site-header/site-header.component';
import { WorkDayListComponent } from '../../work-days/work-day-list/work-day-list.component';
import { PortalStore } from '../+state/portal.store';
import { PortalPaymentListComponent } from '../portal-payment-list/portal-payment-list.component';

/**
 * Smart page of one site in the client portal (/portal/:id): header, money strip, work days and payments.
 * Read only: no edit buttons, and the portal data has no people, notes or contacts to show.
 */
@Component({
    selector: 'atlas-portal-site-page',
    imports: [
        RouterLink,
        NzAlertModule,
        NzButtonModule,
        NzIconModule,
        LedgerComponent,
        SiteHeaderComponent,
        WorkDayListComponent,
        PortalPaymentListComponent,
    ],
    templateUrl: './portal-site-page.component.html',
    styleUrl: './portal-site-page.component.scss',
    // Own store per page (not root): signing out destroys the page and with it the client's data.
    providers: [PortalStore],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PortalSitePageComponent {
    /** Route parameter (withComponentInputBinding). */
    readonly id = input.required<string>();

    protected readonly store = inject(PortalStore);

    constructor() {
        effect(() => {
            const id = this.id();
            untracked(() => void this.store.loadSite(id));
        });
    }

    protected openLabel(count: number): string {
        return `${count} за уточняване`;
    }
}
