import { formatNumber } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { EurPipe } from '../../shared/eur.pipe';
import { UNIT_LABELS } from '../../shared/units';
import { WorkDay, WorkItem } from '../work-day.model';

/** "12,5 м² × 6,50": quantity with a decimal comma, unit and price (only formatting, no sums). */
function calcLabel(item: WorkItem): string {
    const quantity = `${formatNumber(item.quantity, 'bg', '1.0-3')} ${UNIT_LABELS[item.unit]}`;
    return item.rate === null ? quantity : `${quantity} × ${formatNumber(item.rate, 'bg', '1.2-2')}`;
}

/** Short Bulgarian weekdays, Sunday first like Date.getDay() (same as the bg locale data). */
const WEEKDAYS = ['нд', 'пн', 'вт', 'ср', 'чт', 'пт', 'сб'];

/**
 * "2026-11-17" -> { day: "17.11", weekday: "вт" }. Plain string work instead of formatDate: formatDate would pull
 * Angular's date formatting into the initial bundle (shared common chunk).
 */
function dateLabels(value: string): { day: string; weekday: string } {
    const [year, month, day] = value.split('-');
    const weekday = new Date(Number(year), Number(month) - 1, Number(day)).getDay();
    return { day: `${day}.${month}`, weekday: WEEKDAYS[weekday] };
}

/** Dumb list of the work days of a site, newest first as given. Amounts come ready from the server. */
@Component({
    selector: 'atlas-work-day-list',
    imports: [EurPipe, NzButtonModule],
    templateUrl: './work-day-list.component.html',
    styleUrl: './work-day-list.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkDayListComponent {
    readonly days = input.required<WorkDay[]>();
    /** Shows „Редакция“ (Owner and Manager). */
    readonly canEdit = input(true);
    readonly edit = output<WorkDay>();

    protected readonly rows = computed(() =>
        this.days().map((day) => {
            const labels = dateLabels(day.date);
            return {
                day,
                dayLabel: labels.day,
                weekday: labels.weekday,
                items: day.items.map((item) => ({ item, calc: calcLabel(item) })),
            };
        }),
    );
}
