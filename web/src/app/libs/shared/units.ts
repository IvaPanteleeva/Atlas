/** Unit of a service or work item; the same enum names as the API. */
export type Unit = 'LinearMeter' | 'SquareMeter' | 'Day' | 'Piece';

/** Short Bulgarian labels used everywhere in the UI. */
export const UNIT_LABELS: Record<Unit, string> = {
    LinearMeter: 'м.л.',
    SquareMeter: 'м²',
    Day: 'ден',
    Piece: 'бр.',
};

/** Options for the unit select, in display order. */
export const UNIT_OPTIONS: readonly { value: Unit; label: string }[] = (
    ['LinearMeter', 'SquareMeter', 'Day', 'Piece'] as const
).map((value) => ({ value, label: UNIT_LABELS[value] }));
