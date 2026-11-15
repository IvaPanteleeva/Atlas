import { TestBed } from '@angular/core/testing';
import { PlaceholderPageComponent } from './placeholder-page.component';

describe('PlaceholderPageComponent', () => {
    it('shows the given title and "Предстои"', async () => {
        const fixture = TestBed.createComponent(PlaceholderPageComponent);
        fixture.componentRef.setInput('title', 'Хора');
        await fixture.whenStable();

        const element = fixture.nativeElement as HTMLElement;
        expect(element.querySelector('h1')?.textContent).toBe('Хора');
        expect(element.textContent).toContain('Предстои');
    });
});
