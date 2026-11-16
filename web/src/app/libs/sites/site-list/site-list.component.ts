import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { joinMeta, SiteListItem } from '../site.model';
import { SiteStatusTagComponent } from '../site-status-tag/site-status-tag.component';

/** Dumb list of sites: each row is a full-width button that emits the site id. */
@Component({
    selector: 'atlas-site-list',
    imports: [SiteStatusTagComponent],
    templateUrl: './site-list.component.html',
    styleUrl: './site-list.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SiteListComponent {
    readonly sites = input.required<SiteListItem[]>();
    readonly open = output<string>();

    /** The company if there is one, otherwise the end customer; then the address. */
    protected meta(site: SiteListItem): string {
        return joinMeta(site.companyName ?? site.endCustomer, site.address);
    }
}
