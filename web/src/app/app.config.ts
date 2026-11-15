import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withFetch } from '@angular/common/http';
import bg from '@angular/common/locales/bg';
import { ApplicationConfig, LOCALE_ID, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { PlusOutline } from '@ant-design/icons-angular/icons';
import { bg_BG, provideNzI18n } from 'ng-zorro-antd/i18n';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { routes } from './app.routes';

registerLocaleData(bg);

export const appConfig: ApplicationConfig = {
    providers: [
        provideBrowserGlobalErrorListeners(),
        provideRouter(routes),
        provideHttpClient(withFetch()),
        { provide: LOCALE_ID, useValue: 'bg' },
        provideNzI18n(bg_BG),
        provideNzIcons([PlusOutline]),
    ],
};
