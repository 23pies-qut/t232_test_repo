// Calls the backend UserContextController (standalone mode: choose a test user, get a JWT)
import { request } from './apiClient.js';

/** GET /api/UserContext/users -> test users and their tenants */
export const getUsers = () => request('/UserContext/users');

/** POST /api/UserContext/set-user -> JWT. Also used to switch tenant (issues a new token). */
export const setUserContext = (userId, tenantId) =>
  request('/UserContext/set-user', { method: 'POST', json: { userId, tenantId } });
