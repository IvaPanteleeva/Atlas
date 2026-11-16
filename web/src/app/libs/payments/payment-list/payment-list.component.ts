import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { EurPipe } from '../../shared/eur.pipe';
import { Payment } from '../payment.model';

/** "2026-11-03" -> "03.11" (plain string work, like the work day list). */
function dayLabel(value: string): string {
    const [, month, day] = value.split('-');
    return `${day}.${month}`;
}

/** Dumb list of the payments of a site, newest first as given. Each row is a button that emits it for editing. */
@Component({
    selector: 'atlas-payment-list',
    imports: [EurPipe],
    templateUrl: './payment-list.component.html',
    styleUrl: './payment-list.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaymentListComponent {
    readonly payments = input.required<Payment[]>();
    readonly edit = output<Payment>();

    protected readonly rows = computed(() =>
        this.payments().map((payment) => ({ payment, day: dayLabel(payment.date) })),
    );
}
