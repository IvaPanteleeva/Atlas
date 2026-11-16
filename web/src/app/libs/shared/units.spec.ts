import { UNIT_LABELS, UNIT_OPTIONS } from './units';

describe('units', () => {
    it('has the Bulgarian labels', () => {
        expect(UNIT_LABELS).toEqual({ LinearMeter: 'м.л.', SquareMeter: 'м²', Day: 'ден', Piece: 'бр.' });
    });

    it('lists the options in a fixed order', () => {
        expect(UNIT_OPTIONS).toEqual([
            { value: 'LinearMeter', label: 'м.л.' },
            { value: 'SquareMeter', label: 'м²' },
            { value: 'Day', label: 'ден' },
            { value: 'Piece', label: 'бр.' },
        ]);
    });
});
