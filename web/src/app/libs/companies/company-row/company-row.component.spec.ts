import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { APP_ICONS } from '../../shared/icons';
import { CompanyRowComponent } from './company-row.component';

describe('CompanyRowComponent', () => {
    let fixture: ComponentFixture<CompanyRowComponent>;
    let element: HTMLElement;
    let events: string[];

    beforeEach(async () => {
        TestBed.configureTestingModule({ providers: [provideNzIcons(APP_ICONS)] });
        fixture = TestBed.createComponent(CompanyRowComponent);
        fixture.componentRef.setInput('company', { id: 'a', name: 'Женистрой' });
        events = [];
        const component = fixture.componentInstance;
        component.editStart.subscribe(() => events.push('editStart'));
        component.editCancel.subscribe(() => events.push('editCancel'));
        component.renamed.subscribe((name) => events.push(`renamed ${name}`));
        component.removed.subscribe(() => events.push('removed'));
        element = fixture.nativeElement as HTMLElement;
        await fixture.whenStable();
    });

    const button = (label: string) =>
        Array.from(element.querySelectorAll<HTMLButtonElement>('button')).find(
            (candidate) => candidate.getAttribute('aria-label') === label || candidate.textContent?.trim() === label,
        );
    const input = () => element.querySelector<HTMLInputElement>('input');

    async function editing() {
        fixture.componentRef.setInput('editing', true);
        await fixture.whenStable();
    }

    function type(value: string) {
        input()!.value = value;
        input()!.dispatchEvent(new Event('input'));
    }

    it('shows the name with rename and delete buttons', () => {
        expect(element.querySelector('.name')?.textContent?.trim()).toBe('Женистрой');
        expect(button('Преименувай')).toBeDefined();
        expect(button('Изтрий')).toBeDefined();
        expect(input()).toBeNull();
    });

    it('the pencil asks to start editing', () => {
        button('Преименувай')!.click();
        expect(events).toEqual(['editStart']);
    });

    it('in edit mode shows an input with the current name, Запази and Отказ', async () => {
        await editing();

        expect(input()?.value).toBe('Женистрой');
        expect(button('Запази')).toBeDefined();
        expect(button('Отказ')).toBeDefined();
        expect(element.querySelector('.name')).toBeNull();
    });

    it('Запази emits the trimmed new name', async () => {
        await editing();
        type('  Женистрой 2 ');
        await fixture.whenStable();

        button('Запази')!.click();

        expect(events).toEqual(['renamed Женистрой 2']);
    });

    it('Enter in the input saves', async () => {
        await editing();
        type('Нова');

        input()!.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));

        expect(events).toEqual(['renamed Нова']);
    });

    it('Отказ and Escape cancel', async () => {
        await editing();

        button('Отказ')!.click();
        input()!.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));

        expect(events).toEqual(['editCancel', 'editCancel']);
    });

    it('an unchanged name just cancels', async () => {
        await editing();

        button('Запази')!.click();

        expect(events).toEqual(['editCancel']);
    });

    it('a blank name cannot be saved', async () => {
        await editing();
        type('   ');
        await fixture.whenStable();

        expect(button('Запази')!.disabled).toBe(true);
        input()!.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
        expect(events).toEqual([]);
    });

    it('a new edit starts from the current name again', async () => {
        await editing();
        type('Чернова');
        fixture.componentRef.setInput('editing', false);
        await fixture.whenStable();

        await editing();

        expect(input()?.value).toBe('Женистрой');
    });

    it('delete asks for confirmation and emits removed on "Изтрий"', async () => {
        button('Изтрий')!.click();
        await fixture.whenStable();

        // nz-popconfirm renders in a CDK overlay attached to document.body.
        const ok = await vi.waitFor(() => {
            expect(document.body.textContent).toContain('Изтриване на фирмата?');
            const found = Array.from(document.querySelectorAll<HTMLButtonElement>('.ant-popover button')).find(
                (candidate) => candidate.textContent?.trim() === 'Изтрий',
            );
            expect(found).toBeDefined();
            return found!;
        });
        expect(events).toEqual([]);

        ok.click();

        expect(events).toEqual(['removed']);
    });

    it('buttons are disabled while saving', async () => {
        fixture.componentRef.setInput('saving', true);
        await fixture.whenStable();

        expect(button('Преименувай')!.disabled).toBe(true);
        expect(button('Изтрий')!.disabled).toBe(true);
    });
});
