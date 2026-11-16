import { formatNumber, registerLocaleData } from '@angular/common';
import bg from '@angular/common/locales/bg';
import { Directive, ElementRef, forwardRef, inject, input } from '@angular/core';
import {
    AbstractControl,
    ControlValueAccessor,
    NG_VALIDATORS,
    NG_VALUE_ACCESSOR,
    ValidationErrors,
    Validator,
} from '@angular/forms';

// Numbers are always shown the Bulgarian way, whatever LOCALE_ID says (as in eur.pipe).
registerLocaleData(bg);

/** A number with an optional sign and one decimal comma or point ("10,5", "7.5", ",5"); spaces are ignored. */
const DECIMAL = /^-?(\d+([.,]\d*)?|[.,]\d+)$/;

/** Parsed text: a number, null for empty, undefined for text that is not a number. */
function parse(text: string): number | null | undefined {
    const compact = text.replace(/\s/g, '');
    if (!compact) {
        return null;
    }
    return DECIMAL.test(compact) ? Number(compact.replace(',', '.')) : undefined;
}

/** Rounds half away from zero to the given decimals without binary surprises (1.005 -> 1.01). */
function round(value: number, decimals: number): number {
    return Number(`${Math.round(Number(`${Math.abs(value)}e${decimals}`))}e-${decimals}`) * Math.sign(value) || 0;
}

/**
 * Number field with a Bulgarian decimal comma, for quantities and money: `<input nz-input atlasDecimalInput>`.
 * While typing it accepts "10,5" and "10.5" and emits the number (null when empty). On blur it rounds to `decimals`
 * and shows "10,5" (or "7,50" with fixedDecimals for money) — never "10.500", which reads as ten thousand in Bulgarian.
 * Text that is not a number stays on screen, the value is null and the control gets the error `number`.
 */
@Directive({
    selector: 'input[atlasDecimalInput]',
    providers: [
        { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => DecimalInputDirective), multi: true },
        { provide: NG_VALIDATORS, useExisting: forwardRef(() => DecimalInputDirective), multi: true },
    ],
    host: {
        inputmode: 'decimal',
        autocomplete: 'off',
        '(input)': 'onInput()',
        '(blur)': 'onBlur()',
    },
})
export class DecimalInputDirective implements ControlValueAccessor, Validator {
    /** Decimals kept on blur: 3 for quantities, 2 for money. */
    readonly decimals = input(2);
    /** Always show all decimals ("6,00"), for money. */
    readonly fixedDecimals = input(false);

    private readonly element = inject<ElementRef<HTMLInputElement>>(ElementRef).nativeElement;
    private value: number | null = null;
    private invalidText = false;
    private onChange: (value: number | null) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    writeValue(value: number | null | undefined): void {
        this.value = value ?? null;
        this.invalidText = false;
        this.element.value = this.format(this.value);
    }

    registerOnChange(fn: (value: number | null) => void): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    setDisabledState(disabled: boolean): void {
        this.element.disabled = disabled;
    }

    validate(_control: AbstractControl): ValidationErrors | null {
        return this.invalidText ? { number: true } : null;
    }

    protected onInput(): void {
        const parsed = parse(this.element.value);
        this.invalidText = parsed === undefined;
        this.value = parsed ?? null;
        this.onChange(this.value);
    }

    protected onBlur(): void {
        if (!this.invalidText && this.value !== null) {
            const rounded = round(this.value, this.decimals());
            if (rounded !== this.value) {
                this.value = rounded;
                this.onChange(rounded);
            }
            this.element.value = this.format(rounded);
        }
        this.onTouched();
    }

    private format(value: number | null): string {
        if (value === null) {
            return '';
        }
        const decimals = this.decimals();
        const digits = `1.${this.fixedDecimals() ? decimals : 0}-${decimals}`;
        return formatNumber(value, 'bg', digits);
    }
}
