import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { EurPipe } from '../../shared/eur.pipe';
import { PortalStore } from '../+state/portal.store';
import { PortalSiteListComponent } from '../portal-site-list/portal-site-list.component';

/**
 * Smart page of the client portal (/portal): „Дължите X €“ for all the client's sites and the list of sites.
 * Read only. Only this component talks to the store.
 */
@Component({
    selector: 'atlas-portal-sites-page',
    imports: [NzAlertModule, EurPipe, PortalSiteListComponent],
    templateUrl: './portal-sites-page.component.html',
    styleUrl: './portal-sites-page.component.scss',
    // Own store per page (not root): signing out destroys the page and with it the client's data.
    providers: [PortalStore],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PortalSitesPageComponent {
    protected readonly store = inject(PortalStore);

    constructor() {
        void this.store.loadSites();
    }
}
