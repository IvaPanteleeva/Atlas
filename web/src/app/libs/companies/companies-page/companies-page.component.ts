import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { CompaniesStore } from '../+state/companies.store';
import { CompanyAddComponent } from '../company-add/company-add.component';
import { CompanyRowComponent } from '../company-row/company-row.component';

/** Smart page "Фирми" (Owner only): the only component here that talks to the store. */
@Component({
    selector: 'atlas-companies-page',
    imports: [NzAlertModule, CompanyRowComponent, CompanyAddComponent],
    templateUrl: './companies-page.component.html',
    styleUrl: './companies-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CompaniesPageComponent {
    protected readonly store = inject(CompaniesStore);

    /** The row being renamed; one at a time. */
    protected readonly editingId = signal<string | null>(null);
    protected readonly newName = signal('');

    constructor() {
        void this.store.load();
    }

    protected startEdit(id: string): void {
        this.store.clearError();
        this.editingId.set(id);
    }

    protected cancelEdit(): void {
        this.editingId.set(null);
    }

    protected async rename(id: string, name: string): Promise<void> {
        if (await this.store.rename(id, name)) {
            this.editingId.set(null);
        }
    }

    protected async remove(id: string): Promise<void> {
        await this.store.remove(id);
    }

    protected async add(name: string): Promise<void> {
        if (await this.store.create(name)) {
            this.newName.set('');
        }
    }
}
