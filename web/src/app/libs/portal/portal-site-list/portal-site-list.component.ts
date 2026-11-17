import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EurPipe } from '../../shared/eur.pipe';
import { restOf } from '../../sites/site-list/site-list.component';
import { SiteStatusTagComponent } from '../../sites/site-status-tag/site-status-tag.component';
import { PortalSiteListItem } from '../portal.model';

/**
 * Dumb list of the client's sites (looks like the internal list): name, status, end customer, what was earned and
 * what is left, and how many items are still to be clarified. Each row links to the site in the portal.
 */
@Component({
    selector: 'atlas-portal-site-list',
    imports: [RouterLink, EurPipe, SiteStatusTagComponent],
    templateUrl: './portal-site-list.component.html',
    styleUrl: './portal-site-list.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PortalSiteListComponent {
    readonly sites = input.required<PortalSiteListItem[]>();

    protected readonly rows = computed(() =>
        this.sites().map((site) => {
            // A site without work and payments has no money worth showing.
            const hasMoney = site.earned !== 0 || site.paid !== 0;
            return { site, hasMoney, rest: hasMoney ? restOf(site) : null };
        }),
    );
}
