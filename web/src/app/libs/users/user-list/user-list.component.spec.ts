import { ComponentFixture, TestBed } from '@angular/core/testing';
import { UserAccount } from '../user.model';
import { testUser } from '../testing/fake-users.service';
import { UserListComponent } from './user-list.component';

const users: UserAccount[] = [
    testUser({ id: 'u1', userName: 'iva', role: 'Owner' }),
    testUser({ id: 'u2', userName: 'pesho', role: 'Manager' }),
    testUser({ id: 'u3', userName: 'zheni', role: 'ClientViewer', companyId: 'c1', companyName: 'Женистрой' }),
];

describe('UserListComponent', () => {
    let fixture: ComponentFixture<UserListComponent>;
    let element: HTMLElement;
    let passwordFor: UserAccount[];
    let removed: UserAccount[];

    const text = (node: Element | null | undefined) => node?.textContent?.replace(/\s+/g, ' ').trim();
    const rows = () => Array.from(element.querySelectorAll<HTMLElement>('.user'));
    const button = (root: ParentNode, label: string) =>
        Array.from(root.querySelectorAll<HTMLButtonElement>('button')).find((b) => text(b) === label);

    beforeEach(async () => {
        fixture = TestBed.createComponent(UserListComponent);
        element = fixture.nativeElement as HTMLElement;
        passwordFor = [];
        removed = [];
        fixture.componentInstance.newPassword.subscribe((user) => passwordFor.push(user));
        fixture.componentInstance.remove.subscribe((user) => removed.push(user));
        fixture.componentRef.setInput('users', users);
        fixture.componentRef.setInput('currentUserId', 'u1');
        await fixture.whenStable();
    });

    it('shows the name (with „(ти)“ for the signed-in user), the role label and the company', () => {
        expect(rows().map((row) => text(row.querySelector('.user-name')))).toEqual(['iva (ти)', 'pesho', 'zheni']);
        expect(rows().map((row) => text(row.querySelector('.role')))).toEqual([
            'Администратор',
            'Управител',
            'Възложител · Женистрой',
        ]);
    });

    it('„Нова парола“ is on every row', () => {
        button(rows()[0], 'Нова парола')!.click();
        button(rows()[2], 'Нова парола')!.click();

        expect(passwordFor.map((user) => user.id)).toEqual(['u1', 'u3']);
    });

    it('the signed-in user has no „Изтрий“; the others need a second click', async () => {
        expect(button(rows()[0], 'Изтрий')).toBeUndefined();

        button(rows()[1], 'Изтрий')!.click();
        await fixture.whenStable();
        expect(removed).toEqual([]);
        expect(button(rows()[2], 'Потвърди изтриване')).toBeUndefined();

        button(rows()[1], 'Потвърди изтриване')!.click();

        expect(removed.map((user) => user.id)).toEqual(['u2']);
    });
});
