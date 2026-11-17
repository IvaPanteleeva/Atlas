import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { EurPipe } from '../eur.pipe';

/**
 * Dumb second money row „Разход за хора | Остава за нас“ under the ledger (same look, two columns).
 * The amounts come ready from the server; „Остава за нас“ is green above 0, red below 0, neutral at 0.
 */
@Component({
    selector: 'atlas-cost-ledger',
    imports: [EurPipe],
    templateUrl: './cost-ledger.component.html',
    styleUrl: './ledger.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CostLedgerComponent {
    /** Σ pay of the people. */
    readonly workerCost = input.required<number>();
    /** Earned − worker cost. */
    readonly net = input.required<number>();

    protected readonly long = computed(() =>
        [this.workerCost(), this.net()].some((value) => Math.abs(value) >= 10_000),
    );
}
