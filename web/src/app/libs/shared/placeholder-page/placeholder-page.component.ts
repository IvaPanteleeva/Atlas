import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Shared "coming soon" page for tabs that are not built yet. The title comes from route data. */
@Component({
    selector: 'atlas-placeholder-page',
    templateUrl: './placeholder-page.component.html',
    styleUrl: './placeholder-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlaceholderPageComponent {
    readonly title = input.required<string>();
}
