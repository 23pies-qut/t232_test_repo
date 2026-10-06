// =============================================================================
// apiClient - request helpers shared by all api/*Api.jsx files. Paths are relative (/api/...); in development Vite proxies them to the backend.
// Failures throw ApiError (with the code / message / trace returned by the server).
// =============================================================================

export class ApiError extends Error {
  constructor(status, code, message, trace = []) {
    super(message);
    Object.assign(this, { status, code, trace });
  }
}

export const networkError = () => new ApiError(0, 'NETWORK', 'Cannot reach the backend. Make sure MockModule.Server is running.');
export const authHeader = (token) => (token ? { Authorization: `Bearer ${token}` } : {});

/** Returns the parsed JSON for 2xx (null for 204); otherwise throws ApiError. */
export function parseResponse(status, text) {
  let data = null;
  try {
    data = text ? JSON.parse(text) : null;
  } catch {
    /* not JSON */
  }
  if (status >= 200 && status < 300) return data;
  if (status === 401) throw new ApiError(401, 'UNAUTHORIZED', 'The token is invalid or has expired. Please choose a user again.');
  if (status === 403 && !data) throw new ApiError(403, 'FORBIDDEN', 'The token does not have permission for this action.');
  throw new ApiError(status, data?.code || `HTTP_${status}`, data?.message || `Request failed (${status}).`, data?.trace || []);
}

/** Sends a JSON request and returns the parsed JSON response. */
export async function request(path, { token, method = 'GET', json } = {}) {
  let response;
  try {
    response = await fetch('/api' + path, {
      method,
      headers: { ...authHeader(token), ...(json ? { 'Content-Type': 'application/json' } : {}) },
      body: json ? JSON.stringify(json) : undefined
    });
  } catch {
    throw networkError();
  }
  return parseResponse(response.status, await response.text());
}
