import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SiteContact } from '../site-contact.model';
import { testSiteContact } from '../testing/fake-site-contacts.service';
import { SiteContactListComponent } from './site-contact-list.component';

describe('SiteContactListComponent', () => {
    let fixture: ComponentFixture<SiteContactListComponent>;
    let element: HTMLElement;
    let edited: SiteContact[];

    const text = (node: Element | null | undefined) => node?.textContent?.replace(/\s+/g, ' ').trim();
    const rows = () => Array.from(element.querySelectorAll<HTMLElement>('.contact'));

    async function render(contacts: SiteContact[], canEdit = true) {
        fixture.componentRef.setInput('contacts', contacts);
        fixture.componentRef.setInput('canEdit', canEdit);
        await fixture.whenStable();
    }

    beforeEach(() => {
        fixture = TestBed.createComponent(SiteContactListComponent);
        element = fixture.nativeElement as HTMLElement;
        edited = [];
        fixture.componentInstance.edit.subscribe((contact) => edited.push(contact));
    });

    it('shows the empty state', async () => {
        await render([]);

        expect(text(element)).toBe('Няма контакти за обекта.');
    });

    it('shows name, muted role, phone with „Обади се“ and „Копирай“, and the note', async () => {
        await render([
            testSiteContact({ id: 'a', name: 'Професорът', role: 'собственик', phone: '0888 5', note: 'След 17 ч.' }),
            testSiteContact({ id: 'b', name: 'Шофьор', role: null, phone: null, note: null }),
        ]);

        expect(rows().map((row) => text(row.querySelector('.name')))).toEqual(['Професорът', 'Шофьор']);
        expect(text(rows()[0].querySelector('.role'))).toBe('собственик');
        expect(rows()[0].querySelector('a.call')?.getAttribute('href')).toBe('tel:08885');
        expect(text(rows()[0].querySelector('button.copy'))).toBe('Копирай');
        expect(text(rows()[0].querySelector('.note'))).toBe('След 17 ч.');
        expect(rows()[1].querySelector('.role')).toBeNull();
        expect(rows()[1].querySelector('.phone')).toBeNull();
        expect(rows()[1].querySelector('.note')).toBeNull();
    });

    it('„Редакция“ emits the contact; hidden without canEdit', async () => {
        const contact = testSiteContact();
        await render([contact]);

        rows()[0].querySelector<HTMLButtonElement>('button.edit')!.click();
        expect(edited).toEqual([contact]);

        await render([contact], false);
        expect(element.querySelector('button.edit')).toBeNull();
    });
});
