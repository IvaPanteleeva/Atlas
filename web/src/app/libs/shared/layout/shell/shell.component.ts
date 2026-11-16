import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NzDropdownModule } from 'ng-zorro-antd/dropdown';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzMenuModule } from 'ng-zorro-antd/menu';
import { AuthStore } from '../../../auth/+state/auth.store';
import { homeUrlFor } from '../../../auth/auth.guards';
import { NAV_ITEMS, NavItem } from '../nav-items';

/** Layout route for signed-in users: header with tabs and user menu, the page in the outlet. */
@Component({
    selector: 'atlas-shell',
    imports: [RouterOutlet, RouterLink, RouterLinkActive, NzIconModule, NzDropdownModule, NzMenuModule],
    templateUrl: './shell.component.html',
    styleUrl: './shell.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShellComponent {
    protected readonly store = inject(AuthStore);
    private readonly router = inject(Router);

    /** The client only has the portal for now, so no internal tabs. */
    protected readonly navItems = computed<readonly NavItem[]>(() => (this.store.isClient() ? [] : NAV_ITEMS));
    protected readonly homeUrl = computed(() => homeUrlFor(this.store.role()));

    protected async logout(): Promise<void> {
        await this.store.logout();
        await this.router.navigateByUrl('/login');
    }
}
