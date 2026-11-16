/** Local date as yyyy-MM-dd (toISOString would be UTC and can be the previous day). */
export function toIsoDate(date: Date): string {
    const pad = (value: number) => String(value).padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

/** yyyy-MM-dd as a local date (midnight). */
export function fromIsoDate(value: string): Date {
    const [year, month, day] = value.split('-').map(Number);
    return new Date(year, month - 1, day);
}

/** For nzDisabledDate: days after today cannot be picked (the server refuses them too). */
export function isAfterToday(current: Date): boolean {
    return toIsoDate(current) > toIsoDate(new Date());
}
