import { ChangeDetectionStrategy, Component, effect, inject, input, output, untracked } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { Person } from '../../people/person.model';
import { FieldErrors } from '../../shared/api-errors';

/** Select value of „Без човек“ (nz-select shows null as an empty field). */
const NO_PERSON = '';

/** Dumb form with one select: the crew person linked to a login profile, or „Без човек“. */
@Component({
    selector: 'atlas-user-person-form',
    imports: [ReactiveFormsModule, NzAlertModule, NzButtonModule, NzFormModule, NzSelectModule],
    templateUrl: './user-person-form.component.html',
    styleUrl: './user-person-form.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserPersonFormComponent {
    /** Crew people that can be chosen (the current one included, those linked to other profiles left out). */
    readonly people = input<Person[]>([]);
    /** The person linked now; null = none. */
    readonly personId = input<string | null>(null);
    readonly saving = input(false);
    readonly error = input<string | null>(null);
    readonly fieldErrors = input<FieldErrors>({});

    readonly save = output<string | null>();
    readonly cancel = output<void>();

    protected readonly noPerson = NO_PERSON;
    protected readonly form = inject(NonNullableFormBuilder).group({ personId: [NO_PERSON] });

    constructor() {
        effect(() => {
            const personId = this.personId();
            untracked(() => this.form.reset({ personId: personId ?? NO_PERSON }, { emitEvent: false }));
        });
    }

    protected submit(): void {
        if (!this.saving()) {
            this.save.emit(this.form.controls.personId.value || null);
        }
    }
}
