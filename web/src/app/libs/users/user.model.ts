import { Role } from '../auth/auth.model';

/** A row of GET /api/users (Owner only): a login profile. */
export interface UserAccount {
    id: string;
    userName: string;
    role: Role;
    /** Only for ClientViewer: the company whose portal they see. */
    companyId: string | null;
    companyName: string | null;
}

/** Body for POST /api/users. companyId is required for ClientViewer and null otherwise. */
export interface CreateUserRequest {
    userName: string;
    password: string;
    role: Role;
    companyId: string | null;
}

/** Body for PUT /api/users/{id}/password. */
export interface SetPasswordRequest {
    password: string;
}

/** Same as the API (Identity options). */
export const PASSWORD_MIN_LENGTH = 8;

/** Role names in the UI. */
export const ROLE_LABELS: Record<Role, string> = {
    Owner: 'Администратор',
    Manager: 'Управител',
    ClientViewer: 'Възложител',
};

export const ROLE_OPTIONS: { value: Role; label: string }[] = (['Owner', 'Manager', 'ClientViewer'] as const).map(
    (value) => ({ value, label: ROLE_LABELS[value] }),
);
