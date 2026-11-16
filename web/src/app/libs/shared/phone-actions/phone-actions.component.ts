import {
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    ElementRef,
    inject,
    input,
    signal,
    viewChild,
} from '@angular/core';
import { NzButtonModule } from 'ng-zorro-antd/button';

const STATUS_MS = 3000;

/**
 * A phone number with „Обади се“ (tel: link, optional) and „Копирай“. When the clipboard is not available
 * (old browser, no permission) the number is selected so it can be copied by hand.
 */
@Component({
    selector: 'atlas-phone-actions',
    imports: [NzButtonModule],
    templateUrl: './phone-actions.component.html',
    styleUrl: './phone-actions.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhoneActionsComponent {
    readonly phone = input.required<string>();
    readonly showCall = input(true);

    private readonly phoneText = viewChild.required<ElementRef<HTMLElement>>('phoneText');
    private timer: ReturnType<typeof setTimeout> | undefined;

    /** Only digits and a leading + (spaces and dashes are for reading). */
    protected readonly telHref = computed(() => `tel:${this.phone().replace(/[^\d+]/g, '')}`);
    protected readonly status = signal<string | null>(null);

    constructor() {
        inject(DestroyRef).onDestroy(() => clearTimeout(this.timer));
    }

    protected async copy(): Promise<void> {
        try {
            await navigator.clipboard.writeText(this.phone());
            this.showStatus('Копирано');
        } catch {
            this.selectPhone();
            this.showStatus('Маркирано, копирай ръчно');
        }
    }

    private selectPhone(): void {
        const selection = window.getSelection();
        if (!selection) {
            return;
        }
        const range = document.createRange();
        range.selectNodeContents(this.phoneText().nativeElement);
        selection.removeAllRanges();
        selection.addRange(range);
    }

    private showStatus(message: string): void {
        clearTimeout(this.timer);
        this.status.set(message);
        this.timer = setTimeout(() => this.status.set(null), STATUS_MS);
    }
}
