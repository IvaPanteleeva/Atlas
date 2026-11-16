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
import { NzCheckboxModule } from 'ng-zorro-antd/checkbox';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { FieldErrors } from '../../shared/api-errors';
import { Person, PERSON_LIMITS, SavePersonRequest } from '../person.model';

/** Fields that have a place in the form; server errors for other keys go to the alert. */
const FORM_FIELDS = ['name', 'phone', 'note', 'isCrew'];

function notBlank(control: AbstractControl<string>): ValidationErrors | null {
    return control.value.trim() ? null : { required: true };
}

function orNull(value: string): string | null {
    const trimmed = value.trim();
    return trimmed ? trimmed : null;
}

/** Dumb form for a person (create when person is null, edit otherwise). The page owns saving and the sheet. */
@Component({
    selector: 'atlas-person-form',
    imports: [ReactiveFormsModule, NzAlertModule, NzButtonModule, NzCheckboxModule, NzFormModule, NzInputModule],
    templateUrl: './person-form.component.html',
    styleUrl: './person-form.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PersonFormComponent {
    readonly person = input<Person | null>(null);
    /** „Работи с нас“ for a new person: true from „+ Човек в екипа“, false from „+ Контакт“. */
    readonly crewDefault = input(true);
    /** Owner only. */
    readonly canDelete = input(false);
    readonly saving = input(false);
    readonly error = input<string | null>(null);
    readonly fieldErrors = input<FieldErrors>({});

    readonly save = output<SavePersonRequest>();
    readonly delete = output<void>();
    readonly cancel = output<void>();

    protected readonly limits = PERSON_LIMITS;

    protected readonly form = inject(NonNullableFormBuilder).group({
        name: ['', [notBlank, Validators.maxLength(PERSON_LIMITS.name)]],
        phone: ['', Validators.maxLength(PERSON_LIMITS.phone)],
        note: ['', Validators.maxLength(PERSON_LIMITS.note)],
        isCrew: [true],
    });

    protected readonly confirmingDelete = signal(false);

    protected readonly otherErrors = computed(() =>
        Object.entries(this.fieldErrors())
            .filter(([key]) => !FORM_FIELDS.includes(key))
            .map(([, message]) => message),
    );

    constructor() {
        effect(() => {
            const person = this.person();
            const crewDefault = this.crewDefault();
            untracked(() => this.reset(person, crewDefault));
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
        this.save.emit({
            name: value.name.trim(),
            phone: orNull(value.phone),
            note: orNull(value.note),
            isCrew: value.isCrew,
        });
    }

    private reset(person: Person | null, crewDefault: boolean): void {
        this.form.reset(
            {
                name: person?.name ?? '',
                phone: person?.phone ?? '',
                note: person?.note ?? '',
                isCrew: person ? person.isCrew : crewDefault,
            },
            { emitEvent: false },
        );
        this.confirmingDelete.set(false);
    }
}
