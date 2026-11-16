import { ApplicationInitStatus } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NzI18nService } from 'ng-zorro-antd/i18n';
import { provideZorroLocale } from './zorro-locale';

describe('provideZorroLocale', () => {
    it('sets the ng-zorro locale to bg_BG before the app starts', async () => {
        TestBed.configureTestingModule({ providers: [provideZorroLocale()] });

        await TestBed.inject(ApplicationInitStatus).donePromise;

        const i18n = TestBed.inject(NzI18nService);
        expect(i18n.getLocaleId()).toBe('bg');
        expect(i18n.translate('Modal.okText')).toBe('Добре');
    });
});
