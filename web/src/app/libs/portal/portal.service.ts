import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { PortalSite, PortalSiteListItem, PortalSites } from './portal.model';

/**
 * Copies only the fields the portal types declare. The server already leaves out notes, people and pay; this is a
 * second guard so that an extra field by mistake (e.g. `workers` or `note`) never reaches the shared components,
 * which would show it.
 */
function pickSite(site: PortalSite): PortalSite {
    const { id, name, endCustomer, address, mapUrl, status, summary, workDays, payments } = site;
    return {
        id,
        name,
        endCustomer,
        address,
        mapUrl,
        status,
        summary: {
            earned: summary.earned,
            paid: summary.paid,
            remaining: summary.remaining,
            openCount: summary.openCount,
        },
        workDays: workDays.map(({ id, date, title, total, items }) => ({
            id,
            date,
            title,
            total,
            items: items.map(({ description, quantity, unit, rate, needsReview, amount }) => ({
                description,
                quantity,
                unit,
                rate,
                needsReview,
                amount,
            })),
        })),
        payments: payments.map(({ date, amount }) => ({ date, amount })),
    };
}

function pickListItem(site: PortalSiteListItem): PortalSiteListItem {
    const { id, name, endCustomer, status, earned, paid, remaining, openCount } = site;
    return { id, name, endCustomer, status, earned, paid, remaining, openCount };
}

/** Read-only portal endpoints for the client. The server filters by the client's company on every call. */
@Injectable({ providedIn: 'root' })
export class PortalService {
    private readonly http = inject(HttpClient);

    sites(): Observable<PortalSites> {
        return this.http.get<PortalSites>('/portal/sites').pipe(
            map(({ sites, totals }) => ({
                sites: sites.map(pickListItem),
                totals: {
                    owed: totals.owed,
                    overpaid: totals.overpaid,
                    overpaidSiteCount: totals.overpaidSiteCount,
                },
            })),
        );
    }

    site(id: string): Observable<PortalSite> {
        return this.http.get<PortalSite>(`/portal/sites/${id}`).pipe(map(pickSite));
    }
}
