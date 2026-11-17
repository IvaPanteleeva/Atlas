import { Location } from '@angular/common';
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
import { PeopleService } from '../../people/people.service';
import { FakePeopleService } from '../../people/testing/fake-people.service';
import { APP_ICONS } from '../../shared/icons';
import { SitesService } from '../../sites/sites.service';
import { FakeSitesService } from '../../sites/testing/fake-sites.service';
import { TasksService } from '../tasks.service';
import { FakeTasksService, testTask } from '../testing/fake-tasks.service';

describe('TasksPageComponent', () => {
    let tasks: FakeTasksService;
    let sites: FakeSitesService;
    let role: Role;
    let personId: string | null;

    beforeAll(() => {
        Element.prototype.scrollTo ??= () => undefined;
    });

    beforeEach(() => {
        tasks = new FakeTasksService();
        tasks.tasks = [
            testTask({ id: 'c1', text: 'Фактури', siteId: null, createdAt: '2026-11-09T08:00:00Z' }),
            testTask({ id: 'a', text: 'Лепило', siteId: 's2', createdAt: '2026-11-10T08:00:00Z' }),
            testTask({ id: 'b', text: 'Плочки', siteId: 's1', assigneeId: 'p1', assigneeName: 'Гошо' }),
            testTask({ id: 'c2', text: 'Офис', siteId: null, done: true, doneAt: '2026-11-15T08:00:00Z' }),
            testTask({ id: 'd', text: 'Кофи', siteId: 's1', done: true, doneAt: '2026-11-14T08:00:00Z' }),
        ];
        sites = new FakeSitesService();
        role = 'Owner';
        personId = null;
        TestBed.configureTestingModule({
            providers: [
                provideRouter(routes, withComponentInputBinding()),
                provideNzIcons(APP_ICONS),
                { provide: ANIMATION_MODULE_TYPE, useValue: 'NoopAnimations' },
                {
                    provide: AuthService,
                    useValue: {
                        me: (): Observable<Me> => of({ id: 'u1', userName: 'iva', role, companyId: null, personId }),
                    },
                },
                { provide: TasksService, useValue: tasks },
                { provide: SitesService, useValue: sites },
                { provide: PeopleService, useValue: new FakePeopleService() },
            ],
        });
    });

    afterEach(() => {
        document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => (overlay.innerHTML = ''));
    });

    const text = (node: Element | null | undefined) => node?.textContent?.replace(/\s+/g, ' ').trim();
    const chips = (page: HTMLElement) => Array.from(page.querySelectorAll<HTMLButtonElement>('.filters button'));
    const chip = (page: HTMLElement, label: string) => chips(page).find((candidate) => text(candidate) === label)!;
    const sectionTexts = (section: Element | null) =>
        Array.from(section?.querySelectorAll('li.task .text') ?? []).map((node) => text(node));

    async function render() {
        await TestBed.inject(AuthStore).loadMe();
        const harness = await RouterTestingHarness.create();
        await harness.navigateByUrl('/tasks');
        await harness.fixture.whenStable();
        const page = (harness.fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('atlas-tasks-page')!;
        return { harness, page };
    }

    it('loads every task and shows „Фирмата“ with its done ones, then the sites in list order (open only)', async () => {
        const { page } = await render();

        expect(document.title).toBe('Задачи · Атлас');
        expect(tasks.calls).toEqual(['list']);
        expect(text(page.querySelector('h1'))).toBe('Задачи');

        const company = page.querySelector('section.company');
        expect(text(company?.querySelector('h2'))).toContain('Фирмата');
        expect(sectionTexts(company)).toEqual(['Фактури', 'Офис']);
        expect(company?.querySelector('atlas-task-add')).toBeTruthy();

        const siteSections = Array.from(page.querySelectorAll('section.site'));
        expect(siteSections.map((section) => text(section.querySelector('h2')))).toEqual(['Бункера', 'Професора']);
        expect(sectionTexts(siteSections[0])).toEqual(['Плочки']);
        expect(sectionTexts(siteSections[1])).toEqual(['Лепило']);
        expect(siteSections[0].querySelector('atlas-task-add')).toBeNull();
        const link = siteSections[0].querySelector<HTMLAnchorElement>('a.to-site')!;
        expect(text(link)).toBe('Към обекта →');
        expect(link.getAttribute('href')).toBe('/sites/s1');
    });

    it('filters: „Всички“ and the crew; no „Мои“ without a linked person', async () => {
        const { harness, page } = await render();

        expect(chips(page).map((candidate) => text(candidate))).toEqual(['Всички', 'Гошо', 'Пешо']);
        expect(chip(page, 'Всички').getAttribute('aria-pressed')).toBe('true');

        chip(page, 'Пешо').click();
        await harness.fixture.whenStable();

        expect(tasks.calls).toEqual(['list', 'list assigneeId=p3']);
        expect(chip(page, 'Пешо').getAttribute('aria-pressed')).toBe('true');
        expect(chip(page, 'Всички').getAttribute('aria-pressed')).toBe('false');
    });

    it('„Мои“ uses the person of my profile (and that person has no second chip)', async () => {
        personId = 'p1';
        const { harness, page } = await render();

        expect(chips(page).map((candidate) => text(candidate))).toEqual(['Всички', 'Мои', 'Пешо']);
        chip(page, 'Мои').click();
        await harness.fixture.whenStable();

        expect(tasks.calls).toEqual(['list', 'list assigneeId=p1']);
    });

    it('adds a company task (with the person of the filter) and clears the field', async () => {
        const { harness, page } = await render();
        chip(page, 'Пешо').click();
        await harness.fixture.whenStable();

        const input = page.querySelector<HTMLInputElement>('section.company input.new-text')!;
        input.value = 'Да се плати тока';
        input.dispatchEvent(new Event('input'));
        page.querySelector<HTMLFormElement>('section.company form')!.dispatchEvent(new Event('submit'));
        await harness.fixture.whenStable();

        expect(tasks.lastCreate).toEqual({ text: 'Да се плати тока', siteId: null, assigneeId: 'p3' });
        expect(sectionTexts(page.querySelector('section.company'))).toContain('Да се плати тока');
        expect(page.querySelector<HTMLInputElement>('section.company input.new-text')!.value).toBe('');
    });

    it('ticking a site task takes it off the site section', async () => {
        const { harness, page } = await render();

        page.querySelector<HTMLElement>('section.site [nz-checkbox]')!.click();
        await harness.fixture.whenStable();

        expect(tasks.calls).toContain('done b true');
        expect(Array.from(page.querySelectorAll('section.site h2')).map((node) => text(node))).toEqual(['Професора']);
    });

    it('✕ is for Owner only', async () => {
        const owner = await render();
        expect(owner.page.querySelector('button.remove')).toBeTruthy();
    });

    it('Manager has no ✕', async () => {
        role = 'Manager';
        const { page } = await render();

        expect(page.querySelector('section.company li.task')).toBeTruthy();
        expect(page.querySelector('button.remove')).toBeNull();
    });

    it('„Към обекта →“ opens the site', async () => {
        const { harness, page } = await render();

        page.querySelector<HTMLAnchorElement>('section.site a.to-site')!.click();
        await harness.fixture.whenStable();

        expect(TestBed.inject(Location).path()).toBe('/sites/s1');
    });
});
