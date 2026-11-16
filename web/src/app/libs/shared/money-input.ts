import { afterNextRender, Directive, ElementRef, inject } from '@angular/core';

/**
 * Parser for nz-input-number that accepts a decimal comma ("6,5"), as typed on a Bulgarian keyboard.
 * The default parser drops commas as thousand separators, so "6,5" would become 65.
 */
export function parseDecimal(value: string): number {
    const normalized = value.replace(/\s/g, '').replace(',', '.');
    return normalized ? Number(normalized) : NaN;
}

/** Opens the numeric keyboard with a decimal key on phones (nz-input-number has no inputmode input). */
@Directive({ selector: 'nz-input-number[atlasDecimalKeyboard]' })
export class DecimalKeyboardDirective {
    constructor() {
        const host = inject<ElementRef<HTMLElement>>(ElementRef);
        afterNextRender(() => host.nativeElement.querySelector('input')?.setAttribute('inputmode', 'decimal'));
    }
}
