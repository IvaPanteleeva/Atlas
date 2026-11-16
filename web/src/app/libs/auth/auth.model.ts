/** Owner = „Администратор“ (everything), Manager = „Управител“ (all but deleting and login profiles), ClientViewer = „Възложител“. */
export type Role = 'Owner' | 'Manager' | 'ClientViewer';

/** The signed-in user, as returned by GET /api/auth/me and POST /api/auth/login. */
export interface Me {
    id: string;
    userName: string;
    role: Role;
    companyId: string | null;
}

export interface LoginRequest {
    userName: string;
    password: string;
}
