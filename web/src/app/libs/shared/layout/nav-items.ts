import { CheckSquareOutline, HomeOutline, TagOutline, TeamOutline } from '@ant-design/icons-angular/icons';

export interface NavItem {
    path: string;
    label: string;
    icon: string;
}

/** Main tabs, in display order. Icons are ng-zorro outline icons registered in app.config.ts. */
export const NAV_ITEMS: readonly NavItem[] = [
    { path: '/sites', label: 'Обекти', icon: 'home' },
    { path: '/people', label: 'Хора', icon: 'team' },
    { path: '/tasks', label: 'Задачи', icon: 'check-square' },
    { path: '/rate-card', label: 'Цени', icon: 'tag' },
];

/** Icons used by NAV_ITEMS, passed to provideNzIcons in app.config.ts (and in tests). */
export const NAV_ICONS = [HomeOutline, TeamOutline, CheckSquareOutline, TagOutline];
