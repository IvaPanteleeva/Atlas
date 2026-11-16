import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';
import { CompaniesService } from '../companies.service';
import { Company, SaveCompanyRequest } from '../company.model';
import { CompaniesStore } from './companies.store';

function httpError(status: number, error: unknown = null) {
    return throwError(() => new HttpErrorResponse({ status, error }));
}

/** Hand-written fake: each test sets what the next call returns. */
class FakeCompaniesService {
    listResult: Observable<Company[]> = of([]);
    createResult: Observable<Company> = of({ id: 'new', name: 'Нова' });
    updateResult: Observable<Company> = of({ id: 'a', name: 'Друга' });
    removeResult: Observable<void> = of(undefined);
    calls: string[] = [];

    list(): Observable<Company[]> {
        this.calls.push('list');
        return this.listResult;
    }

    create(request: SaveCompanyRequest): Observable<Company> {
        this.calls.push(`create ${request.name}`);
        return this.createResult;
    }

    update(id: string, request: SaveCompanyRequest): Observable<Company> {
        this.calls.push(`update ${id} ${request.name}`);
        return this.updateResult;
    }

    remove(id: string): Observable<void> {
        this.calls.push(`remove ${id}`);
        return this.removeResult;
    }
}

describe('CompaniesStore', () => {
    let fake: FakeCompaniesService;
    let store: InstanceType<typeof CompaniesStore>;

    const names = () => store.companies().map((c) => c.name);

    beforeEach(() => {
        fake = new FakeCompaniesService();
        TestBed.configureTestingModule({ providers: [{ provide: CompaniesService, useValue: fake }] });
        store = TestBed.inject(CompaniesStore);
    });

    async function loaded(companies: Company[]) {
        fake.listResult = of(companies);
        await store.load();
    }

    it('starts empty', () => {
        expect(store.companies()).toEqual([]);
        expect(store.loading()).toBe(false);
        expect(store.saving()).toBe(false);
        expect(store.error()).toBeNull();
    });

    it('load fills the list and clears loading', async () => {
        await loaded([
            { id: 'a', name: 'Алфа' },
            { id: 'b', name: 'Бета' },
        ]);

        expect(names()).toEqual(['Алфа', 'Бета']);
        expect(store.loading()).toBe(false);
        expect(store.error()).toBeNull();
    });

    it('load error sets a message and keeps loading false', async () => {
        fake.listResult = httpError(500);

        await store.load();

        expect(store.error()).toBe('Грешка при зареждане на фирмите');
        expect(store.loading()).toBe(false);
    });

    it('create sends the trimmed name, returns the company and keeps the list sorted (bg)', async () => {
        await loaded([
            { id: 'a', name: 'Алфа' },
            { id: 'v', name: 'Вега' },
        ]);
        fake.createResult = of({ id: 'b', name: 'Бета' });

        const created = await store.create('  Бета ');

        expect(created).toEqual({ id: 'b', name: 'Бета' });
        expect(fake.calls).toContain('create Бета');
        expect(names()).toEqual(['Алфа', 'Бета', 'Вега']);
        expect(store.saving()).toBe(false);
    });

    it('create 409 returns null and shows the ProblemDetails title', async () => {
        await loaded([{ id: 'a', name: 'Алфа' }]);
        fake.createResult = httpError(409, { title: 'Фирма с това име вече има', status: 409 });

        const created = await store.create('Алфа');

        expect(created).toBeNull();
        expect(store.error()).toBe('Фирма с това име вече има');
        expect(names()).toEqual(['Алфа']);
        expect(store.saving()).toBe(false);
    });

    it('create 400 shows the first validation message', async () => {
        fake.createResult = httpError(400, {
            title: 'One or more validation errors occurred.',
            errors: { name: ['Въведи име', 'Друго'] },
        });

        await store.create('x');

        expect(store.error()).toBe('Въведи име');
    });

    it('an error without a title falls back to a generic message', async () => {
        fake.createResult = httpError(500);

        await store.create('x');

        expect(store.error()).toBe('Грешка при записа');
    });

    it('a new save clears the previous error', async () => {
        fake.createResult = httpError(409, { title: 'Фирма с това име вече има' });
        await store.create('Алфа');
        fake.createResult = of({ id: 'b', name: 'Бета' });

        await store.create('Бета');

        expect(store.error()).toBeNull();
    });

    it('rename replaces the company and re-sorts the list', async () => {
        await loaded([
            { id: 'a', name: 'Алфа' },
            { id: 'b', name: 'Бета' },
        ]);
        fake.updateResult = of({ id: 'a', name: 'Гама' });

        const ok = await store.rename('a', ' Гама ');

        expect(ok).toBe(true);
        expect(fake.calls).toContain('update a Гама');
        expect(store.companies()).toEqual([
            { id: 'b', name: 'Бета' },
            { id: 'a', name: 'Гама' },
        ]);
    });

    it('rename 409 returns false and keeps the old name', async () => {
        await loaded([{ id: 'a', name: 'Алфа' }]);
        fake.updateResult = httpError(409, { title: 'Фирма с това име вече има' });

        const ok = await store.rename('a', 'Бета');

        expect(ok).toBe(false);
        expect(store.error()).toBe('Фирма с това име вече има');
        expect(names()).toEqual(['Алфа']);
    });

    it('remove deletes the company from the list', async () => {
        await loaded([
            { id: 'a', name: 'Алфа' },
            { id: 'b', name: 'Бета' },
        ]);

        const ok = await store.remove('a');

        expect(ok).toBe(true);
        expect(fake.calls).toContain('remove a');
        expect(names()).toEqual(['Бета']);
    });

    it('remove 409 keeps the company and shows "Фирмата се използва"', async () => {
        await loaded([{ id: 'a', name: 'Алфа' }]);
        fake.removeResult = httpError(409, { title: 'Фирмата се използва' });

        const ok = await store.remove('a');

        expect(ok).toBe(false);
        expect(store.error()).toBe('Фирмата се използва');
        expect(names()).toEqual(['Алфа']);
    });

    it('clearError hides the message', async () => {
        fake.removeResult = httpError(409, { title: 'Фирмата се използва' });
        await store.remove('a');

        store.clearError();

        expect(store.error()).toBeNull();
    });
});
