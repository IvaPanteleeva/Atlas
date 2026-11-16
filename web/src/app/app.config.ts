import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import bg from '@angular/common/locales/bg';
import {
    ApplicationConfig,
    inject,
    LOCALE_ID,
    provideAppInitializer,
    provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { bg_BG, provideNzI18n } from 'ng-zorro-antd/i18n';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { routes } from './app.routes';
import { AuthStore } from './libs/auth/+state/auth.store';
import { authInterceptor } from './libs/auth/auth.interceptor';
import { APP_ICONS } from './libs/shared/icons';

registerLocaleData(bg);

export const appConfig: ApplicationConfig = {
    providers: [
        provideBrowserGlobalErrorListeners(),
        // One GET /api/auth/me before the first navigation, so the guards know who is signed in.
        // The loader in index.html stays until this resolves.
        provideAppInitializer(() => inject(AuthStore).loadMe()),
        provideRouter(routes, withComponentInputBinding()),
        provideHttpClient(withFetch(), withInterceptors([authInterceptor])),
        { provide: LOCALE_ID, useValue: 'bg' },
        provideNzI18n(bg_BG),
        provideNzIcons(APP_ICONS),
    ],
};
