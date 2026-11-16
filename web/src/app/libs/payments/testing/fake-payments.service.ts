import { Observable, of } from 'rxjs';
import { Payment, SavePaymentRequest } from '../payment.model';

/** Test data and a hand-written PaymentsService fake shared by the specs. */
export function testPayment(overrides: Partial<Payment> = {}): Payment {
    return { id: 'pay1', siteId: 's1', date: '2026-11-16', amount: 500, note: 'аванс', ...overrides };
}

export class FakePaymentsService {
    listResult: Observable<Payment[]> = of([]);
    createResult: Observable<Payment> | null = null;
    updateResult: Observable<Payment> | null = null;
    removeResult: Observable<void> = of(undefined);
    calls: string[] = [];
    lastRequest: SavePaymentRequest | null = null;

    list(siteId: string): Observable<Payment[]> {
        this.calls.push(`list ${siteId}`);
        return this.listResult;
    }

    create(siteId: string, request: SavePaymentRequest): Observable<Payment> {
        this.calls.push(`create ${siteId} ${request.date} ${request.amount}`);
        this.lastRequest = request;
        return this.createResult ?? of(testPayment({ id: 'new', siteId, ...request }));
    }

    update(siteId: string, id: string, request: SavePaymentRequest): Observable<Payment> {
        this.calls.push(`update ${siteId} ${id} ${request.date} ${request.amount}`);
        this.lastRequest = request;
        return this.updateResult ?? of(testPayment({ id, siteId, ...request }));
    }

    remove(siteId: string, id: string): Observable<void> {
        this.calls.push(`remove ${siteId} ${id}`);
        return this.removeResult;
    }
}
