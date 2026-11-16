import {
    afterRenderEffect,
    ChangeDetectionStrategy,
    Component,
    computed,
    ElementRef,
    input,
    linkedSignal,
    output,
    viewChild,
} from '@angular/core';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { Company, COMPANY_NAME_MAX_LENGTH } from '../company.model';

/** Dumb row: shows a company, or an inline rename field while the page says it is being edited. */
@Component({
    selector: 'atlas-company-row',
    imports: [NzButtonModule, NzIconModule, NzInputModule, NzPopconfirmModule],
    templateUrl: './company-row.component.html',
    styleUrl: './company-row.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CompanyRowComponent {
    readonly company = input.required<Company>();
    /** Controlled by the page, so only one row is edited at a time and it closes only after a successful save. */
    readonly editing = input(false);
    readonly saving = input(false);

    readonly editStart = output<void>();
    readonly editCancel = output<void>();
    readonly renamed = output<string>();
    readonly removed = output<void>();

    protected readonly maxLength = COMPANY_NAME_MAX_LENGTH;

    /** The typed name; starts again from the current name every time editing opens or closes. */
    protected readonly draft = linkedSignal({
        source: () => ({ editing: this.editing(), name: this.company().name }),
        computation: ({ name }) => name,
    });
    protected readonly canSave = computed(() => !this.saving() && !!this.draft().trim());

    private readonly nameInput = viewChild<ElementRef<HTMLInputElement>>('nameInput');

    constructor() {
        afterRenderEffect(() => {
            const field = this.nameInput()?.nativeElement;
            if (field) {
                field.focus();
                field.select();
            }
        });
    }

    protected save(): void {
        if (!this.canSave()) {
            return;
        }
        const name = this.draft().trim();
        if (name === this.company().name) {
            this.editCancel.emit();
            return;
        }
        this.renamed.emit(name);
    }
}
