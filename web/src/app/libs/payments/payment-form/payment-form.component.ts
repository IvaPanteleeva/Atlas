import {
    ChangeDetectionStrategy,
    Component,
    computed,
    effect,
    inject,
    input,
    output,
    signal,
    untracked,
} from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzDatePickerModule } from 'ng-zorro-antd/date-picker';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { FieldErrors } from '../../shared/api-errors';
import { DATE_PICKER_PROVIDERS } from '../../shared/date-adapter';
import { DecimalInputDirective } from '../../shared/decimal-input';
import { fromIsoDate, isAfterToday, toIsoDate } from '../../shared/iso-date';
import { PAYMENT_LIMITS, Payment, SavePaymentRequest } from '../payment.model';

/** Server error keys that have a place in the form; the rest go to the alert. */
const FORM_FIELDS = ['amount', 'date', 'note'];

/**
 * Dumb form for a client payment (create when payment is null, edit otherwise): amount, date (today by default,
 * no future days) and an optional note. Editing offers a two-step delete.
 */
@Component({
    selector: 'atlas-payment-form',
    imports: [
        ReactiveFormsModule,
        NzAlertModule,
        NzButtonModule,
        NzDatePickerModule,
        NzFormModule,
        NzInputModule,
        DecimalInputDirective,
    ],
    providers: DATE_PICKER_PROVIDERS,
    templateUrl: './payment-form.component.html',
    styleUrl: './payment-form.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaymentFormComponent {
    readonly payment = input<Payment | null>(null);
    readonly saving = input(false);
    readonly error = input<string | null>(null);
    readonly fieldErrors = input<FieldErrors>({});

    readonly save = output<SavePaymentRequest>();
    readonly delete = output<void>();
    readonly cancel = output<void>();

    protected readonly limits = PAYMENT_LIMITS;
    protected readonly disabledDate = isAfterToday;

    protected readonly form = inject(NonNullableFormBuilder).group({
        amount: [
            null as number | null,
            [Validators.required, Validators.min(PAYMENT_LIMITS.amountMin), Validators.max(PAYMENT_LIMITS.amountMax)],
        ],
        date: [null as Date | null, Validators.required],
        note: ['', Validators.maxLength(PAYMENT_LIMITS.note)],
    });

    protected readonly confirmingDelete = signal(false);

    protected readonly otherErrors = computed(() =>
        Object.entries(this.fieldErrors())
            .filter(([key]) => !FORM_FIELDS.includes(key))
            .map(([, message]) => message),
    );

    constructor() {
        effect(() => {
            const payment = this.payment();
            untracked(() => this.reset(payment));
        });
    }

    protected serverError(field: string): string | null {
        return this.fieldErrors()[field] ?? null;
    }

    protected requestDelete(): void {
        if (this.confirmingDelete()) {
            this.delete.emit();
        } else {
            this.confirmingDelete.set(true);
        }
    }

    protected submit(): void {
        if (this.saving()) {
            return;
        }
        if (this.form.invalid) {
            for (const control of Object.values(this.form.controls)) {
                control.markAsDirty();
                control.markAsTouched();
                // With events: nz-form-control shows the error tip on statusChanges, also for untouched fields.
                control.updateValueAndValidity();
            }
            return;
        }
        const value = this.form.getRawValue();
        const note = value.note.trim();
        this.save.emit({ date: toIsoDate(value.date!), amount: value.amount!, note: note ? note : null });
    }

    private reset(payment: Payment | null): void {
        this.form.reset(
            {
                amount: payment?.amount ?? null,
                date: payment ? fromIsoDate(payment.date) : new Date(),
                note: payment?.note ?? '',
            },
            { emitEvent: false },
        );
        this.confirmingDelete.set(false);
    }
}
