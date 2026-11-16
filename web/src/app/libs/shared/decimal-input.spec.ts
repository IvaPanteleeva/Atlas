import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { DecimalInputDirective } from './decimal-input';

@Component({
    imports: [ReactiveFormsModule, DecimalInputDirective],
    template: `<input atlasDecimalInput [formControl]="control" [decimals]="decimals()" [fixedDecimals]="fixed()" />`,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class HostComponent {
    readonly control = new FormControl<number | null>(null);
    readonly decimals = signal(3);
    readonly fixed = signal(false);
}

describe('DecimalInputDirective', () => {
    async function setup(options: { decimals?: number; fixed?: boolean; value?: number | null } = {}) {
        const fixture = TestBed.createComponent(HostComponent);
        const host = fixture.componentInstance;
        host.decimals.set(options.decimals ?? 3);
        host.fixed.set(options.fixed ?? false);
        if (options.value !== undefined) {
            host.control.setValue(options.value);
        }
        await fixture.whenStable();
        const input = (fixture.nativeElement as HTMLElement).querySelector('input')!;
        const type = async (text: string) => {
            input.value = text;
            input.dispatchEvent(new Event('input'));
            await fixture.whenStable();
        };
        const blur = async () => {
            input.dispatchEvent(new Event('blur'));
            await fixture.whenStable();
        };
        return { fixture, control: host.control, input, type, blur };
    }

    it('opens the decimal keyboard on phones', async () => {
        const { input } = await setup();

        expect(input.getAttribute('inputmode')).toBe('decimal');
    });

    it('accepts a decimal comma and a decimal point while typing', async () => {
        const { control, input, type } = await setup();

        await type('10,5');
        expect(control.value).toBe(10.5);
        expect(input.value).toBe('10,5');

        await type('7.5');
        expect(control.value).toBe(7.5);
    });

    it('on blur rounds to the decimals and shows a Bulgarian comma without trailing zeros', async () => {
        const { control, input, type, blur } = await setup({ decimals: 3 });

        await type('10.5');
        await blur();
        expect(input.value).toBe('10,5');

        await type('12,1254');
        await blur();
        expect(control.value).toBe(12.125);
        expect(input.value).toBe('12,125');

        await type('2');
        await blur();
        expect(input.value).toBe('2');
    });

    it('money (fixedDecimals) always shows two decimals', async () => {
        const { control, input, type, blur } = await setup({ decimals: 2, fixed: true });

        await type('7,5');
        await blur();
        expect(control.value).toBe(7.5);
        expect(input.value).toBe('7,50');

        await type('6');
        await blur();
        expect(input.value).toBe('6,00');

        await type('1,005');
        await blur();
        expect(control.value).toBe(1.01);
    });

    it('formats values written by the form the same way', async () => {
        const quantity = await setup({ value: 2 });
        expect(quantity.input.value).toBe('2');

        const money = await setup({ decimals: 2, fixed: true, value: 6.5 });
        expect(money.input.value).toBe('6,50');

        money.control.setValue(null);
        await money.fixture.whenStable();
        expect(money.input.value).toBe('');
    });

    it('empty is null; text that is not a number is null, stays on screen and is an error', async () => {
        const { control, input, type, blur } = await setup({ value: 3 });

        await type('');
        expect(control.value).toBeNull();
        expect(control.valid).toBe(true);

        await type('12,5а');
        expect(control.value).toBeNull();
        expect(control.hasError('number')).toBe(true);
        await blur();
        expect(input.value).toBe('12,5а');

        await type('1,5');
        expect(control.hasError('number')).toBe(false);
    });

    it('follows the disabled state of the control', async () => {
        const { fixture, control, input } = await setup({ value: 6.5, decimals: 2, fixed: true });

        control.disable();
        await fixture.whenStable();
        expect(input.disabled).toBe(true);
        expect(input.value).toBe('6,50');

        control.enable();
        await fixture.whenStable();
        expect(input.disabled).toBe(false);
    });
});
