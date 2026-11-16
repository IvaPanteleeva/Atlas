import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { EurPipe } from '../../shared/eur.pipe';
import { UNIT_LABELS } from '../../shared/units';
import { Service } from '../service.model';

/** Dumb list of services: name on the left, unit and default price on the right. Rows are buttons only when canEdit. */
@Component({
    selector: 'atlas-service-list',
    providers: [EurPipe],
    templateUrl: './service-list.component.html',
    styleUrl: './service-list.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServiceListComponent {
    readonly services = input.required<Service[]>();
    /** Owner: rows open the form, and the empty state points to the button. */
    readonly canEdit = input(false);
    readonly open = output<Service>();

    private readonly eur = inject(EurPipe);

    /** "м² · 6,00 €" or "м² · без цена". */
    protected priceText(service: Service): string {
        const price = service.defaultRate === null ? 'без цена' : this.eur.transform(service.defaultRate);
        return `${UNIT_LABELS[service.unit]} · ${price}`;
    }
}
