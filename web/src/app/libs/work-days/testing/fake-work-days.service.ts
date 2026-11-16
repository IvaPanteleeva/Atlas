import { Observable, of } from 'rxjs';
import { SaveWorkDayRequest, WorkDay, WorkItem } from '../work-day.model';

/** Test data and a hand-written WorkDaysService fake shared by the specs. */
export function testWorkItem(overrides: Partial<WorkItem> = {}): WorkItem {
    return {
        id: 'i1',
        serviceId: 'sv1',
        description: 'Шпакловка',
        quantity: 12.5,
        unit: 'SquareMeter',
        rate: 6.5,
        needsReview: false,
        amount: 81.25,
        ...overrides,
    };
}

export function testWorkDay(overrides: Partial<WorkDay> = {}): WorkDay {
    return {
        id: 'd1',
        siteId: 's1',
        date: '2026-11-16',
        title: 'Шпакловка финал',
        note: null,
        items: [testWorkItem()],
        total: 81.25,
        openCount: 0,
        ...overrides,
    };
}

/** Echoes the request back as a saved day (amounts are not computed: the server does that). */
function echo(siteId: string, id: string, request: SaveWorkDayRequest): WorkDay {
    return testWorkDay({
        id,
        siteId,
        date: request.date,
        title: request.title,
        note: request.note,
        items: request.items.map((item, index) =>
            testWorkItem({ ...item, id: item.id ?? `${id}-i${index}`, amount: null }),
        ),
        total: 0,
    });
}

export class FakeWorkDaysService {
    listResult: Observable<WorkDay[]> = of([]);
    createResult: Observable<WorkDay> | null = null;
    updateResult: Observable<WorkDay> | null = null;
    removeResult: Observable<void> = of(undefined);
    calls: string[] = [];
    lastRequest: SaveWorkDayRequest | null = null;

    list(siteId: string): Observable<WorkDay[]> {
        this.calls.push(`list ${siteId}`);
        return this.listResult;
    }

    create(siteId: string, request: SaveWorkDayRequest): Observable<WorkDay> {
        this.calls.push(`create ${siteId} ${request.date}`);
        this.lastRequest = request;
        return this.createResult ?? of(echo(siteId, 'new', request));
    }

    update(siteId: string, id: string, request: SaveWorkDayRequest): Observable<WorkDay> {
        this.calls.push(`update ${siteId} ${id} ${request.date}`);
        this.lastRequest = request;
        return this.updateResult ?? of(echo(siteId, id, request));
    }

    remove(siteId: string, id: string): Observable<void> {
        this.calls.push(`remove ${siteId} ${id}`);
        return this.removeResult;
    }
}
