import { Provider } from '@angular/core';
import {
    NativeDateAdapter,
    NZ_DATE_CONFIG,
    NZ_DATE_CONFIG_DEFAULT,
    NZ_DATE_LOCALE,
    NzDateAdapter,
} from 'ng-zorro-antd/core/time';

/**
 * Date adapter for nz-date-picker (ng-zorro 22 needs one): native Date, Bulgarian names, weeks start on Monday.
 * Same as provideNzNativeDateAdapter({ locale: 'bg', firstDayOfWeek: 1 }), but as component providers, so the
 * adapter code stays in the lazy chunk of the component that has a date picker instead of the initial bundle.
 */
export const DATE_PICKER_PROVIDERS: Provider[] = [
    { provide: NZ_DATE_LOCALE, useValue: 'bg' },
    { provide: NZ_DATE_CONFIG, useValue: { ...NZ_DATE_CONFIG_DEFAULT, firstDayOfWeek: 1 } },
    NativeDateAdapter,
    { provide: NzDateAdapter, useExisting: NativeDateAdapter },
];
