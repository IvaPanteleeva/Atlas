import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { APP_ICONS } from '../../shared/icons';
import { PasswordFormComponent } from './password-form.component';

describe('PasswordFormComponent', () => {
    let fixture: ComponentFixture<PasswordFormComponent>;
    let element: HTMLElement;
    let saved: string[];
    let cancels: number;

    beforeEach(async () => {
        TestBed.configureTestingModule({ providers: [provideNzIcons(APP_ICONS)] });
        fixture = TestBed.createComponent(PasswordFormComponent);
        element = fixture.nativeElement as HTMLElement;
        saved = [];
        cancels = 0;
        fixture.componentInstance.save.subscribe((password) => saved.push(password));
        fixture.componentInstance.cancel.subscribe(() => cancels++);
        await fixture.whenStable();
    });

    const input = () => element.querySelector<HTMLInputElement>('#new-password')!;
    const button = (label: string) =>
        Array.from(element.querySelectorAll<HTMLButtonElement>('button')).find(
            (b) => b.textContent?.trim() === label || b.getAttribute('aria-label') === label,
        );

    async function type(value: string) {
        input().value = value;
        input().dispatchEvent(new Event('input'));
        await fixture.whenStable();
    }

    it('needs at least 8 characters', async () => {
        await type('1234567');
        button('Запази')!.click();
        await fixture.whenStable();

        expect(saved).toEqual([]);
        expect(element.textContent).toContain('Поне 8 знака');

        await type('12345678');
        button('Запази')!.click();

        expect(saved).toEqual(['12345678']);
    });

    it('the eye shows and hides the password', async () => {
        expect(input().type).toBe('password');

        button('Покажи паролата')!.click();
        await fixture.whenStable();

        expect(input().type).toBe('text');
    });

    it('shows the server error for the password and the general error', async () => {
        fixture.componentRef.setInput('fieldErrors', { password: 'Паролата е твърде кратка' });
        fixture.componentRef.setInput('error', 'Профилът вече го няма');
        await fixture.whenStable();

        expect(input().closest('nz-form-item')?.textContent).toContain('Паролата е твърде кратка');
        expect(element.querySelector('nz-alert')?.textContent).toContain('Профилът вече го няма');
    });

    it('Отказ emits cancel', () => {
        button('Отказ')!.click();

        expect(cancels).toBe(1);
    });
});
