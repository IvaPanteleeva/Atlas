import { NgTemplateOutlet } from '@angular/common';
import {
    afterNextRender,
    ChangeDetectionStrategy,
    Component,
    effect,
    inject,
    input,
    signal,
    untracked,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { AuthStore } from '../../auth/+state/auth.store';
import { CompaniesStore } from '../../companies/+state/companies.store';
import { Company } from '../../companies/company.model';
import { ServicesStore } from '../../services/+state/services.store';
import { SitePricesStore } from '../../site-prices/+state/site-prices.store';
import { CreateSitePriceRequest, SitePrice } from '../../site-prices/site-price.model';
import { SitePriceFormComponent } from '../../site-prices/site-price-form/site-price-form.component';
import { SitePriceListComponent } from '../../site-prices/site-price-list/site-price-list.component';
import { WorkDaysStore } from '../../work-days/+state/work-days.store';
import { SaveWorkDayRequest, WorkDay } from '../../work-days/work-day.model';
import { WorkDayFormComponent } from '../../work-days/work-day-form/work-day-form.component';
import { WorkDayListComponent } from '../../work-days/work-day-list/work-day-list.component';
import { SitesStore } from '../+state/sites.store';
import { SaveSiteRequest } from '../site.model';
import { SiteFormComponent } from '../site-form/site-form.component';
import { SiteHeaderComponent } from '../site-header/site-header.component';

/** Smart page for one site (/sites/:id): header, "Работни дни" and "Цени за обекта"; more sections come later. */
@Component({
    selector: 'atlas-site-detail-page',
    imports: [
        NgTemplateOutlet,
        RouterLink,
        NzAlertModule,
        NzButtonModule,
        NzIconModule,
        NzModalModule,
        SiteHeaderComponent,
        SiteFormComponent,
        SitePriceListComponent,
        SitePriceFormComponent,
        WorkDayListComponent,
        // Only used inside @defer: the form (and the date picker) is a separate lazy chunk.
        WorkDayFormComponent,
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
    protected readonly prices = inject(SitePricesStore);
    protected readonly services = inject(ServicesStore);
    protected readonly workDays = inject(WorkDaysStore);
    private readonly router = inject(Router);

    protected readonly formOpen = signal(false);
    protected readonly createdCompany = signal<Company | null>(null);
    protected readonly priceFormOpen = signal(false);
    /** The price being edited; null = a new one. */
    protected readonly editingPrice = signal<SitePrice | null>(null);
    protected readonly dayFormOpen = signal(false);
    /** The day being edited; null = a new one. */
    protected readonly editingDay = signal<WorkDay | null>(null);

    constructor() {
        // Fetch the lazy work day form chunk when the browser is idle, so the first "+ Ден работа" opens quickly.
        afterNextRender(() => {
            const prefetch = () =>
                void import('../../work-days/work-day-form/work-day-form.component').catch(() => undefined);
            if (typeof requestIdleCallback === 'function') {
                requestIdleCallback(prefetch);
            } else {
                setTimeout(prefetch, 2000);
            }
        });

        effect(() => {
            const id = this.id();
            untracked(() => {
                this.formOpen.set(false);
                this.priceFormOpen.set(false);
                this.dayFormOpen.set(false);
                void this.store.loadOne(id);
                void this.workDays.load(id);
                void this.prices.load(id);
                // The price and work day forms offer services from the shared list.
                void this.services.load();
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

    protected openPriceForm(price: SitePrice | null): void {
        if (!this.auth.isOwner()) {
            return;
        }
        this.prices.clearErrors();
        this.editingPrice.set(price);
        this.priceFormOpen.set(true);
    }

    protected closePriceForm(): void {
        this.priceFormOpen.set(false);
    }

    protected async savePrice(request: CreateSitePriceRequest): Promise<void> {
        const editing = this.editingPrice();
        const saved = editing
            ? await this.prices.update(this.id(), editing.id, { rate: request.rate })
            : await this.prices.create(this.id(), request);
        if (saved) {
            this.priceFormOpen.set(false);
        }
    }

    protected async removePrice(): Promise<void> {
        const editing = this.editingPrice();
        if (editing && (await this.prices.remove(this.id(), editing.id))) {
            this.priceFormOpen.set(false);
        }
    }

    protected openDayForm(day: WorkDay | null): void {
        this.workDays.clearErrors();
        this.editingDay.set(day);
        this.dayFormOpen.set(true);
    }

    protected closeDayForm(): void {
        this.dayFormOpen.set(false);
    }

    protected async saveDay(request: SaveWorkDayRequest): Promise<void> {
        const editing = this.editingDay();
        const saved = editing
            ? await this.workDays.update(this.id(), editing.id, request)
            : await this.workDays.create(this.id(), request);
        if (saved) {
            this.dayFormOpen.set(false);
            this.reloadAfterDayChange();
        }
    }

    protected async removeDay(): Promise<void> {
        const editing = this.editingDay();
        if (editing && (await this.workDays.remove(this.id(), editing.id))) {
            this.dayFormOpen.set(false);
            this.reloadAfterDayChange();
        }
    }

    /** A new day makes a lead current, so the header is reloaded; the list too, quietly (nothing blinks). */
    private reloadAfterDayChange(): void {
        void this.store.refreshOne(this.id());
        void this.workDays.load(this.id());
    }
}
