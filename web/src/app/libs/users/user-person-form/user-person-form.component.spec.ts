import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { testPerson } from '../../people/testing/fake-people.service';
import { APP_ICONS } from '../../shared/icons';
import { UserPersonFormComponent } from './user-person-form.component';

describe('UserPersonFormComponent', () => {
    let fixture: ComponentFixture<UserPersonFormComponent>;
    let element: HTMLElement;
    let saved: (string | null)[];
    let cancels: number;

    beforeAll(() => {
        Element.prototype.scrollTo ??= () => undefined;
    });

    beforeEach(async () => {
        TestBed.configureTestingModule({ providers: [provideNzIcons(APP_ICONS)] });
        fixture = TestBed.createComponent(UserPersonFormComponent);
        element = fixture.nativeElement as HTMLElement;
        saved = [];
        cancels = 0;
        fixture.componentInstance.save.subscribe((personId) => saved.push(personId));
        fixture.componentInstance.cancel.subscribe(() => cancels++);
        fixture.componentRef.setInput('people', [
            testPerson({ id: 'p1', name: 'Гошо' }),
            testPerson({ id: 'p4', name: 'Ива' }),
        ]);
        await fixture.whenStable();
    });

    afterEach(() => {
        document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => (overlay.innerHTML = ''));
    });

    const text = (node: Element | null | undefined) => node?.textContent?.replace(/\s+/g, ' ').trim();
    const selected = () => text(element.querySelector('nz-select#link-person .ant-select-selection-item'));
    const button = (label: string) =>
        Array.from(element.querySelectorAll<HTMLButtonElement>('button')).find((b) => text(b) === label);

    async function choose(label: string) {
        element.querySelector<HTMLElement>('nz-select#link-person .ant-select-selector')!.click();
        await fixture.whenStable();
        const options = await vi.waitFor(() => {
            const found = Array.from(document.querySelectorAll<HTMLElement>('nz-option-item'));
            expect(found.length).toBeGreaterThan(0);
            return found;
        });
        expect(options.map((o) => text(o))).toEqual(['Без човек', 'Гошо', 'Ива']);
        options.find((option) => text(option) === label)!.click();
        await fixture.whenStable();
    }

    it('starts with the current person (or „Без човек“) and sends the choice', async () => {
        expect(selected()).toBe('Без човек');

        fixture.componentRef.setInput('personId', 'p4');
        await fixture.whenStable();
        expect(selected()).toBe('Ива');

        await choose('Гошо');
        button('Запази')!.click();
        await choose('Без човек');
        button('Запази')!.click();

        expect(saved).toEqual(['p1', null]);
    });

    it('shows the server errors and Отказ emits cancel', async () => {
        fixture.componentRef.setInput('fieldErrors', { personId: 'Човекът не е от екипа.' });
        fixture.componentRef.setInput('error', 'Човекът вече е свързан с друг профил');
        await fixture.whenStable();

        expect(text(element)).toContain('Човекът не е от екипа.');
        expect(text(element.querySelector('nz-alert'))).toContain('Човекът вече е свързан с друг профил');
        button('Отказ')!.click();
        expect(cancels).toBe(1);
    });
});
