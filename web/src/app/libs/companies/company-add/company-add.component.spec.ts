import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CompanyAddComponent } from './company-add.component';

describe('CompanyAddComponent', () => {
    let fixture: ComponentFixture<CompanyAddComponent>;
    let element: HTMLElement;
    let added: string[];

    beforeEach(async () => {
        fixture = TestBed.createComponent(CompanyAddComponent);
        added = [];
        fixture.componentInstance.added.subscribe((name) => added.push(name));
        element = fixture.nativeElement as HTMLElement;
        await fixture.whenStable();
    });

    const input = () => element.querySelector<HTMLInputElement>('input')!;
    const addButton = () => element.querySelector<HTMLButtonElement>('button')!;

    async function type(value: string) {
        input().value = value;
        input().dispatchEvent(new Event('input'));
        await fixture.whenStable();
    }

    it('has the placeholder and a disabled "Добави" while empty', () => {
        expect(input().placeholder).toBe('Нова фирма…');
        expect(addButton().textContent?.trim()).toBe('Добави');
        expect(addButton().disabled).toBe(true);
    });

    it('"Добави" emits the trimmed name', async () => {
        await type('  Нова фирма ');

        addButton().click();

        expect(added).toEqual(['Нова фирма']);
    });

    it('Enter adds too', async () => {
        await type('Нова');

        input().dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));

        expect(added).toEqual(['Нова']);
    });

    it('a blank name is not emitted', async () => {
        await type('   ');

        input().dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));

        expect(added).toEqual([]);
        expect(addButton().disabled).toBe(true);
    });

    it('does not emit while saving', async () => {
        await type('Нова');
        fixture.componentRef.setInput('saving', true);
        await fixture.whenStable();

        input().dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));

        expect(added).toEqual([]);
    });

    it('the parent controls the value (two-way name)', async () => {
        await type('Нова');
        expect(fixture.componentInstance.name()).toBe('Нова');

        fixture.componentRef.setInput('name', '');
        await fixture.whenStable();

        expect(input().value).toBe('');
    });
});
