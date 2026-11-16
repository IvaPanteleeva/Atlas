/** A company that gives us work (e.g. ЖЕНИСТРОЙ ЕООД), as returned by /api/companies. */
export interface Company {
    id: string;
    name: string;
}

/** Body for POST /api/companies and PUT /api/companies/{id}. */
export interface SaveCompanyRequest {
    name: string;
}

/** Same limit as the API (CompanyConfiguration.NameMaxLength). */
export const COMPANY_NAME_MAX_LENGTH = 200;
