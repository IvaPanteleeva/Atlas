import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NAV_ITEMS } from '../nav-items';

@Component({
    selector: 'atlas-shell',
    imports: [RouterOutlet, RouterLink, RouterLinkActive, NzIconModule],
    templateUrl: './shell.component.html',
    styleUrl: './shell.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShellComponent {
    protected readonly navItems = NAV_ITEMS;
}
