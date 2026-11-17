import { ANIMATION_MODULE_TYPE } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { Observable, of } from 'rxjs';
import { routes } from '../../../app.routes';
import { AuthStore } from '../../auth/+state/auth.store';
import { Me, Role } from '../../auth/auth.model';
import { AuthService } from '../../auth/auth.service';
import { APP_ICONS } from '../../shared/icons';
import { ServicesService } from '../services.service';
import { FakeServicesService, httpError } from '../testing/fake-services.service';

describe('ServicesPageComponent', () => {
    let fake: FakeServicesService;
    let role: Role;

    beforeAll(() => {
        Element.prototype.scrollTo ??= () => undefined;
    });

    beforeEach(() => {
        fake = new FakeServicesService();
        role = 'Owner';
        TestBed.configureTestingModule({
            providers: [
                provideRouter(routes, withComponentInputBinding()),
                provideNzIcons(APP_ICONS),
                // jsdom fires no animationend, so the modal would never finish closing.
                { provide: ANIMATION_MODULE_TYPE, useValue: 'NoopAnimations' },
                {
                    provide: AuthService,
                    useValue: {
                        me: (): Observable<Me> =>
                            of({ id: '1', userName: 'iva', role, companyId: null, personId: null }),
                    },
                },
                { provide: ServicesService, useValue: fake },
            ],
        });
    });

    const button = (root: ParentNode, label: string) =>
        Array.from(root.querySelectorAll<HTMLButtonElement>('button')).find(
            (candidate) => candidate.textContent?.trim() === label,
        );
    const sheet = () => document.querySelector<HTMLElement>('.atlas-sheet');
    const formInSheet = () => document.querySelector<HTMLElement>('.atlas-sheet atlas-service-form');
    const names = (page: HTMLElement) =>
        Array.from(page.querySelectorAll('.service .name')).map((n) => n.textContent?.trim());

    async function render() {
        await TestBed.inject(AuthStore).loadMe();
        const harness = await RouterTestingHarness.create();
        await harness.navigateByUrl('/services');
        await harness.fixture.whenStable();
        const element = harness.fixture.nativeElement as HTMLElement;
        return { harness, page: element.querySelector<HTMLElement>('atlas-services-page')! };
    }

    async function waitForForm() {
        return vi.waitFor(() => {
            const form = formInSheet();
            expect(form).toBeTruthy();
            return form!;
        });
    }

    function type(root: ParentNode, selector: string, value: string) {
        const input = root.querySelector<HTMLInputElement>(selector)!;
        input.value = value;
        input.dispatchEvent(new Event('input'));
    }

    it('shows the heading, the intro and the list', async () => {
        const { page } = await render();

        expect(page.querySelector('h1')?.textContent?.trim()).toBe('Услуги');
        expect(page.textContent).toContain('Общият списък с работи. Цените за конкретен обект се задават в обекта.');
        expect(fake.calls).toEqual(['list']);
        expect(names(page)).toEqual(['Боядисване', 'Шпакловка']);
        expect(document.title).toBe('Услуги · Атлас');
    });

    it('shows a load error', async () => {
        fake.listResult = httpError(500);

        const { page } = await render();

        expect(page.querySelector('nz-alert')?.textContent).toContain('Грешка при зареждане на услугите');
    });

    it('Owner creates a service from "Нова услуга"', async () => {
        const { harness, page } = await render();

        button(page, 'Нова услуга')!.click();
        await harness.fixture.whenStable();
        const form = await waitForForm();
        expect(sheet()?.textContent).toContain('Нова услуга');

        type(form, '#service-name', 'Грундиране');
        type(form, '#service-rate', '2.5');
        await harness.fixture.whenStable();
        button(form, 'Запази')!.click();
        await harness.fixture.whenStable();

        expect(fake.lastRequest).toEqual({ name: 'Грундиране', unit: 'SquareMeter', defaultRate: 2.5 });
        await vi.waitFor(() => expect(formInSheet()).toBeNull());
        expect(names(page)).toEqual(['Боядисване', 'Грундиране', 'Шпакловка']);
    });

    it('Owner edits a service by tapping its row', async () => {
        const { harness, page } = await render();

        page.querySelectorAll<HTMLButtonElement>('button.service')[1].click();
        await harness.fixture.whenStable();
        const form = await waitForForm();
        expect(form.querySelector<HTMLInputElement>('#service-name')?.value).toBe('Шпакловка');

        type(form, '#service-name', 'Шпакловка фина');
        await harness.fixture.whenStable();
        button(form, 'Запази')!.click();
        await harness.fixture.whenStable();

        expect(fake.calls).toContain('update sv2 Шпакловка фина');
        await vi.waitFor(() => expect(formInSheet()).toBeNull());
        expect(names(page)).toEqual(['Боядисване', 'Шпакловка фина']);
    });

    it('a 409 keeps the form open and shows the reason', async () => {
        fake.createResult = httpError(409, { title: 'Тази услуга вече я има' });
        const { harness, page } = await render();
        button(page, 'Нова услуга')!.click();
        await harness.fixture.whenStable();
        const form = await waitForForm();

        type(form, '#service-name', 'Шпакловка');
        await harness.fixture.whenStable();
        button(form, 'Запази')!.click();
        await harness.fixture.whenStable();

        expect(form.querySelector('nz-alert')?.textContent).toContain('Тази услуга вече я има');
        expect(formInSheet()).toBe(form);
    });

    it('Owner deletes after confirming; a refused delete (409) shows the reason', async () => {
        fake.removeResult = httpError(409, { title: 'Услугата има цени в 2 обекта' });
        const { harness, page } = await render();
        page.querySelectorAll<HTMLButtonElement>('button.service')[0].click();
        await harness.fixture.whenStable();
        const form = await waitForForm();

        button(form, 'Изтрий')!.click();
        await harness.fixture.whenStable();
        button(form, 'Потвърди изтриване')!.click();
        await harness.fixture.whenStable();

        expect(fake.calls).toContain('remove sv1');
        expect(form.querySelector('nz-alert')?.textContent).toContain('Услугата има цени в 2 обекта');
        expect(names(page)).toEqual(['Боядисване', 'Шпакловка']);

        fake.removeResult = of(undefined);
        button(form, 'Потвърди изтриване')!.click();
        await harness.fixture.whenStable();

        await vi.waitFor(() => expect(formInSheet()).toBeNull());
        expect(names(page)).toEqual(['Шпакловка']);
    });

    it('Manager adds and edits services but cannot delete one', async () => {
        role = 'Manager';
        const { harness, page } = await render();

        expect(button(page, 'Нова услуга')).toBeDefined();
        page.querySelectorAll<HTMLButtonElement>('button.service')[0].click();
        await harness.fixture.whenStable();
        const form = await waitForForm();

        expect(button(form, 'Запази')).toBeDefined();
        expect(button(form, 'Изтрий')).toBeUndefined();
    });
});
