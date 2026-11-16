import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { APP_ICONS } from '../../shared/icons';
import { SaveSiteContactRequest } from '../site-contact.model';
import { testSiteContact } from '../testing/fake-site-contacts.service';
import { SiteContactFormComponent } from './site-contact-form.component';

describe('SiteContactFormComponent', () => {
    let fixture: ComponentFixture<SiteContactFormComponent>;
    let element: HTMLElement;
    let saved: SaveSiteContactRequest[];
    let deletes: number;
    let cancels: number;

    beforeEach(async () => {
        TestBed.configureTestingModule({ providers: [provideNzIcons(APP_ICONS)] });
        fixture = TestBed.createComponent(SiteContactFormComponent);
        saved = [];
        deletes = 0;
        cancels = 0;
        const form = fixture.componentInstance;
        form.save.subscribe((request) => saved.push(request));
        form.delete.subscribe(() => deletes++);
        form.cancel.subscribe(() => cancels++);
        element = fixture.nativeElement as HTMLElement;
        await fixture.whenStable();
    });

    async function set(name: string, value: unknown) {
        fixture.componentRef.setInput(name, value);
        await fixture.whenStable();
    }

    const field = <T extends HTMLElement = HTMLInputElement>(id: string) => element.querySelector<T>(`#${id}`)!;
    const button = (label: string) =>
        Array.from(element.querySelectorAll<HTMLButtonElement>('button')).find(
            (candidate) => candidate.textContent?.trim() === label,
        );

    async function type(id: string, value: string) {
        const input = field<HTMLInputElement | HTMLTextAreaElement>(id);
        input.value = value;
        input.dispatchEvent(new Event('input'));
        await fixture.whenStable();
    }

    async function submit() {
        button('Запази')!.click();
        await fixture.whenStable();
    }

    it('has the fields with their limits, the role placeholder and the tel keyboard', () => {
        expect(field('sc-name').maxLength).toBe(100);
        expect(field('sc-role').maxLength).toBe(50);
        expect(field('sc-role').placeholder).toBe('напр. собственик, съсед');
        expect(field('sc-phone').maxLength).toBe(30);
        expect(field('sc-phone').getAttribute('inputmode')).toBe('tel');
        expect(field<HTMLTextAreaElement>('sc-note').maxLength).toBe(500);
        expect(button('Изтрий')).toBeUndefined();
    });

    it('requires a name (blank does not count)', async () => {
        await type('sc-name', '  ');

        await submit();

        expect(saved).toEqual([]);
        expect(element.textContent).toContain('Въведи име');
    });

    it('trims the values; empty optional ones are null', async () => {
        await type('sc-name', ' Професорът ');
        await type('sc-role', ' собственик ');
        await type('sc-phone', ' ');

        await submit();

        expect(saved).toEqual([{ name: 'Професорът', role: 'собственик', phone: null, note: null }]);
    });

    it('fills the fields when editing', async () => {
        await set('contact', testSiteContact({ note: 'След 17 ч.' }));

        expect(field('sc-name').value).toBe('Професорът');
        expect(field('sc-role').value).toBe('собственик');
        expect(field('sc-phone').value).toBe('0888 555 666');
        expect(field<HTMLTextAreaElement>('sc-note').value).toBe('След 17 ч.');
        await submit();

        expect(saved).toEqual([{ name: 'Професорът', role: 'собственик', phone: '0888 555 666', note: 'След 17 ч.' }]);
    });

    it('delete only with canDelete when editing, and it needs a second click', async () => {
        await set('contact', testSiteContact());
        expect(button('Изтрий')).toBeUndefined();

        await set('canDelete', true);
        button('Изтрий')!.click();
        await fixture.whenStable();
        expect(deletes).toBe(0);
        button('Потвърди изтриване')!.click();

        expect(deletes).toBe(1);
    });

    it('shows server errors and Отказ emits cancel', async () => {
        await set('fieldErrors', { role: 'Най-много 50 знака' });
        await set('error', 'Грешка при записа');

        expect(field('sc-role').closest('nz-form-item')?.textContent).toContain('Най-много 50 знака');
        expect(element.querySelector('nz-alert')?.textContent).toContain('Грешка при записа');

        button('Отказ')!.click();
        expect(cancels).toBe(1);
    });
});
