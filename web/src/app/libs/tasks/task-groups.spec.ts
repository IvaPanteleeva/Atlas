import { SiteListItem } from '../sites/site.model';
import { groupOpenBySite } from './task-groups';
import { testTask } from './testing/fake-tasks.service';

function site(id: string, name: string): SiteListItem {
    return { id, name, status: 'Active', companyName: null, endCustomer: null, address: null, lastWorkDate: null };
}

describe('groupOpenBySite', () => {
    it('groups the open site tasks in the order of the sites list; company and done tasks stay out', () => {
        const tasks = [
            testTask({ id: 'a', siteId: 's2' }),
            testTask({ id: 'b', siteId: null }),
            testTask({ id: 'c', siteId: 's1' }),
            testTask({ id: 'd', siteId: 's2' }),
            testTask({ id: 'e', siteId: 's3', done: true }),
            testTask({ id: 'f', siteId: 'gone' }),
        ];

        const groups = groupOpenBySite(tasks, [site('s1', 'Бункера'), site('s2', 'Професора'), site('s3', 'Вила')]);

        expect(groups.map((group) => [group.siteName, group.tasks.map((task) => task.id)])).toEqual([
            ['Бункера', ['c']],
            ['Професора', ['a', 'd']],
            ['Обект', ['f']],
        ]);
    });
});
