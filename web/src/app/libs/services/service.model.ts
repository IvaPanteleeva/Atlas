import { Unit } from '../shared/units';

/** A row of GET /api/services: a kind of work with its unit and an optional default price. */
export interface Service {
    id: string;
    name: string;
    unit: Unit;
    /** Used when the site has no price of its own; null = no default price. */
    defaultRate: number | null;
}

/** Body for POST /api/services and PUT /api/services/{id}. */
export interface SaveServiceRequest {
    name: string;
    unit: Unit;
    defaultRate: number | null;
}

/** Same limit as the API. */
export const SERVICE_NAME_MAX_LENGTH = 200;

/** Price limits shared by the service and site price forms (the API checks the same). */
export const MONEY_MIN = 0.01;
export const MONEY_MAX = 100000;
