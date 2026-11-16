import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SiteStatusTagComponent } from './site-status-tag.component';

describe('SiteStatusTagComponent', () => {
    let fixture: ComponentFixture<SiteStatusTagComponent>;

    beforeEach(() => {
        fixture = TestBed.createComponent(SiteStatusTagComponent);
    });

    for (const [status, label] of [
        ['Lead', 'Оглед / оферта'],
        ['Active', 'Текущ'],
        ['Done', 'Приключен'],
    ] as const) {
        it(`${status} shows "${label}" with the pill--${status.toLowerCase()} class`, async () => {
            fixture.componentRef.setInput('status', status);
            await fixture.whenStable();

            const pill = (fixture.nativeElement as HTMLElement).querySelector('.pill')!;
            expect(pill.textContent?.trim()).toBe(label);
            expect(pill.classList).toContain(`pill--${status.toLowerCase()}`);
        });
    }
});
