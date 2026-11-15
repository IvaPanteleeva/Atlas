import { Location } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { routes } from '../../../../app.routes';
import { NAV_ICONS } from '../nav-items';
import { ShellComponent } from './shell.component';

describe('ShellComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [ShellComponent],
            providers: [provideRouter(routes, withComponentInputBinding()), provideNzIcons(NAV_ICONS)],
        });
    });

    async function render(url: string) {
        const harness = await RouterTestingHarness.create();
        await harness.navigateByUrl(url);
        const fixture = TestBed.createComponent(ShellComponent);
        await fixture.whenStable();
        return { harness, element: fixture.nativeElement as HTMLElement };
    }

    it('renders the four tabs with labels and links in both navs', async () => {
        const { element } = await render('/sites');

        for (const selector of ['.tabs--top', '.tabs--bottom']) {
            const links = Array.from(element.querySelectorAll<HTMLAnchorElement>(`${selector} a`));
            expect(links.map((a) => a.textContent?.trim())).toEqual(['Обекти', 'Хора', 'Задачи', 'Цени']);
            expect(links.map((a) => a.getAttribute('href'))).toEqual(['/sites', '/people', '/tasks', '/rate-card']);
        }
    });

    it('marks the "Хора" tab as current after navigating to /people', async () => {
        const { harness, element } = await render('/people');
        expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('Хора');

        const current = Array.from(element.querySelectorAll('.tabs--bottom a[aria-current="page"]'));
        expect(current.map((a) => a.textContent?.trim())).toEqual(['Хора']);
        const currentTop = element.querySelector('.tabs--top a[aria-current="page"]');
        expect(currentTop?.textContent?.trim()).toBe('Хора');
    });

    it('redirects / to /sites', async () => {
        const harness = await RouterTestingHarness.create();
        await harness.navigateByUrl('/');
        expect(TestBed.inject(Location).path()).toBe('/sites');
    });
});
