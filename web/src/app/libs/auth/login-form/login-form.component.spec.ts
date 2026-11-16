import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { APP_ICONS } from '../../shared/icons';
import { LoginRequest } from '../auth.model';
import { LoginFormComponent } from './login-form.component';

describe('LoginFormComponent', () => {
    let fixture: ComponentFixture<LoginFormComponent>;
    let element: HTMLElement;
    let emitted: LoginRequest[];

    beforeEach(async () => {
        TestBed.configureTestingModule({ providers: [provideNzIcons(APP_ICONS)] });
        fixture = TestBed.createComponent(LoginFormComponent);
        emitted = [];
        fixture.componentInstance.submitted.subscribe((value) => emitted.push(value));
        element = fixture.nativeElement as HTMLElement;
        await fixture.whenStable();
    });

    function type(selector: string, value: string) {
        const input = element.querySelector<HTMLInputElement>(selector)!;
        input.value = value;
        input.dispatchEvent(new Event('input'));
    }

    async function submit() {
        element.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
        await fixture.whenStable();
    }

    it('does not emit for an empty form and shows the required messages', async () => {
        await submit();

        expect(emitted).toEqual([]);
        expect(element.textContent).toContain('Въведи потребителско име');
        expect(element.textContent).toContain('Въведи парола');
    });

    it('emits userName and password when both are filled', async () => {
        type('input[autocomplete="username"]', '  iva ');
        type('input[autocomplete="current-password"]', 'secret-pass');

        await submit();

        expect(emitted).toEqual([{ userName: 'iva', password: 'secret-pass' }]);
    });

    it('does not emit while pending', async () => {
        fixture.componentRef.setInput('pending', true);
        type('input[autocomplete="username"]', 'iva');
        type('input[autocomplete="current-password"]', 'secret-pass');

        fixture.componentInstance['submit']();

        expect(emitted).toEqual([]);
    });

    it('shows the error in an alert', async () => {
        fixture.componentRef.setInput('error', 'Грешно име или парола');
        await fixture.whenStable();

        expect(element.querySelector('nz-alert')?.textContent).toContain('Грешно име или парола');
    });

    it('has no alert without an error', () => {
        expect(element.querySelector('nz-alert')).toBeNull();
    });

    it('toggles password visibility with the eye button', async () => {
        const password = () => element.querySelector<HTMLInputElement>('input[autocomplete="current-password"]')!;
        expect(password().type).toBe('password');

        element.querySelector<HTMLButtonElement>('button.eye')!.click();
        await fixture.whenStable();

        expect(password().type).toBe('text');
    });
});
