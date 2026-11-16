import { Routes } from '@angular/router';
import { authGuard, clientGuard, guestGuard, ownerGuard, staffGuard } from './libs/auth/auth.guards';

const placeholderPage = () =>
    import('./libs/shared/placeholder-page/placeholder-page.component').then((m) => m.PlaceholderPageComponent);

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
                data: { title: 'Хора' },
                canActivate: [staffGuard],
                loadComponent: placeholderPage,
            },
            {
                path: 'tasks',
                title: 'Задачи · Атлас',
                data: { title: 'Задачи' },
                canActivate: [staffGuard],
                loadComponent: placeholderPage,
            },
            {
                path: 'rate-card',
                title: 'Цени · Атлас',
                data: { title: 'Цени' },
                canActivate: [staffGuard],
                loadComponent: placeholderPage,
            },
            {
                path: 'companies',
                title: 'Фирми · Атлас',
                canActivate: [ownerGuard],
                loadComponent: () =>
                    import('./libs/companies/companies-page/companies-page.component').then(
                        (m) => m.CompaniesPageComponent,
                    ),
            },
            {
                path: 'portal',
                title: 'Портал · Атлас',
                data: { title: 'Порталът идва скоро' },
                canActivate: [clientGuard],
                loadComponent: placeholderPage,
            },
        ],
    },
    { path: '**', redirectTo: '' },
];
