import { SiteListItem } from '../sites/site.model';
import { SiteTask } from './task.model';

/** Open tasks of one site in „Задачи“. */
export interface SiteTaskGroup {
    siteId: string;
    siteName: string;
    tasks: SiteTask[];
}

/**
 * The open tasks of each site that has any, the sites in the order of the sites list (the API order).
 * A site missing from the list (e.g. not loaded yet) goes last. The tasks keep their order.
 */
export function groupOpenBySite(tasks: SiteTask[], sites: SiteListItem[]): SiteTaskGroup[] {
    const groups = new Map<string, SiteTask[]>();
    for (const task of tasks) {
        if (task.siteId && !task.done) {
            groups.set(task.siteId, [...(groups.get(task.siteId) ?? []), task]);
        }
    }
    const position = new Map(sites.map((site, index) => [site.id, index]));
    const names = new Map(sites.map((site) => [site.id, site.name]));
    return [...groups.entries()]
        .map(([siteId, siteTasks]) => ({ siteId, siteName: names.get(siteId) ?? 'Обект', tasks: siteTasks }))
        .sort((a, b) => (position.get(a.siteId) ?? sites.length) - (position.get(b.siteId) ?? sites.length));
}
