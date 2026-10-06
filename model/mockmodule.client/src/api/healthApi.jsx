// Calls the backend HealthController
import { request } from './apiClient.js';

/** GET /api/Health → { ok, message } */
export const getHealth = () => request('/Health');
