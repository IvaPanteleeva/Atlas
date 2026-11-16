export interface NavItem {
    path: string;
    label: string;
    icon: string;
}

/** Main tabs, in display order. Icons are ng-zorro outline icons registered via APP_ICONS (shared/icons.ts). */
export const NAV_ITEMS: readonly NavItem[] = [
    { path: '/sites', label: 'Обекти', icon: 'home' },
    { path: '/people', label: 'Хора', icon: 'team' },
    { path: '/tasks', label: 'Задачи', icon: 'check-square' },
    { path: '/rate-card', label: 'Цени', icon: 'tag' },
];
