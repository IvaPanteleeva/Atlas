import { ComponentFixture, TestBed } from '@angular/core/testing';
import { WorkDay } from '../work-day.model';
import { testWorkDay, testWorkItem } from '../testing/fake-work-days.service';
import { WorkDayListComponent } from './work-day-list.component';

describe('WorkDayListComponent', () => {
    let fixture: ComponentFixture<WorkDayListComponent>;
    let element: HTMLElement;
    let edited: WorkDay[];

    const text = (node: Element | null | undefined) => node?.textContent?.replace(/\s+/g, ' ').trim();
    const days = () => Array.from(element.querySelectorAll<HTMLElement>('.day'));

    async function render(list: WorkDay[], canEdit = true) {
        fixture.componentRef.setInput('days', list);
        fixture.componentRef.setInput('canEdit', canEdit);
        await fixture.whenStable();
    }

    beforeEach(() => {
        fixture = TestBed.createComponent<WorkDayListComponent<WorkDay>>(WorkDayListComponent);
        element = fixture.nativeElement as HTMLElement;
        edited = [];
        fixture.componentInstance.edit.subscribe((day) => edited.push(day));
    });

    it('shows the empty state', async () => {
        await render([]);

        expect(text(element)).toBe('Няма записани дни. Натисни „+ Ден работа“ след всеки ден на обекта.');
    });

    it('shows the date as "dd.MM" with the short Bulgarian weekday, and the title', async () => {
        await render([
            testWorkDay({ id: 'd1', date: '2026-11-16', title: 'Шпакловка финал' }),
            testWorkDay({ id: 'd2', date: '2026-11-15', title: null }),
        ]);

        expect(days().map((d) => text(d.querySelector('.date__day')))).toEqual(['16.11', '15.11']);
        expect(days().map((d) => text(d.querySelector('.date__weekday')))).toEqual(['пн', 'нд']);
        expect(text(days()[0].querySelector('.title'))).toBe('Шпакловка финал');
        expect(days()[1].querySelector('.title')).toBeNull();
    });

    it('shows quantity with a decimal comma, unit, price and the amount from the server', async () => {
        await render([testWorkDay({ items: [testWorkItem({ quantity: 12.5, rate: 6.5, amount: 81.25 })] })]);

        const row = days()[0].querySelector('.item')!;
        expect(text(row.querySelector('.desc'))).toBe('Шпакловка');
        expect(text(row.querySelector('.calc'))).toBe('12,5 м² × 6,50');
        expect(text(row.querySelector('.sum'))).toBe('81,25 €');
        expect(row.querySelector('.pill')).toBeNull();
    });

    it('marks a missing price with „цена?“ and no sum; a price to clarify with „уточни“', async () => {
        await render([
            testWorkDay({
                items: [
                    testWorkItem({
                        id: 'a',
                        description: 'Первази',
                        quantity: 1.125,
                        unit: 'LinearMeter',
                        rate: null,
                        amount: null,
                    }),
                    testWorkItem({ id: 'b', description: 'Боя', needsReview: true, rate: 3, quantity: 2, amount: 6 }),
                ],
                total: 6,
            }),
        ]);

        const [first, second] = Array.from(days()[0].querySelectorAll('.item'));
        expect(text(first.querySelector('.pill'))).toBe('цена?');
        expect(text(first.querySelector('.calc'))).toBe('1,125 м.л.');
        expect(text(first.querySelector('.sum'))).toBe('—');
        expect(text(second.querySelector('.pill'))).toBe('уточни');
        expect(text(second.querySelector('.sum'))).toBe('6,00 €');
    });

    it('shows „Общо“ only with more than one item', async () => {
        await render([
            testWorkDay({
                id: 'd1',
                items: [testWorkItem({ id: 'a' }), testWorkItem({ id: 'b', amount: 1000 })],
                total: 1081.25,
            }),
            testWorkDay({ id: 'd2' }),
        ]);

        const total = days()[0].querySelector('.total')!;
        expect(text(total.querySelector('td'))).toBe('Общо');
        expect(text(total.querySelector('.sum'))).toBe('1 081,25 €');
        expect(days()[1].querySelector('.total')).toBeNull();
    });

    it('shows who worked as chips next to the title (also without a title)', async () => {
        await render([
            testWorkDay({
                id: 'd1',
                workers: [
                    { id: 'p1', name: 'Гошо', pay: 120, isPartner: false },
                    { id: 'p3', name: 'Пешо', pay: null, isPartner: false },
                ],
            }),
            testWorkDay({ id: 'd2', title: null, workers: [{ id: 'p1', name: 'Гошо', pay: 120, isPartner: false }] }),
            testWorkDay({ id: 'd3', workers: [] }),
        ]);

        expect(Array.from(days()[0].querySelectorAll('.chip')).map((chip) => text(chip))).toEqual(['Гошо', 'Пешо']);
        expect(text(days()[0].querySelector('.head .title'))).toBe('Шпакловка финал');
        expect(Array.from(days()[1].querySelectorAll('.chip')).map((chip) => text(chip))).toEqual(['Гошо']);
        expect(days()[2].querySelector('.chips')).toBeNull();
    });

    it('shows the note', async () => {
        await render([testWorkDay({ note: 'Ключ при съседа\nвтори ред' })]);

        expect(days()[0].querySelector('.note')?.textContent).toBe('Ключ при съседа\nвтори ред');
    });

    it('„Редакция“ emits the day; hidden without canEdit', async () => {
        const day = testWorkDay();
        await render([day]);

        const button = Array.from(element.querySelectorAll('button')).find((b) => text(b) === 'Редакция')!;
        button.click();
        expect(edited).toEqual([day]);

        await render([day], false);
        expect(element.querySelector('button')).toBeNull();
    });
    it('read only for the portal: own empty text, and days without workers and note show neither', async () => {
        fixture.componentRef.setInput('emptyText', 'Още няма работни дни.');
        await render([], false);
        expect(text(element)).toBe('Още няма работни дни.');

        // A portal day: no id on the items, no workers, no note.
        const portalDay = {
            id: 'd1',
            date: '2026-11-16',
            title: 'Шпакловка',
            total: 6,
            items: [{ description: 'Боя', quantity: 2, unit: 'Piece', rate: 3, needsReview: false, amount: 6 }],
        } as unknown as WorkDay;
        await render([portalDay], false);

        expect(text(days()[0].querySelector('.desc'))).toBe('Боя');
        expect(days()[0].querySelector('.chips')).toBeNull();
        expect(days()[0].querySelector('.note')).toBeNull();
        expect(element.querySelectorAll('button').length).toBe(0);
    });
});
