export type Role = 'Owner' | 'Crew' | 'ClientViewer';

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
