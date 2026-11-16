import { formatNumber, registerLocaleData } from '@angular/common';
import bg from '@angular/common/locales/bg';
import { Pipe, PipeTransform } from '@angular/core';

// Money is always shown the Bulgarian way, whatever LOCALE_ID says (app.config also registers bg).
registerLocaleData(bg);

/**
 * Money from the API as "1 157,89 €" (locale bg, always two decimals, non-breaking spaces).
 * Angular's formatNumber is used instead of Intl: Intl skips grouping for four-digit amounts in bg ("1157,89").
 * Only formatting; the amounts themselves come ready from the server.
 */
@Pipe({ name: 'eur' })
export class EurPipe implements PipeTransform {
    transform(value: number | null | undefined): string {
        if (value === null || value === undefined) {
            return '';
        }
        return `${formatNumber(value, 'bg', '1.2-2')} €`;
    }
}
