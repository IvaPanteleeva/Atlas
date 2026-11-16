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
import {
    AbstractControl,
    NonNullableFormBuilder,
    ReactiveFormsModule,
    ValidationErrors,
    Validators,
} from '@angular/forms';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzInputNumberModule } from 'ng-zorro-antd/input-number';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { FieldErrors } from '../../shared/api-errors';
import { DecimalKeyboardDirective, parseDecimal } from '../../shared/money-input';
import { Unit, UNIT_OPTIONS } from '../../shared/units';
import { MONEY_MAX, MONEY_MIN, SaveServiceRequest, Service, SERVICE_NAME_MAX_LENGTH } from '../service.model';

/** Fields that have a place in the form; server errors for other keys go to the alert. */
const FORM_FIELDS = ['name', 'unit', 'defaultRate'];

function notBlank(control: AbstractControl<string>): ValidationErrors | null {
    return control.value.trim() ? null : { required: true };
}

/** Dumb form for a service (create when service is null, edit otherwise). The page owns saving and the modal. */
@Component({
    selector: 'atlas-service-form',
    imports: [
        ReactiveFormsModule,
        NzAlertModule,
        NzButtonModule,
        NzFormModule,
        NzInputModule,
        NzInputNumberModule,
        NzSelectModule,
        DecimalKeyboardDirective,
    ],
    templateUrl: './service-form.component.html',
    styleUrl: './service-form.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServiceFormComponent {
    readonly service = input<Service | null>(null);
    readonly saving = input(false);
    readonly error = input<string | null>(null);
    readonly fieldErrors = input<FieldErrors>({});

    readonly save = output<SaveServiceRequest>();
    readonly delete = output<void>();
    readonly cancel = output<void>();

    protected readonly nameMaxLength = SERVICE_NAME_MAX_LENGTH;
    protected readonly units = UNIT_OPTIONS;
    protected readonly moneyMin = MONEY_MIN;
    protected readonly moneyMax = MONEY_MAX;
    protected readonly parseDecimal = parseDecimal;

    protected readonly form = inject(NonNullableFormBuilder).group({
        name: ['', [notBlank, Validators.maxLength(SERVICE_NAME_MAX_LENGTH)]],
        unit: ['SquareMeter' as Unit | null, Validators.required],
        defaultRate: [null as number | null, [Validators.min(MONEY_MIN), Validators.max(MONEY_MAX)]],
    });

    protected readonly confirmingDelete = signal(false);

    /** Server messages for keys the form has no field for. */
    protected readonly otherErrors = computed(() =>
        Object.entries(this.fieldErrors())
            .filter(([key]) => !FORM_FIELDS.includes(key))
            .map(([, message]) => message),
    );

    constructor() {
        effect(() => {
            const service = this.service();
            untracked(() => this.reset(service));
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
        this.save.emit({ name: value.name.trim(), unit: value.unit!, defaultRate: value.defaultRate ?? null });
    }

    private reset(service: Service | null): void {
        this.form.reset(
            {
                name: service?.name ?? '',
                unit: service?.unit ?? 'SquareMeter',
                defaultRate: service?.defaultRate ?? null,
            },
            { emitEvent: false },
        );
        this.confirmingDelete.set(false);
    }
}
