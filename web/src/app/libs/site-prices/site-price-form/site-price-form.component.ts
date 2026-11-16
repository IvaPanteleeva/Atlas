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
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputNumberModule } from 'ng-zorro-antd/input-number';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { MONEY_MAX, MONEY_MIN, Service } from '../../services/service.model';
import { FieldErrors } from '../../shared/api-errors';
import { DecimalKeyboardDirective, parseDecimal } from '../../shared/money-input';
import { UNIT_LABELS } from '../../shared/units';
import { CreateSitePriceRequest, SitePrice } from '../site-price.model';

/** Fields that have a place in the form; server errors for other keys go to the alert. */
const FORM_FIELDS = ['serviceId', 'rate'];

/**
 * Dumb form for a site price. Creating (price null): pick one of the services that has no price on this site yet.
 * Editing: the service is fixed, only the rate changes. Emits { serviceId, rate } either way.
 */
@Component({
    selector: 'atlas-site-price-form',
    imports: [
        ReactiveFormsModule,
        RouterLink,
        NzAlertModule,
        NzButtonModule,
        NzFormModule,
        NzInputNumberModule,
        NzSelectModule,
        DecimalKeyboardDirective,
    ],
    templateUrl: './site-price-form.component.html',
    styleUrl: './site-price-form.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SitePriceFormComponent {
    readonly price = input<SitePrice | null>(null);
    /** All services (GET /api/services). */
    readonly services = input<Service[]>([]);
    /** The prices the site already has; their services are not offered again. */
    readonly prices = input<SitePrice[]>([]);
    readonly saving = input(false);
    readonly error = input<string | null>(null);
    readonly fieldErrors = input<FieldErrors>({});

    readonly save = output<CreateSitePriceRequest>();
    readonly delete = output<void>();
    readonly cancel = output<void>();

    protected readonly unitLabels = UNIT_LABELS;
    protected readonly moneyMin = MONEY_MIN;
    protected readonly moneyMax = MONEY_MAX;
    protected readonly parseDecimal = parseDecimal;

    protected readonly form = inject(NonNullableFormBuilder).group({
        serviceId: ['', Validators.required],
        rate: [null as number | null, [Validators.required, Validators.min(MONEY_MIN), Validators.max(MONEY_MAX)]],
    });

    protected readonly confirmingDelete = signal(false);

    /** Services without a price on this site, in the API order (by name). */
    protected readonly freeServices = computed(() => {
        const taken = new Set(this.prices().map((price) => price.serviceId));
        return this.services().filter((service) => !taken.has(service.id));
    });

    /** Creating, but nothing left to choose: the form shows a message instead of the fields. */
    protected readonly nothingToAdd = computed(() => !this.price() && !this.freeServices().length);

    protected readonly otherErrors = computed(() =>
        Object.entries(this.fieldErrors())
            .filter(([key]) => !FORM_FIELDS.includes(key))
            .map(([, message]) => message),
    );

    /** The service whose default price was last applied. */
    private lastServiceId = '';

    constructor() {
        effect(() => {
            const price = this.price();
            untracked(() => this.reset(price));
        });

        // Picking a service prefills its default price, if it has one.
        this.form.controls.serviceId.valueChanges.pipe(takeUntilDestroyed()).subscribe((serviceId) => {
            // Only a real change; submit re-validates with events and must not overwrite a typed price.
            if (serviceId === this.lastServiceId) {
                return;
            }
            this.lastServiceId = serviceId;
            const defaultRate = this.services().find((service) => service.id === serviceId)?.defaultRate;
            if (defaultRate !== null && defaultRate !== undefined) {
                this.form.controls.rate.setValue(defaultRate);
            }
        });
    }

    protected serviceLabel(name: string, unit: Service['unit']): string {
        return `${name} · ${UNIT_LABELS[unit]}`;
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
        if (this.saving() || this.nothingToAdd()) {
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
        this.save.emit({ serviceId: value.serviceId, rate: value.rate! });
    }

    private reset(price: SitePrice | null): void {
        this.lastServiceId = price?.serviceId ?? '';
        this.form.reset({ serviceId: price?.serviceId ?? '', rate: price?.rate ?? null }, { emitEvent: false });
        this.confirmingDelete.set(false);
    }
}
