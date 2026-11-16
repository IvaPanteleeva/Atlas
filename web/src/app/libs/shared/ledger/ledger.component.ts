import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { EurPipe } from '../eur.pipe';

/**
 * Dumb money strip „Изработено | Платено | Остатък“ (prototype .ledger). The amounts come ready from the server.
 * The third column is red while something is owed and green when it is paid; a negative remaining amount reads
 * „Надплатено“ with the amount without the sign.
 */
@Component({
    selector: 'atlas-ledger',
    imports: [EurPipe],
    templateUrl: './ledger.component.html',
    styleUrl: './ledger.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LedgerComponent {
    readonly earned = input.required<number>();
    readonly paid = input.required<number>();
    readonly remaining = input.required<number>();
    /** „Остатък“ for one site, „Дължат ни“ for all sites. */
    readonly lastLabel = input<'Остатък' | 'Дължат ни'>('Остатък');

    protected readonly overpaid = computed(() => this.remaining() < 0);
    protected readonly owed = computed(() => this.remaining() > 0);
    protected readonly restLabel = computed(() => (this.overpaid() ? 'Надплатено' : this.lastLabel()));
    /** Only the sign is dropped for display; the amount itself is the server's. */
    protected readonly restAmount = computed(() => Math.abs(this.remaining()));
    /** Five or more digits before the comma: on the phone the digits get smaller so "13 557,89 €" fits. */
    protected readonly long = computed(() =>
        [this.earned(), this.paid(), this.remaining()].some((value) => Math.abs(value) >= 10_000),
    );
}
