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
import { PaymentsStore } from '../../payments/+state/payments.store';
import { Payment, SavePaymentRequest } from '../../payments/payment.model';
import { PaymentFormComponent } from '../../payments/payment-form/payment-form.component';
import { PaymentListComponent } from '../../payments/payment-list/payment-list.component';
import { PeopleStore } from '../../people/+state/people.store';
import { ServicesStore } from '../../services/+state/services.store';
import { CostLedgerComponent } from '../../shared/ledger/cost-ledger.component';
import { LedgerComponent } from '../../shared/ledger/ledger.component';
import { SiteContactsStore } from '../../site-contacts/+state/site-contacts.store';
import { SaveSiteContactRequest, SiteContact } from '../../site-contacts/site-contact.model';
import { SiteContactFormComponent } from '../../site-contacts/site-contact-form/site-contact-form.component';
import { SiteContactListComponent } from '../../site-contacts/site-contact-list/site-contact-list.component';
import { SitePricesStore } from '../../site-prices/+state/site-prices.store';
import { CreateSitePriceRequest, SitePrice } from '../../site-prices/site-price.model';
import { SitePriceFormComponent } from '../../site-prices/site-price-form/site-price-form.component';
import { SitePriceListComponent } from '../../site-prices/site-price-list/site-price-list.component';
import { SummariesStore } from '../../summaries/+state/summaries.store';
import { WorkDaysStore } from '../../work-days/+state/work-days.store';
import { SaveWorkDayRequest, WorkDay } from '../../work-days/work-day.model';
import { WorkDayFormComponent } from '../../work-days/work-day-form/work-day-form.component';
import { WorkDayListComponent } from '../../work-days/work-day-list/work-day-list.component';
import { SitesStore } from '../+state/sites.store';
import { SaveSiteRequest } from '../site.model';
import { SiteFormComponent } from '../site-form/site-form.component';
import { SiteHeaderComponent } from '../site-header/site-header.component';

/**
 * Smart page for one site (/sites/:id): header, "Работни дни", "Плащания" and "Цени за обекта".
 * Owner and Manager see and edit everything here; only Owner deletes (sites, days, payments, prices, contacts).
 */
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
        LedgerComponent,
        CostLedgerComponent,
        PaymentListComponent,
        SiteContactListComponent,
        SiteContactFormComponent,
        // Only used inside @defer: the forms (and the date picker) are separate lazy chunks.
        WorkDayFormComponent,
        PaymentFormComponent,
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
    protected readonly summaries = inject(SummariesStore);
    protected readonly payments = inject(PaymentsStore);
    protected readonly people = inject(PeopleStore);
    protected readonly contacts = inject(SiteContactsStore);
    private readonly router = inject(Router);

    protected readonly formOpen = signal(false);
    protected readonly createdCompany = signal<Company | null>(null);
    protected readonly priceFormOpen = signal(false);
    /** The price being edited; null = a new one. */
    protected readonly editingPrice = signal<SitePrice | null>(null);
    protected readonly dayFormOpen = signal(false);
    /** The day being edited; null = a new one. */
    protected readonly editingDay = signal<WorkDay | null>(null);
    protected readonly contactFormOpen = signal(false);
    /** The contact being edited; null = a new one. */
    protected readonly editingContact = signal<SiteContact | null>(null);
    protected readonly paymentFormOpen = signal(false);
    /** The payment being edited; null = a new one. */
    protected readonly editingPayment = signal<Payment | null>(null);

    constructor() {
        // Fetch the lazy form chunks when the browser is idle, so the first "+ Ден работа" / "+ Плащане" opens quickly.
        afterNextRender(() => {
            const prefetch = () => {
                void import('../../work-days/work-day-form/work-day-form.component').catch(() => undefined);
                if (this.auth.isStaff()) {
                    void import('../../payments/payment-form/payment-form.component').catch(() => undefined);
                }
            };
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
                this.paymentFormOpen.set(false);
                this.contactFormOpen.set(false);
                void this.store.loadOne(id);
                void this.contacts.load(id);
                void this.workDays.load(id);
                if (this.auth.isStaff()) {
                    void this.summaries.loadSite(id);
                    void this.payments.load(id);
                }
                void this.prices.load(id);
                // The price and work day forms offer services from the shared list.
                void this.services.load();
                // „Кой работи“ in the work day form offers the crew.
                void this.people.load();
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
        if (!this.auth.isStaff()) {
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

    protected openContactForm(contact: SiteContact | null): void {
        this.contacts.clearErrors();
        this.editingContact.set(contact);
        this.contactFormOpen.set(true);
    }

    protected closeContactForm(): void {
        this.contactFormOpen.set(false);
    }

    protected async saveContact(request: SaveSiteContactRequest): Promise<void> {
        const editing = this.editingContact();
        const saved = editing
            ? await this.contacts.update(this.id(), editing.id, request)
            : await this.contacts.create(this.id(), request);
        if (saved) {
            this.contactFormOpen.set(false);
        }
    }

    protected async removeContact(): Promise<void> {
        const editing = this.editingContact();
        if (editing && (await this.contacts.remove(this.id(), editing.id))) {
            this.contactFormOpen.set(false);
        }
    }

    protected openPaymentForm(payment: Payment | null): void {
        this.payments.clearErrors();
        this.editingPayment.set(payment);
        this.paymentFormOpen.set(true);
    }

    protected closePaymentForm(): void {
        this.paymentFormOpen.set(false);
    }

    protected async savePayment(request: SavePaymentRequest): Promise<void> {
        const editing = this.editingPayment();
        const saved = editing
            ? await this.payments.update(this.id(), editing.id, request)
            : await this.payments.create(this.id(), request);
        if (saved) {
            this.paymentFormOpen.set(false);
            this.reloadAfterPaymentChange();
        }
    }

    protected async removePayment(): Promise<void> {
        const editing = this.editingPayment();
        if (editing && (await this.payments.remove(this.id(), editing.id))) {
            this.paymentFormOpen.set(false);
            this.reloadAfterPaymentChange();
        }
    }

    /** „1 човек без сума за деня“, „N хора …“. */
    protected workerOpenLabel(count: number): string {
        return `${count} ${count === 1 ? 'човек' : 'хора'} без сума за деня`;
    }

    /** One open item reads „1 позиция …“, more „N позиции …“. */
    protected openLabel(count: number): string {
        return `${count} ${count === 1 ? 'позиция' : 'позиции'} без цена / за уточняване`;
    }

    /**
     * A new day makes a lead current, so the header is reloaded; the list too, quietly (nothing blinks).
     * The money changes with the days, so the summary is reloaded as well.
     */
    private reloadAfterDayChange(): void {
        void this.store.refreshOne(this.id());
        void this.workDays.load(this.id());
        if (this.auth.isStaff()) {
            void this.summaries.refreshSite(this.id());
        }
    }

    /** Quietly reloads the summary and the payments. */
    private reloadAfterPaymentChange(): void {
        void this.summaries.refreshSite(this.id());
        void this.payments.load(this.id());
    }
}
