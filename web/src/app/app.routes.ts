import { Routes } from '@angular/router';

const placeholderPage = () =>
    import('./libs/shared/placeholder-page/placeholder-page.component').then((m) => m.PlaceholderPageComponent);

export const routes: Routes = [
    { path: '', pathMatch: 'full', redirectTo: 'sites' },
    { path: 'sites', title: 'Обекти · Атлас', data: { title: 'Обекти' }, loadComponent: placeholderPage },
    { path: 'people', title: 'Хора · Атлас', data: { title: 'Хора' }, loadComponent: placeholderPage },
    { path: 'tasks', title: 'Задачи · Атлас', data: { title: 'Задачи' }, loadComponent: placeholderPage },
    { path: 'rate-card', title: 'Цени · Атлас', data: { title: 'Цени' }, loadComponent: placeholderPage },
    { path: '**', redirectTo: 'sites' },
];
