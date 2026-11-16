import { fromIsoDate, isAfterToday, toIsoDate } from './iso-date';

describe('iso-date', () => {
    afterEach(() => vi.useRealTimers());

    it('toIsoDate uses the local date, not UTC', () => {
        expect(toIsoDate(new Date(2026, 10, 16, 0, 30))).toBe('2026-11-16');
        expect(toIsoDate(new Date(2026, 0, 5, 23, 59))).toBe('2026-01-05');
    });

    it('fromIsoDate gives local midnight', () => {
        const date = fromIsoDate('2026-11-03');

        expect([date.getFullYear(), date.getMonth(), date.getDate(), date.getHours()]).toEqual([2026, 10, 3, 0]);
    });

    it('isAfterToday is true only from tomorrow', () => {
        vi.useFakeTimers({ toFake: ['Date'] });
        vi.setSystemTime(new Date(2026, 10, 16, 23, 50));

        expect(isAfterToday(new Date(2026, 10, 16, 23, 59))).toBe(false);
        expect(isAfterToday(new Date(2026, 9, 1))).toBe(false);
        expect(isAfterToday(new Date(2026, 10, 17, 0, 1))).toBe(true);
    });
});
