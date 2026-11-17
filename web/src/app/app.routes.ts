import { Routes } from '@angular/router';
import { authGuard, clientGuard, guestGuard, staffGuard } from './libs/auth/auth.guards';

export const routes: Routes = [
    {
        path: 'login',
        title: 'Вход · Атлас',
        canMatch: [guestGuard],
        loadComponent: () => import('./libs/auth/login-page/login-page.component').then((m) => m.LoginPageComponent),
    },
    {
        // Layout route: everything inside the shell needs a signed-in user.
        path: '',
        loadComponent: () => import('./libs/shared/layout/shell/shell.component').then((m) => m.ShellComponent),
        canActivate: [authGuard],
        children: [
            { path: '', pathMatch: 'full', redirectTo: 'sites' },
            {
                path: 'sites',
                title: 'Обекти · Атлас',
                canActivate: [staffGuard],
                loadComponent: () =>
                    import('./libs/sites/sites-page/sites-page.component').then((m) => m.SitesPageComponent),
            },
            {
                path: 'sites/:id',
                title: 'Обект · Атлас',
                canActivate: [staffGuard],
                loadComponent: () =>
                    import('./libs/sites/site-detail-page/site-detail-page.component').then(
                        (m) => m.SiteDetailPageComponent,
                    ),
            },
            {
                path: 'people',
                title: 'Хора · Атлас',
                canActivate: [staffGuard],
                loadComponent: () =>
                    import('./libs/people/people-page/people-page.component').then((m) => m.PeoplePageComponent),
            },
            {
                path: 'tasks',
                title: 'Задачи · Атлас',
                canActivate: [staffGuard],
                loadComponent: () =>
                    import('./libs/tasks/tasks-page/tasks-page.component').then((m) => m.TasksPageComponent),
            },
            {
                path: 'services',
                title: 'Услуги · Атлас',
                canActivate: [staffGuard],
                loadComponent: () =>
                    import('./libs/services/services-page/services-page.component').then(
                        (m) => m.ServicesPageComponent,
                    ),
            },
            // The tab used to be "Цени" (/rate-card); old links and bookmarks still work.
            { path: 'rate-card', redirectTo: 'services' },
            {
                path: 'companies',
                title: 'Фирми · Атлас',
                canActivate: [staffGuard],
                loadComponent: () =>
                    import('./libs/companies/companies-page/companies-page.component').then(
                        (m) => m.CompaniesPageComponent,
                    ),
            },
            {
                path: 'portal',
                title: 'Обекти · Атлас',
                canActivate: [clientGuard],
                loadComponent: () =>
                    import('./libs/portal/portal-sites-page/portal-sites-page.component').then(
                        (m) => m.PortalSitesPageComponent,
                    ),
            },
            {
                // Not /portal/sites/:id: that is the API path (GET /portal/sites/{id}), so a reload there would get
                // JSON from the API instead of the app (production serves both from one origin).
                path: 'portal/:id',
                title: 'Обект · Атлас',
                canActivate: [clientGuard],
                loadComponent: () =>
                    import('./libs/portal/portal-site-page/portal-site-page.component').then(
                        (m) => m.PortalSitePageComponent,
                    ),
            },
        ],
    },
    { path: '**', redirectTo: '' },
];
