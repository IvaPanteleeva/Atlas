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
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { routes } from './app.routes';
import { AuthStore } from './libs/auth/+state/auth.store';
import { authInterceptor } from './libs/auth/auth.interceptor';
import { APP_ICONS } from './libs/shared/icons';
import { provideZorroLocale } from './libs/shared/zorro-locale';

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
        // ng-zorro locale bg_BG (loaded lazily, see zorro-locale.ts).
        provideZorroLocale(),
        provideNzIcons(APP_ICONS),
    ],
};
