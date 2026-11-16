import { ChangeDetectionStrategy, Component, computed, input, model, output } from '@angular/core';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzInputModule } from 'ng-zorro-antd/input';
import { COMPANY_NAME_MAX_LENGTH } from '../company.model';

/** Dumb add line: the page owns the typed name (two-way) and clears it after a successful create. */
@Component({
    selector: 'atlas-company-add',
    imports: [NzButtonModule, NzInputModule],
    templateUrl: './company-add.component.html',
    styleUrl: './company-add.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CompanyAddComponent {
    readonly name = model('');
    readonly saving = input(false);
    readonly added = output<string>();

    protected readonly maxLength = COMPANY_NAME_MAX_LENGTH;
    protected readonly canAdd = computed(() => !this.saving() && !!this.name().trim());

    protected add(): void {
        if (this.canAdd()) {
            this.added.emit(this.name().trim());
        }
    }
}
