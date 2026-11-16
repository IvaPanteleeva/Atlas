import { EnvironmentProviders, inject, Injector, provideAppInitializer } from '@angular/core';

/**
 * ng-zorro texts in Bulgarian (bg_BG), set once before the app starts.
 * Same result as provideNzI18n(bg_BG), but ng-zorro's i18n module is loaded with import(): a static import in
 * app.config would keep the whole module (date helpers, date-fns, Angular's formatDate) in the initial bundle,
 * because the date picker in the lazy work day form uses them.
 */
export function provideZorroLocale(): EnvironmentProviders {
    return provideAppInitializer(() => {
        const injector = inject(Injector);
        return import('ng-zorro-antd/i18n').then(({ NzI18nService, bg_BG }) =>
            injector.get(NzI18nService).setLocale(bg_BG),
        );
    });
}
