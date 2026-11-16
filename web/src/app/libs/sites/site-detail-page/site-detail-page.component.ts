import { ChangeDetectionStrategy, Component, effect, inject, input, signal, untracked } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { AuthStore } from '../../auth/+state/auth.store';
import { CompaniesStore } from '../../companies/+state/companies.store';
import { Company } from '../../companies/company.model';
import { SitesStore } from '../+state/sites.store';
import { SaveSiteRequest } from '../site.model';
import { SiteFormComponent } from '../site-form/site-form.component';
import { SiteHeaderComponent } from '../site-header/site-header.component';

/** Smart page for one site (/sites/:id): header now, the sections come with their features. */
@Component({
    selector: 'atlas-site-detail-page',
    imports: [
        RouterLink,
        NzAlertModule,
        NzButtonModule,
        NzIconModule,
        NzModalModule,
        SiteHeaderComponent,
        SiteFormComponent,
    ],
    templateUrl: './site-detail-page.component.html',
    styleUrl: './site-detail-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SiteDetailPageComponent {
    /** Route parameter (withComponentInputBinding). */
    readonly id = input.required<string>();

    protected readonly store = inject(SitesStore);
    protected readonly companies = inject(CompaniesStore);
    protected readonly auth = inject(AuthStore);
    private readonly router = inject(Router);

    protected readonly formOpen = signal(false);
    protected readonly createdCompany = signal<Company | null>(null);

    constructor() {
        effect(() => {
            const id = this.id();
            untracked(() => {
                this.formOpen.set(false);
                void this.store.loadOne(id);
            });
        });
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
        if (await this.store.update(this.id(), request)) {
            this.formOpen.set(false);
        } else if (this.store.notFound()) {
            this.formOpen.set(false);
        }
    }

    protected async remove(): Promise<void> {
        if (await this.store.remove(this.id())) {
            this.formOpen.set(false);
            await this.router.navigateByUrl('/sites');
        }
    }

    protected async createCompany(name: string): Promise<void> {
        const company = await this.companies.create(name);
        if (company) {
            this.createdCompany.set(company);
        }
    }
}
