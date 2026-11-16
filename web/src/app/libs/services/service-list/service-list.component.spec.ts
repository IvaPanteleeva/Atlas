import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Service } from '../service.model';
import { ServiceListComponent } from './service-list.component';

const services: Service[] = [
    { id: 'a', name: 'Боядисване', unit: 'SquareMeter', defaultRate: 6 },
    { id: 'b', name: 'Шпакловка', unit: 'Day', defaultRate: null },
];

describe('ServiceListComponent', () => {
    let fixture: ComponentFixture<ServiceListComponent>;
    let element: HTMLElement;
    let opened: Service[];

    beforeEach(() => {
        fixture = TestBed.createComponent(ServiceListComponent);
        opened = [];
        fixture.componentInstance.open.subscribe((service) => opened.push(service));
        element = fixture.nativeElement as HTMLElement;
    });

    async function render(list: Service[], canEdit: boolean) {
        fixture.componentRef.setInput('services', list);
        fixture.componentRef.setInput('canEdit', canEdit);
        await fixture.whenStable();
    }

    const text = (node: Element | null | undefined) => node?.textContent?.replace(/\s+/g, ' ').trim();

    it('shows the empty state; Owner also gets the hint for the button', async () => {
        await render([], false);
        expect(text(element.querySelector('.empty'))).toBe('Още няма услуги.');

        await render([], true);
        expect(text(element.querySelector('.empty'))).toBe('Още няма услуги. Добави първата с „+ Нова услуга“.');
    });

    it('shows the name on the left and unit · price (or "без цена") on the right', async () => {
        await render(services, false);

        const rows = Array.from(element.querySelectorAll('.service'));
        expect(rows.map((row) => text(row.querySelector('.name')))).toEqual(['Боядисване', 'Шпакловка']);
        expect(rows.map((row) => text(row.querySelector('.price')))).toEqual(['м² · 6,00 €', 'ден · без цена']);
        expect(rows[1].querySelector('.price--none')).not.toBeNull();
        expect(rows[0].querySelector('.price--none')).toBeNull();
    });

    it('rows are buttons that emit the service only when canEdit', async () => {
        await render(services, false);
        expect(element.querySelectorAll('button').length).toBe(0);

        await render(services, true);
        element.querySelectorAll<HTMLButtonElement>('button.service')[1].click();

        expect(opened).toEqual([services[1]]);
    });
});
