import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { EurPipe } from '../../shared/eur.pipe';
import { SiteSummaryRow, SummariesBySite } from '../../summaries/summary.model';
import { joinMeta, SiteListItem } from '../site.model';
import { SiteStatusTagComponent } from '../site-status-tag/site-status-tag.component';

/** What is shown under the earned amount: „остават X €“, „изплатено“ or „надплатено X €“. */
type Rest = { kind: 'owed' | 'paid' | 'overpaid'; amount: number } | null;

function restOf({ earned, remaining }: SiteSummaryRow): Rest {
    if (remaining > 0) {
        return { kind: 'owed', amount: remaining };
    }
    if (remaining < 0) {
        // Only the sign is dropped for display; the amount is the server's.
        return { kind: 'overpaid', amount: -remaining };
    }
    return earned > 0 ? { kind: 'paid', amount: 0 } : null;
}

/**
 * Dumb list of sites: each row is a full-width button that emits the site id.
 * With summaries each row also shows what was earned and what is left; without them no money.
 */
@Component({
    selector: 'atlas-site-list',
    imports: [EurPipe, SiteStatusTagComponent],
    templateUrl: './site-list.component.html',
    styleUrl: './site-list.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SiteListComponent {
    readonly sites = input.required<SiteListItem[]>();
    /** Money per site id (Owner and Manager); null = no money on the rows. */
    readonly summaries = input<SummariesBySite | null>(null);
    readonly open = output<string>();

    protected readonly rows = computed(() => {
        const summaries = this.summaries();
        return this.sites().map((site) => {
            const summary = summaries?.[site.id];
            // A site without work and payments has nothing worth showing.
            const money = summary && (summary.earned !== 0 || summary.paid !== 0) ? summary : null;
            return { site, meta: this.meta(site), money, rest: money ? restOf(money) : null };
        });
    });

    /** The company if there is one, otherwise the end customer; then the address. */
    private meta(site: SiteListItem): string {
        return joinMeta(site.companyName ?? site.endCustomer, site.address);
    }
}
