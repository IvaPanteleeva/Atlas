import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { AuthStore } from '../../auth/+state/auth.store';
import { ServicesStore } from '../+state/services.store';
import { SaveServiceRequest, Service } from '../service.model';
import { ServiceFormComponent } from '../service-form/service-form.component';
import { ServiceListComponent } from '../service-list/service-list.component';

/** Smart page "Услуги": the shared list of kinds of work. Owner and Manager add and edit; only Owner deletes. */
@Component({
    selector: 'atlas-services-page',
    imports: [NzAlertModule, NzButtonModule, NzIconModule, NzModalModule, ServiceListComponent, ServiceFormComponent],
    templateUrl: './services-page.component.html',
    styleUrl: './services-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServicesPageComponent {
    protected readonly store = inject(ServicesStore);
    protected readonly auth = inject(AuthStore);

    protected readonly formOpen = signal(false);
    /** The service being edited; null = a new one. */
    protected readonly editing = signal<Service | null>(null);

    constructor() {
        void this.store.load();
    }

    protected openForm(service: Service | null): void {
        if (!this.auth.isStaff()) {
            return;
        }
        this.store.clearErrors();
        this.editing.set(service);
        this.formOpen.set(true);
    }

    protected closeForm(): void {
        this.formOpen.set(false);
    }

    protected async save(request: SaveServiceRequest): Promise<void> {
        const editing = this.editing();
        const saved = editing ? await this.store.update(editing.id, request) : await this.store.create(request);
        if (saved) {
            this.formOpen.set(false);
        }
    }

    protected async remove(): Promise<void> {
        const editing = this.editing();
        if (editing && (await this.store.remove(editing.id))) {
            this.formOpen.set(false);
        }
    }
}
