import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Person } from '../person.model';
import { testPerson } from '../testing/fake-people.service';
import { ContactListComponent } from './contact-list.component';

describe('ContactListComponent', () => {
    let fixture: ComponentFixture<ContactListComponent>;
    let element: HTMLElement;
    let edited: Person[];

    const text = (node: Element | null | undefined) => node?.textContent?.replace(/\s+/g, ' ').trim();
    const rows = () => Array.from(element.querySelectorAll<HTMLElement>('.contact'));

    async function render(people: Person[], options: { showNotes?: boolean; canEdit?: boolean } = {}) {
        fixture.componentRef.setInput('people', people);
        fixture.componentRef.setInput('showNotes', options.showNotes ?? false);
        fixture.componentRef.setInput('canEdit', options.canEdit ?? false);
        await fixture.whenStable();
    }

    beforeEach(() => {
        fixture = TestBed.createComponent(ContactListComponent);
        element = fixture.nativeElement as HTMLElement;
        edited = [];
        fixture.componentInstance.edit.subscribe((person) => edited.push(person));
    });

    it('shows the empty state', async () => {
        await render([]);

        expect(text(element)).toBe('Няма контакти.');
    });

    it('shows name and phone with „Копирай“ (no call button), without notes or edit when not allowed', async () => {
        await render([
            testPerson({ id: 'a', name: 'Иван бояджия', phone: '0877 1', note: 'Само боя', isCrew: false }),
            testPerson({ id: 'b', name: 'Магазин', phone: null, isCrew: false }),
        ]);

        expect(rows().map((row) => text(row.querySelector('.name')))).toEqual(['Иван бояджия', 'Магазин']);
        expect(text(rows()[0].querySelector('.phone'))).toBe('0877 1');
        expect(rows()[0].querySelector('a.call')).toBeNull();
        expect(text(rows()[0].querySelector('button.copy'))).toBe('Копирай');
        expect(rows()[1].querySelector('.phone')).toBeNull();
        expect(element.querySelector('.note')).toBeNull();
        expect(element.querySelector('button.edit')).toBeNull();
    });

    it('Owner sees the note and „Редакция“', async () => {
        const person = testPerson({ id: 'a', name: 'Иван', note: 'Само боя', isCrew: false });
        await render([person], { showNotes: true, canEdit: true });

        expect(text(rows()[0].querySelector('.note'))).toBe('Само боя');
        rows()[0].querySelector<HTMLButtonElement>('button.edit')!.click();

        expect(edited).toEqual([person]);
    });
});
