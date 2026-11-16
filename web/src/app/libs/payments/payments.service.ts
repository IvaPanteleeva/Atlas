import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Payment, SavePaymentRequest } from './payment.model';

/** Client payments of one site. Owner only (the API answers 403 to Crew). */
@Injectable({ providedIn: 'root' })
export class PaymentsService {
    private readonly http = inject(HttpClient);

    list(siteId: string): Observable<Payment[]> {
        return this.http.get<Payment[]>(`/api/sites/${siteId}/payments`);
    }

    create(siteId: string, request: SavePaymentRequest): Observable<Payment> {
        return this.http.post<Payment>(`/api/sites/${siteId}/payments`, request);
    }

    update(siteId: string, id: string, request: SavePaymentRequest): Observable<Payment> {
        return this.http.put<Payment>(`/api/sites/${siteId}/payments/${id}`, request);
    }

    remove(siteId: string, id: string): Observable<void> {
        return this.http.delete<void>(`/api/sites/${siteId}/payments/${id}`);
    }
}
