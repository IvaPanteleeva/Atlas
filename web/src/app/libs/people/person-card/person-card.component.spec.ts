import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Person } from '../person.model';
import { testPerson } from '../testing/fake-people.service';
import { PersonCardComponent } from './person-card.component';

describe('PersonCardComponent', () => {
    let fixture: ComponentFixture<PersonCardComponent>;
    let element: HTMLElement;
    let edited: Person[];
    let originalClipboard: PropertyDescriptor | undefined;

    const text = (node: Element | null | undefined) => node?.textContent?.replace(/\s+/g, ' ').trim();
    const button = (label: string) =>
        Array.from(element.querySelectorAll<HTMLButtonElement>('button')).find((b) => text(b) === label);

    function mockClipboard(writeText: (value: string) => Promise<void>) {
        Object.defineProperty(navigator, 'clipboard', { value: { writeText }, configurable: true });
    }

    async function render(person: Person, options: { showNote?: boolean; canEdit?: boolean } = {}) {
        fixture.componentRef.setInput('person', person);
        fixture.componentRef.setInput('showNote', options.showNote ?? false);
        fixture.componentRef.setInput('canEdit', options.canEdit ?? false);
        await fixture.whenStable();
    }

    beforeEach(() => {
        originalClipboard = Object.getOwnPropertyDescriptor(navigator, 'clipboard');
        fixture = TestBed.createComponent(PersonCardComponent);
        element = fixture.nativeElement as HTMLElement;
        edited = [];
        fixture.componentInstance.edit.subscribe((person) => edited.push(person));
    });

    afterEach(() => {
        if (originalClipboard) {
            Object.defineProperty(navigator, 'clipboard', originalClipboard);
        } else {
            delete (navigator as { clipboard?: unknown }).clipboard;
        }
        window.getSelection()?.removeAllRanges();
    });

    it('shows the name, the phone and a tel: link „Обади се“', async () => {
        await render(testPerson({ name: 'Гошо', phone: '0888 111 222' }));

        expect(text(element.querySelector('.name'))).toBe('Гошо');
        expect(text(element.querySelector('.phone'))).toBe('0888 111 222');
        const call = element.querySelector<HTMLAnchorElement>('a.call')!;
        expect(text(call)).toBe('Обади се');
        expect(call.getAttribute('href')).toBe('tel:0888111222');
    });

    it('a person without a phone has no phone buttons', async () => {
        await render(testPerson({ phone: null }));

        expect(element.querySelector('.phone')).toBeNull();
        expect(element.querySelector('a.call')).toBeNull();
        expect(button('Копирай')).toBeUndefined();
    });

    it('„Копирай“ writes the phone to the clipboard', async () => {
        const writeText = vi.fn(() => Promise.resolve());
        mockClipboard(writeText);
        await render(testPerson({ phone: '+359 888 111 222' }));

        button('Копирай')!.click();
        await fixture.whenStable();

        expect(writeText).toHaveBeenCalledWith('+359 888 111 222');
        await vi.waitFor(() => expect(text(element.querySelector('.copy-status'))).toBe('Копирано'));
    });

    it('when the clipboard fails, the phone is selected for manual copy', async () => {
        mockClipboard(() => Promise.reject(new Error('denied')));
        await render(testPerson({ phone: '0888 111 222' }));

        button('Копирай')!.click();
        await fixture.whenStable();

        await vi.waitFor(() => expect(text(element.querySelector('.copy-status'))).toBe('Маркирано, копирай ръчно'));
        expect(window.getSelection()?.toString()).toBe('0888 111 222');
    });

    it('shows „N дни на обекти · последно dd.mm“, „1 ден“ and „Няма записани дни“', async () => {
        await render(testPerson({ dayCount: 12, lastWorkDate: '2026-11-16' }));
        expect(text(element.querySelector('.days'))).toBe('12 дни на обекти · последно 16.11');

        await render(testPerson({ dayCount: 1, lastWorkDate: '2026-03-05' }));
        expect(text(element.querySelector('.days'))).toBe('1 ден на обекти · последно 05.03');

        await render(testPerson({ dayCount: 0, lastWorkDate: null }));
        expect(text(element.querySelector('.days'))).toBe('Няма записани дни');
    });

    it('the note and „Редакция“ are only shown when allowed (Owner)', async () => {
        const person = testPerson({ note: 'Има кола' });
        await render(person);
        expect(element.querySelector('.note')).toBeNull();
        expect(button('Редакция')).toBeUndefined();

        await render(person, { showNote: true, canEdit: true });
        expect(text(element.querySelector('.note'))).toBe('Има кола');
        button('Редакция')!.click();
        expect(edited).toEqual([person]);
    });
});
