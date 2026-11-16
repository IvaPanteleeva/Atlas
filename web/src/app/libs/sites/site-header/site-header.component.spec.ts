import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Site } from '../site.model';
import { SiteHeaderComponent } from './site-header.component';

const site: Site = {
    id: 's1',
    name: 'Бункера',
    status: 'Active',
    companyId: 'c1',
    companyName: 'Женистрой',
    endCustomer: 'Иван',
    address: 'ул. Шипка 5',
    mapUrl: 'https://maps.app.goo.gl/abc',
    note: 'Ред 1\nРед 2',
};

describe('SiteHeaderComponent', () => {
    let fixture: ComponentFixture<SiteHeaderComponent>;
    let element: HTMLElement;

    beforeEach(() => {
        fixture = TestBed.createComponent(SiteHeaderComponent);
        element = fixture.nativeElement as HTMLElement;
    });

    async function render(value: Site) {
        fixture.componentRef.setInput('site', value);
        await fixture.whenStable();
    }

    it('shows the name, status, meta line, map link and note', async () => {
        await render(site);

        expect(element.querySelector('h1')?.textContent?.trim()).toBe('Бункера');
        expect(element.querySelector('atlas-site-status-tag')?.textContent?.trim()).toBe('Текущ');
        expect(element.querySelector('.meta')?.textContent?.replace(/\s+/g, ' ').trim()).toBe(
            'Женистрой · Иван · ул. Шипка 5 · карта ↗',
        );
        const link = element.querySelector<HTMLAnchorElement>('.meta a')!;
        expect(link.href).toBe('https://maps.app.goo.gl/abc');
        expect(link.target).toBe('_blank');
        expect(link.rel).toContain('noopener');
        expect(element.querySelector('.note')?.textContent).toBe('Ред 1\nРед 2');
    });

    it('hides the empty parts', async () => {
        await render({ ...site, companyName: null, endCustomer: null, address: null, mapUrl: null, note: null });

        expect(element.querySelector('.meta')).toBeNull();
        expect(element.querySelector('.note')).toBeNull();
    });

    it('a map link alone is still shown', async () => {
        await render({ ...site, companyName: null, endCustomer: null, address: null });

        expect(element.querySelector('.meta')?.textContent?.trim()).toBe('карта ↗');
    });
});
