import { TestBed } from '@angular/core/testing';
import { EurPipe } from './eur.pipe';

/** Non-breaking space: between the thousands and before the € sign. */
const NBSP = ' ';

describe('EurPipe', () => {
    let pipe: EurPipe;

    beforeEach(() => {
        // No LOCALE_ID 'bg' here on purpose: money is always formatted the Bulgarian way.
        TestBed.configureTestingModule({ providers: [EurPipe] });
        pipe = TestBed.inject(EurPipe);
    });

    it('formats 1157.89 as "1 157,89 €" with non-breaking spaces', () => {
        expect(pipe.transform(1157.89)).toBe(`1${NBSP}157,89${NBSP}€`);
    });

    it('always shows two decimals', () => {
        expect(pipe.transform(6.5)).toBe(`6,50${NBSP}€`);
        expect(pipe.transform(100000)).toBe(`100${NBSP}000,00${NBSP}€`);
        expect(pipe.transform(0.01)).toBe(`0,01${NBSP}€`);
    });

    it('returns an empty string for null and undefined', () => {
        expect(pipe.transform(null)).toBe('');
        expect(pipe.transform(undefined)).toBe('');
    });
});
