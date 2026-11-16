import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { SITE_STATUS_LABELS, SiteStatus } from '../site.model';

/** Dumb status pill (prototype .pill): Lead muted, Active amber, Done green. */
@Component({
    selector: 'atlas-site-status-tag',
    template: `<span class="pill" [class]="'pill pill--' + status().toLowerCase()">{{ label() }}</span>`,
    styleUrl: './site-status-tag.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SiteStatusTagComponent {
    readonly status = input.required<SiteStatus>();

    protected readonly label = computed(() => SITE_STATUS_LABELS[this.status()]);
}
