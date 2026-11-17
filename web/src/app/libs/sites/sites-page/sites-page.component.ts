import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { AuthStore } from '../../auth/+state/auth.store';
import { CompaniesStore } from '../../companies/+state/companies.store';
import { Company } from '../../companies/company.model';
import { EurPipe } from '../../shared/eur.pipe';
import { CostLedgerComponent } from '../../shared/ledger/cost-ledger.component';
import { LedgerComponent } from '../../shared/ledger/ledger.component';
import { SummariesStore } from '../../summaries/+state/summaries.store';
import { SitesStore } from '../+state/sites.store';
import { SaveSiteRequest } from '../site.model';
import { SiteFormComponent } from '../site-form/site-form.component';
import { SiteListComponent } from '../site-list/site-list.component';

/**
 * Smart page "Обекти": the list and the "Нов обект" form. Only this component talks to the stores.
 * Owner and Manager see the money: the totals above the list and the amounts on each row.
 */
@Component({
    selector: 'atlas-sites-page',
    imports: [
        NzAlertModule,
        NzButtonModule,
        NzIconModule,
        NzModalModule,
        EurPipe,
        LedgerComponent,
        CostLedgerComponent,
        SiteListComponent,
        SiteFormComponent,
    ],
    templateUrl: './sites-page.component.html',
    styleUrl: './sites-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SitesPageComponent {
    protected readonly store = inject(SitesStore);
    protected readonly companies = inject(CompaniesStore);
    protected readonly auth = inject(AuthStore);
    protected readonly summaries = inject(SummariesStore);
    private readonly router = inject(Router);

    protected readonly formOpen = signal(false);
    protected readonly createdCompany = signal<Company | null>(null);

    constructor() {
        void this.store.load();
        if (this.auth.isStaff()) {
            void this.summaries.loadAll();
        }
    }

    protected openSite(id: string): void {
        void this.router.navigate(['/sites', id]);
    }

    protected openForm(): void {
        this.store.clearErrors();
        this.companies.clearError();
        this.createdCompany.set(null);
        void this.companies.load();
        this.formOpen.set(true);
    }

    protected closeForm(): void {
        this.formOpen.set(false);
    }

    protected async save(request: SaveSiteRequest): Promise<void> {
        const site = await this.store.create(request);
        if (site) {
            this.formOpen.set(false);
            await this.router.navigate(['/sites', site.id]);
        }
    }

    protected async createCompany(name: string): Promise<void> {
        const company = await this.companies.create(name);
        if (company) {
            this.createdCompany.set(company);
        }
    }
}
