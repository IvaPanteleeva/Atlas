import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SiteListItem } from '../site.model';
import { SiteListComponent } from './site-list.component';

const sites: SiteListItem[] = [
    {
        id: 'a',
        name: 'Бункера',
        status: 'Active',
        companyName: 'Женистрой',
        endCustomer: 'Иван',
        address: 'ул. Шипка 5',
    },
    { id: 'b', name: 'Професора', status: 'Lead', companyName: null, endCustomer: 'Професорът', address: null },
    { id: 'c', name: 'Гаража', status: 'Done', companyName: null, endCustomer: null, address: null },
];

describe('SiteListComponent', () => {
    let fixture: ComponentFixture<SiteListComponent>;
    let element: HTMLElement;
    let opened: string[];

    beforeEach(() => {
        fixture = TestBed.createComponent(SiteListComponent);
        opened = [];
        fixture.componentInstance.open.subscribe((id) => opened.push(id));
        element = fixture.nativeElement as HTMLElement;
    });

    async function render(list: SiteListItem[]) {
        fixture.componentRef.setInput('sites', list);
        await fixture.whenStable();
    }

    const rows = () => Array.from(element.querySelectorAll<HTMLButtonElement>('button.site'));

    it('shows the empty state for no sites', async () => {
        await render([]);

        expect(rows().length).toBe(0);
        expect(element.textContent).toContain('Още няма обекти. Добави първия с „+ Нов обект“.');
    });

    it('shows one row per site with name, status and meta (company, else end customer, then address)', async () => {
        await render(sites);

        expect(rows().map((row) => row.querySelector('.name')?.textContent?.trim())).toEqual([
            'Бункера',
            'Професора',
            'Гаража',
        ]);
        expect(rows().map((row) => row.querySelector('.meta')?.textContent?.trim() ?? null)).toEqual([
            'Женистрой · ул. Шипка 5',
            'Професорът',
            null,
        ]);
        expect(rows()[0].querySelector('atlas-site-status-tag')?.textContent?.trim()).toBe('Текущ');
    });

    it('clicking a row emits open with its id', async () => {
        await render(sites);

        rows()[1].click();

        expect(opened).toEqual(['b']);
    });
});
