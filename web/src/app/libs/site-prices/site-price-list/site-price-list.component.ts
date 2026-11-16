import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { EurPipe } from '../../shared/eur.pipe';
import { UNIT_LABELS } from '../../shared/units';
import { SitePrice } from '../site-price.model';

/** Dumb list of the agreed prices of a site. Rows are buttons only when canEdit (Owner). */
@Component({
    selector: 'atlas-site-price-list',
    imports: [EurPipe],
    templateUrl: './site-price-list.component.html',
    styleUrl: './site-price-list.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SitePriceListComponent {
    readonly prices = input.required<SitePrice[]>();
    readonly canEdit = input(false);
    readonly open = output<SitePrice>();

    protected readonly unitLabels = UNIT_LABELS;
}
