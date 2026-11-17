import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { EurPipe } from '../../shared/eur.pipe';
import { PortalPayment } from '../portal.model';

/** "2026-11-03" -> "03.11" (same as the internal payment list). */
function dayLabel(value: string): string {
    const [, month, day] = value.split('-');
    return `${day}.${month}`;
}

/** Dumb read-only list of the payments of a site in the portal: date and amount, in the server's order. */
@Component({
    selector: 'atlas-portal-payment-list',
    imports: [EurPipe],
    templateUrl: './portal-payment-list.component.html',
    styleUrl: './portal-payment-list.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PortalPaymentListComponent {
    readonly payments = input.required<PortalPayment[]>();

    protected readonly rows = computed(() =>
        this.payments().map((payment) => ({ payment, day: dayLabel(payment.date) })),
    );
}
