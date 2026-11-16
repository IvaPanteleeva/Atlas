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
import { CompaniesService } from '../../companies/companies.service';
import { APP_ICONS } from '../../shared/icons';
import { FakeUsersService } from '../../users/testing/fake-users.service';
import { UsersService } from '../../users/users.service';
import { PeopleService } from '../people.service';
import { FakePeopleService, httpError } from '../testing/fake-people.service';

describe('PeoplePageComponent', () => {
    let people: FakePeopleService;
    let users: FakeUsersService;
    let companyCalls: number;
    let role: Role;

    beforeAll(() => {
        Element.prototype.scrollTo ??= () => undefined;
    });

    beforeEach(() => {
        people = new FakePeopleService();
        users = new FakeUsersService();
        companyCalls = 0;
        role = 'Owner';
        TestBed.configureTestingModule({
            providers: [
                provideRouter(routes, withComponentInputBinding()),
                provideNzIcons(APP_ICONS),
                // jsdom fires no animationend, so the modal would never finish closing.
                { provide: ANIMATION_MODULE_TYPE, useValue: 'NoopAnimations' },
                {
                    provide: AuthService,
                    useValue: { me: (): Observable<Me> => of({ id: 'u1', userName: 'iva', role, companyId: null }) },
                },
                { provide: PeopleService, useValue: people },
                { provide: UsersService, useValue: users },
                {
                    provide: CompaniesService,
                    useValue: {
                        list: () => {
                            companyCalls++;
                            return of([{ id: 'c1', name: 'Женистрой' }]);
                        },
                    },
                },
            ],
        });
    });

    afterEach(() => {
        document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => (overlay.innerHTML = ''));
    });

    const text = (node: Element | null | undefined) => node?.textContent?.replace(/\s+/g, ' ').trim();
    const button = (root: ParentNode, label: string) =>
        Array.from(root.querySelectorAll<HTMLButtonElement>('button')).find((b) => text(b) === label);
    const cards = (page: HTMLElement) => Array.from(page.querySelectorAll<HTMLElement>('atlas-person-card'));
    const cardNames = (page: HTMLElement) => cards(page).map((card) => text(card.querySelector('.name')));
    const inSheet = (selector: string) => document.querySelector<HTMLElement>(`.atlas-sheet ${selector}`);

    async function render() {
        await TestBed.inject(AuthStore).loadMe();
        const harness = await RouterTestingHarness.create();
        await harness.navigateByUrl('/people');
        await harness.fixture.whenStable();
        const element = harness.fixture.nativeElement as HTMLElement;
        return { harness, page: element.querySelector<HTMLElement>('atlas-people-page')! };
    }

    async function waitFor(selector: string) {
        return vi.waitFor(() => {
            const found = inSheet(selector);
            expect(found).toBeTruthy();
            return found!;
        });
    }

    async function type(root: ParentNode, selector: string, value: string) {
        const input = root.querySelector<HTMLInputElement>(selector)!;
        input.value = value;
        input.dispatchEvent(new Event('input'));
    }

    describe('as Owner', () => {
        it('shows the crew as cards, the contacts as a list and the profiles; sets the title', async () => {
            const { page } = await render();

            expect(text(page.querySelector('h1'))).toBe('Нашите хора');
            expect(document.title).toBe('Хора · Атлас');
            expect(cardNames(page)).toEqual(['Гошо', 'Пешо']);
            expect(text(cards(page)[1].querySelector('.note'))).toBe('Има кола');
            expect(text(page.querySelector('atlas-contact-list .name'))).toBe('Иван бояджия');
            expect(text(page.querySelector('atlas-contact-list .note'))).toBe('Само боя');
            expect(text(page.querySelector('#profiles-title'))).toBe('Профили за вход');
            expect(page.querySelectorAll('atlas-user-list .user').length).toBe(3);
            expect(people.calls).toEqual(['list']);
            expect(users.calls).toEqual(['list']);
        });

        it('„Човек в екипа“ opens the form with „Работи с нас“ on and adds a card', async () => {
            const { harness, page } = await render();

            button(page, 'Човек в екипа')!.click();
            await harness.fixture.whenStable();
            const form = await waitFor('atlas-person-form');
            expect(form.querySelector<HTMLInputElement>('label.crew input')!.checked).toBe(true);

            await type(form, '#person-name', 'Владо');
            await harness.fixture.whenStable();
            button(form, 'Запази')!.click();
            await harness.fixture.whenStable();

            expect(people.lastRequest).toEqual({ name: 'Владо', phone: null, note: null, isCrew: true });
            await vi.waitFor(() => expect(inSheet('atlas-person-form')).toBeNull());
            expect(cardNames(page)).toEqual(['Владо', 'Гошо', 'Пешо']);
        });

        it('„Контакт“ opens the form with „Работи с нас“ off', async () => {
            const { harness, page } = await render();

            button(page, 'Контакт')!.click();
            await harness.fixture.whenStable();
            const form = await waitFor('atlas-person-form');

            expect(form.querySelector<HTMLInputElement>('label.crew input')!.checked).toBe(false);
        });

        it('a refused delete (409) shows the reason in the form', async () => {
            people.removeResult = httpError(409, { title: 'Човекът има работни дни' });
            const { harness, page } = await render();

            button(cards(page)[0], 'Редакция')!.click();
            await harness.fixture.whenStable();
            const form = await waitFor('atlas-person-form');
            button(form, 'Изтрий')!.click();
            await harness.fixture.whenStable();
            button(form, 'Потвърди изтриване')!.click();
            await harness.fixture.whenStable();

            expect(people.calls).toContain('remove p1');
            expect(form.querySelector('nz-alert')?.textContent).toContain('Човекът има работни дни');
            expect(cardNames(page)).toEqual(['Гошо', 'Пешо']);
        });

        it('creates a client profile with a company', async () => {
            const { harness, page } = await render();

            button(page, 'Профил')!.click();
            await harness.fixture.whenStable();
            const form = await waitFor('atlas-user-form');
            expect(companyCalls).toBe(1);
            await type(form, '#user-name', 'zheni2');
            await type(form, '#user-password', '12345678');
            await harness.fixture.whenStable();
            form.querySelector<HTMLElement>('nz-select#user-role .ant-select-selector')!.click();
            await harness.fixture.whenStable();
            (
                await vi.waitFor(() => {
                    const option = Array.from(document.querySelectorAll<HTMLElement>('nz-option-item')).find(
                        (candidate) => text(candidate) === 'Възложител',
                    );
                    expect(option).toBeTruthy();
                    return option!;
                })
            ).click();
            await harness.fixture.whenStable();
            form.querySelector<HTMLElement>('nz-select#user-company .ant-select-selector')!.click();
            await harness.fixture.whenStable();
            (
                await vi.waitFor(() => {
                    const option = Array.from(document.querySelectorAll<HTMLElement>('nz-option-item')).find(
                        (candidate) => text(candidate) === 'Женистрой',
                    );
                    expect(option).toBeTruthy();
                    return option!;
                })
            ).click();
            await harness.fixture.whenStable();
            button(form, 'Запази')!.click();
            await harness.fixture.whenStable();

            expect(users.lastCreate).toEqual({
                userName: 'zheni2',
                password: '12345678',
                role: 'ClientViewer',
                companyId: 'c1',
            });
            await vi.waitFor(() => expect(inSheet('atlas-user-form')).toBeNull());
            expect(text(page.querySelector('atlas-user-list'))).toContain('zheni2');
        });

        it('a 409 on create keeps the form open with the message', async () => {
            users.createResult = httpError(409, { title: 'Има такъв потребител' });
            const { harness, page } = await render();
            button(page, 'Профил')!.click();
            await harness.fixture.whenStable();
            const form = await waitFor('atlas-user-form');

            await type(form, '#user-name', 'pesho');
            await type(form, '#user-password', '12345678');
            await harness.fixture.whenStable();
            button(form, 'Запази')!.click();
            await harness.fixture.whenStable();

            expect(form.querySelector('nz-alert')?.textContent).toContain('Има такъв потребител');
            expect(inSheet('atlas-user-form')).toBe(form);
        });

        it('„Нова парола“ sends the password for that profile', async () => {
            const { harness, page } = await render();
            const row = page.querySelectorAll<HTMLElement>('atlas-user-list .user')[1];

            button(row, 'Нова парола')!.click();
            await harness.fixture.whenStable();
            const form = await waitFor('atlas-password-form');
            expect(text(document.querySelector('.atlas-sheet .ant-modal-title'))).toBe('Нова парола за pesho');
            await type(form, '#new-password', '87654321');
            await harness.fixture.whenStable();
            button(form, 'Запази')!.click();
            await harness.fixture.whenStable();

            expect(users.calls).toContain('password u2');
            expect(users.lastPassword).toEqual({ password: '87654321' });
            await vi.waitFor(() => expect(inSheet('atlas-password-form')).toBeNull());
            expect(text(page.querySelector('.profiles .notice'))).toBe('Паролата на pesho е сменена.');
        });

        it('no „Изтрий“ for yourself; a refused delete (409) shows the reason above the list', async () => {
            users.removeResult = httpError(409, { title: 'Трябва да остане поне един Owner.' });
            const { harness, page } = await render();
            const rows = () => Array.from(page.querySelectorAll<HTMLElement>('atlas-user-list .user'));

            expect(text(rows()[0])).toContain('iva (ти)');
            expect(button(rows()[0], 'Изтрий')).toBeUndefined();

            button(rows()[1], 'Изтрий')!.click();
            await harness.fixture.whenStable();
            button(rows()[1], 'Потвърди изтриване')!.click();
            await harness.fixture.whenStable();

            expect(users.calls).toContain('remove u2');
            expect(page.querySelector('.profiles nz-alert')?.textContent).toContain(
                'Трябва да остане поне един Owner.',
            );
            expect(rows().length).toBe(3);
        });
    });

    describe('as Manager', () => {
        beforeEach(() => {
            role = 'Manager';
        });

        it('sees notes and may add and edit, but has no profiles section and no /api/users request', async () => {
            const { page } = await render();

            expect(cardNames(page)).toEqual(['Гошо', 'Пешо']);
            expect(text(cards(page)[1].querySelector('.note'))).toBe('Има кола');
            expect(text(page.querySelector('atlas-contact-list .note'))).toBe('Само боя');
            expect(button(page, 'Човек в екипа')).toBeDefined();
            expect(button(page, 'Контакт')).toBeDefined();
            expect(page.querySelector('#profiles-title')).toBeNull();
            expect(page.querySelector('atlas-user-list')).toBeNull();
            expect(users.calls).toEqual([]);
            expect(companyCalls).toBe(0);
        });

        it('edits a person but sees no delete', async () => {
            const { harness, page } = await render();

            button(cards(page)[0], 'Редакция')!.click();
            await harness.fixture.whenStable();
            const form = await waitFor('atlas-person-form');

            expect(button(form, 'Запази')).toBeDefined();
            expect(button(form, 'Изтрий')).toBeUndefined();
        });

        it('shows the empty states', async () => {
            people.listResult = of([]);

            const { page } = await render();

            expect(text(page.querySelector('.crew-empty'))).toBe('Няма хора в екипа.');
            expect(text(page.querySelector('atlas-contact-list'))).toBe('Няма контакти.');
        });
    });
});
