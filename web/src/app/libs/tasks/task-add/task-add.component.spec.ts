import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { testPerson } from '../../people/testing/fake-people.service';
import { APP_ICONS } from '../../shared/icons';
import { NewTask } from '../task.model';
import { TaskAddComponent } from './task-add.component';

describe('TaskAddComponent', () => {
    let fixture: ComponentFixture<TaskAddComponent>;
    let element: HTMLElement;
    let added: NewTask[];

    beforeAll(() => {
        Element.prototype.scrollTo ??= () => undefined;
    });

    beforeEach(async () => {
        TestBed.configureTestingModule({ providers: [provideNzIcons(APP_ICONS)] });
        fixture = TestBed.createComponent(TaskAddComponent);
        element = fixture.nativeElement as HTMLElement;
        added = [];
        fixture.componentInstance.add.subscribe((task) => added.push(task));
        fixture.componentRef.setInput('crew', [
            testPerson({ id: 'p1', name: 'Гошо' }),
            testPerson({ id: 'p3', name: 'Пешо' }),
        ]);
        await fixture.whenStable();
    });

    afterEach(() => {
        document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => (overlay.innerHTML = ''));
    });

    const text = (node: Element | null | undefined) => node?.textContent?.replace(/\s+/g, ' ').trim();
    const input = () => element.querySelector<HTMLInputElement>('input.new-text')!;
    const selected = () => text(element.querySelector('nz-select .ant-select-selection-item'));
    const placeholder = () => text(element.querySelector('nz-select .ant-select-selection-placeholder'));

    async function type(value: string) {
        input().value = value;
        input().dispatchEvent(new Event('input'));
        await fixture.whenStable();
    }

    async function choose(label: string) {
        element.querySelector<HTMLElement>('nz-select .ant-select-selector')!.click();
        await fixture.whenStable();
        const options = await vi.waitFor(() => {
            const found = Array.from(document.querySelectorAll<HTMLElement>('nz-option-item'));
            expect(found.length).toBeGreaterThan(0);
            return found;
        });
        expect(options.map((option) => text(option))).toEqual(['Никой', 'Гошо', 'Пешо']);
        options.find((option) => text(option) === label)!.click();
        await fixture.whenStable();
    }

    function submit() {
        element.querySelector<HTMLFormElement>('form')!.dispatchEvent(new Event('submit'));
    }

    it('has „Нова задача…“, „Кой?“ and „Добави“', () => {
        expect(input().placeholder).toBe('Нова задача…');
        expect(input().maxLength).toBe(500);
        expect(placeholder()).toBe('Кой?');
        expect(text(element.querySelector('button[type=submit]'))).toBe('Добави');
    });

    it('Enter / „Добави“ sends the trimmed text and the person', async () => {
        await type('  Да се купи лепило ');
        submit();
        await choose('Пешо');
        await type('Кофи');
        element.querySelector<HTMLButtonElement>('button[type=submit]')!.click();

        expect(added).toEqual([
            { text: 'Да се купи лепило', assigneeId: null },
            { text: 'Кофи', assigneeId: 'p3' },
        ]);
    });

    it('an empty text sends nothing; saving blocks a second send', async () => {
        await type('   ');
        submit();
        expect(added).toEqual([]);

        await type('Лепило');
        fixture.componentRef.setInput('saving', true);
        await fixture.whenStable();
        submit();
        expect(added).toEqual([]);
    });

    it('starts with the default person and clears the field after resetKey changes', async () => {
        fixture.componentRef.setInput('defaultAssigneeId', 'p1');
        await fixture.whenStable();
        expect(selected()).toBe('Гошо');

        await choose('Пешо');
        await type('Лепило');
        fixture.componentRef.setInput('resetKey', 1);
        await fixture.whenStable();

        expect(input().value).toBe('');
        expect(selected()).toBe('Гошо');
    });

    it('shows the server message', async () => {
        fixture.componentRef.setInput('error', 'Човекът не е от екипа');
        await fixture.whenStable();

        expect(text(element.querySelector('.server-error'))).toBe('Човекът не е от екипа');
    });
});
