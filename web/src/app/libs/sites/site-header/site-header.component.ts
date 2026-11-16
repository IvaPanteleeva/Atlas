import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { joinMeta, Site } from '../site.model';
import { SiteStatusTagComponent } from '../site-status-tag/site-status-tag.component';

/** Dumb header of the site detail: name, status, company / end customer / address, map link, note. */
@Component({
    selector: 'atlas-site-header',
    imports: [SiteStatusTagComponent],
    templateUrl: './site-header.component.html',
    styleUrl: './site-header.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SiteHeaderComponent {
    readonly site = input.required<Site>();

    protected readonly meta = computed(() => {
        const site = this.site();
        return joinMeta(site.companyName, site.endCustomer, site.address);
    });
}
