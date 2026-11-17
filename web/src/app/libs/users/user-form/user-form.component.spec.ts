import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { testPerson } from '../../people/testing/fake-people.service';
import { APP_ICONS } from '../../shared/icons';
import { CreateUserRequest } from '../user.model';
import { UserFormComponent } from './user-form.component';

describe('UserFormComponent', () => {
    let fixture: ComponentFixture<UserFormComponent>;
    let element: HTMLElement;
    let saved: CreateUserRequest[];
    let cancels: number;

    beforeAll(() => {
        Element.prototype.scrollTo ??= () => undefined;
    });

    beforeEach(async () => {
        TestBed.configureTestingModule({ providers: [provideNzIcons(APP_ICONS)] });
        fixture = TestBed.createComponent(UserFormComponent);
        element = fixture.nativeElement as HTMLElement;
        saved = [];
        cancels = 0;
        fixture.componentInstance.save.subscribe((request) => saved.push(request));
        fixture.componentInstance.cancel.subscribe(() => cancels++);
        fixture.componentRef.setInput('people', [
            testPerson({ id: 'p1', name: 'Гошо' }),
            testPerson({ id: 'p4', name: 'Ива' }),
        ]);
        fixture.componentRef.setInput('companies', [
            { id: 'c1', name: 'Женистрой' },
            { id: 'c2', name: 'Друга' },
        ]);
        await fixture.whenStable();
    });

    afterEach(() => {
        document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => (overlay.innerHTML = ''));
    });

    const text = (node: Element | null | undefined) => node?.textContent?.replace(/\s+/g, ' ').trim();
    const field = (id: string) => element.querySelector<HTMLInputElement>(`#${id}`)!;
    const button = (label: string) =>
        Array.from(element.querySelectorAll<HTMLButtonElement>('button')).find(
            (b) => text(b) === label || b.getAttribute('aria-label') === label,
        );

    async function type(id: string, value: string) {
        field(id).value = value;
        field(id).dispatchEvent(new Event('input'));
        await fixture.whenStable();
    }

    async function choose(select: string, label: string) {
        element.querySelector<HTMLElement>(`nz-select#${select} .ant-select-selector`)!.click();
        await fixture.whenStable();
        const options = await vi.waitFor(() => {
            const found = Array.from(document.querySelectorAll<HTMLElement>('nz-option-item'));
            expect(found.length).toBeGreaterThan(0);
            return found;
        });
        options.find((option) => text(option) === label)!.click();
        await fixture.whenStable();
    }

    async function submit() {
        button('Запази')!.click();
        await fixture.whenStable();
    }

    it('starts as „Управител“ without the company field', () => {
        expect(text(element.querySelector('nz-select#user-role .ant-select-selection-item'))).toBe('Управител');
        expect(element.querySelector('nz-select#user-company')).toBeNull();
    });

    it('requires a name and a password of at least 8 characters', async () => {
        await type('user-password', '1234');

        await submit();

        expect(saved).toEqual([]);
        expect(element.textContent).toContain('Въведи потребителско име');
        expect(element.textContent).toContain('Поне 8 знака');
    });

    it('creates a manager profile without a company (name trimmed)', async () => {
        await type('user-name', '  pesho ');
        await type('user-password', '12345678');

        await submit();

        expect(saved).toEqual([
            { userName: 'pesho', password: '12345678', role: 'Manager', companyId: null, personId: null },
        ]);
    });

    it('the company is asked for (and required) only for „Възложител“', async () => {
        await type('user-name', 'zheni');
        await type('user-password', '12345678');
        await choose('user-role', 'Възложител');
        expect(element.querySelector('nz-select#user-company')).toBeTruthy();

        await submit();
        expect(saved).toEqual([]);
        expect(element.textContent).toContain('Избери фирма');

        await choose('user-company', 'Женистрой');
        await submit();
        expect(saved).toEqual([
            { userName: 'zheni', password: '12345678', role: 'ClientViewer', companyId: 'c1', personId: null },
        ]);

        await choose('user-role', 'Администратор');
        expect(element.querySelector('nz-select#user-company')).toBeNull();
        await submit();
        expect(saved[1]).toEqual({
            userName: 'zheni',
            password: '12345678',
            role: 'Owner',
            companyId: null,
            personId: null,
        });
    });

    it('„Човек от екипа“ is optional („Без човек“), sent as personId and hidden for „Възложител“', async () => {
        expect(text(element.querySelector('nz-select#user-person .ant-select-selection-item'))).toBe('Без човек');
        await type('user-name', 'iva2');
        await type('user-password', '12345678');
        await choose('user-person', 'Ива');

        await submit();
        expect(saved).toEqual([
            { userName: 'iva2', password: '12345678', role: 'Manager', companyId: null, personId: 'p4' },
        ]);

        await choose('user-role', 'Възложител');
        expect(element.querySelector('nz-select#user-person')).toBeNull();
        await choose('user-company', 'Женистрой');
        await submit();
        expect(saved[1].personId).toBeNull();
    });

    it('the role select offers Администратор, Управител and Възложител', async () => {
        element.querySelector<HTMLElement>('nz-select#user-role .ant-select-selector')!.click();
        await fixture.whenStable();

        const options = await vi.waitFor(() => {
            const found = Array.from(document.querySelectorAll<HTMLElement>('nz-option-item'));
            expect(found.length).toBe(3);
            return found;
        });
        expect(options.map((o) => text(o))).toEqual(['Администратор', 'Управител', 'Възложител']);
    });

    it('the eye shows the password', async () => {
        button('Покажи паролата')!.click();
        await fixture.whenStable();

        expect(field('user-password').type).toBe('text');
    });

    it('shows a 409 in the alert and field errors under their fields', async () => {
        fixture.componentRef.setInput('error', 'Има такъв потребител');
        fixture.componentRef.setInput('fieldErrors', { userName: 'Невалидно име' });
        await fixture.whenStable();

        expect(element.querySelector('nz-alert')?.textContent).toContain('Има такъв потребител');
        expect(field('user-name').closest('nz-form-item')?.textContent).toContain('Невалидно име');
    });

    it('Отказ emits cancel', () => {
        button('Отказ')!.click();

        expect(cancels).toBe(1);
    });
});
