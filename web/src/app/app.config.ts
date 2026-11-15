import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withFetch } from '@angular/common/http';
import bg from '@angular/common/locales/bg';
import { ApplicationConfig, LOCALE_ID, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { bg_BG, provideNzI18n } from 'ng-zorro-antd/i18n';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { routes } from './app.routes';
import { NAV_ICONS } from './libs/shared/layout/nav-items';

registerLocaleData(bg);

export const appConfig: ApplicationConfig = {
    providers: [
        provideBrowserGlobalErrorListeners(),
        provideRouter(routes, withComponentInputBinding()),
        provideHttpClient(withFetch()),
        { provide: LOCALE_ID, useValue: 'bg' },
        provideNzI18n(bg_BG),
        provideNzIcons(NAV_ICONS),
    ],
};
