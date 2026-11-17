import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { APP_ICONS } from '../../shared/icons';
import { SavePersonRequest } from '../person.model';
import { testPerson } from '../testing/fake-people.service';
import { PersonFormComponent } from './person-form.component';

describe('PersonFormComponent', () => {
    let fixture: ComponentFixture<PersonFormComponent>;
    let element: HTMLElement;
    let saved: SavePersonRequest[];
    let deletes: number;
    let cancels: number;

    beforeEach(async () => {
        TestBed.configureTestingModule({ providers: [provideNzIcons(APP_ICONS)] });
        fixture = TestBed.createComponent(PersonFormComponent);
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
    const crewBox = () => element.querySelector<HTMLInputElement>('label.crew input[type="checkbox"]')!;
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

    it('has name, phone (tel keyboard), note with the hint and the crew checkbox', () => {
        expect(field('person-name').maxLength).toBe(100);
        expect(field('person-phone').getAttribute('inputmode')).toBe('tel');
        expect(field('person-phone').maxLength).toBe(30);
        expect(field<HTMLTextAreaElement>('person-note').maxLength).toBe(500);
        expect(element.textContent).toContain('Само за нас');
        expect(element.querySelector('label.crew')?.textContent).toContain('Работи с нас (в екипа)');
        expect(field('person-rate')).toBeTruthy();
        expect(field('person-rate').closest('nz-form-item')?.textContent).toContain('Дневна ставка');
        expect(field('person-rate').closest('nz-form-item')?.textContent).toContain('Предлага се като сума за деня');
        expect(field('person-rate').closest('nz-input-wrapper')?.textContent).toContain('€');
        expect(button('Изтрий')).toBeUndefined();
    });

    it('requires a name (blank does not count)', async () => {
        await type('person-name', '   ');

        await submit();

        expect(saved).toEqual([]);
        expect(element.textContent).toContain('Въведи име');
    });

    it('a new crew member: the checkbox is preset by crewDefault, values are trimmed, empty ones are null', async () => {
        await set('crewDefault', true);
        expect(crewBox().checked).toBe(true);
        await type('person-name', '  Владо ');
        await type('person-phone', ' ');

        await submit();

        expect(saved).toEqual([{ name: 'Владо', phone: null, note: null, isCrew: true, dailyRate: null }]);
    });

    it('a new contact: the checkbox starts off', async () => {
        await set('crewDefault', false);
        expect(crewBox().checked).toBe(false);
        await type('person-name', 'Иван бояджия');
        await type('person-phone', '0877 000 111');
        await type('person-note', 'Само боя');

        await submit();

        expect(saved).toEqual([
            {
                name: 'Иван бояджия',
                phone: '0877 000 111',
                note: 'Само боя',
                isCrew: false,
                dailyRate: null,
            },
        ]);
    });

    it('fills the fields when editing; unchecking „Работи с нас“ is sent', async () => {
        await set(
            'person',
            testPerson({
                name: 'Пешо',
                phone: '0899',
                note: 'Има кола',
                isCrew: true,
                dailyRate: 110,
            }),
        );
        expect(field('person-rate').value).toBe('110,00');
        expect(field('person-name').value).toBe('Пешо');
        expect(field('person-phone').value).toBe('0899');
        expect(field<HTMLTextAreaElement>('person-note').value).toBe('Има кола');
        expect(crewBox().checked).toBe(true);

        crewBox().click();
        await fixture.whenStable();
        await submit();

        expect(saved).toEqual([{ name: 'Пешо', phone: '0899', note: 'Има кола', isCrew: false, dailyRate: 110 }]);
    });

    it('the daily rate is sent as a number (decimal comma) and must be above 0', async () => {
        await type('person-name', 'Владо');
        await type('person-rate', '0');
        await submit();
        expect(saved).toEqual([]);
        expect(field('person-rate').closest('nz-form-item')?.textContent).toContain(
            'Сумата трябва да е по-голяма от 0',
        );

        await type('person-rate', '125,5');
        await submit();
        expect(saved).toEqual([{ name: 'Владо', phone: null, note: null, isCrew: true, dailyRate: 125.5 }]);
    });

    it('a linked person shows „Свързан с профил … — съдружник“; there is no partner checkbox', async () => {
        expect(element.querySelector('.linked')).toBeNull();
        expect(element.querySelector('label.partner')).toBeNull();

        await set('person', testPerson({ name: 'Ива', isPartner: true, linkedUserName: 'iva' }));

        expect(element.querySelector('.linked')?.textContent?.trim()).toBe('Свързан с профил iva — съдружник');
        await submit();
        expect(saved[0]).not.toHaveProperty('isPartner');
    });

    it('delete is only shown with canDelete (Owner)', async () => {
        await set('person', testPerson());

        expect(button('Изтрий')).toBeUndefined();
    });

    it('delete is shown when editing and needs a second click', async () => {
        await set('person', testPerson());
        await set('canDelete', true);

        button('Изтрий')!.click();
        await fixture.whenStable();
        expect(deletes).toBe(0);

        button('Потвърди изтриване')!.click();

        expect(deletes).toBe(1);
    });

    it('shows a refused delete (409) in the alert and server field errors under the fields', async () => {
        await set('person', testPerson());
        await set('error', 'Човекът има работни дни');
        await set('fieldErrors', { phone: 'Телефонът е твърде дълъг' });

        expect(element.querySelector('nz-alert')?.textContent).toContain('Човекът има работни дни');
        expect(field('person-phone').closest('nz-form-item')?.textContent).toContain('Телефонът е твърде дълъг');
    });

    it('Отказ emits cancel', () => {
        button('Отказ')!.click();

        expect(cancels).toBe(1);
    });
});
