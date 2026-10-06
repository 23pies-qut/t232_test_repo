// Calls the backend FilesController. Every request sends Authorization: Bearer <JWT>.
import { authHeader, networkError, parseResponse, request } from './apiClient.js';

/** GET /api/Files -> files of the current tenant */
export const listFiles = (token) => request('/Files', { token });

/** GET /api/Files/progress/{uploadId} -> server-side steps the upload has completed */
export const getUploadProgress = (uploadId, token) => request(`/Files/progress/${uploadId}`, { token });

/** DELETE /api/Files/{id} -> soft delete */
export const deleteFile = (fileId, token) => request(`/Files/${fileId}`, { method: 'DELETE', token });

/** POST /api/Files (multipart/form-data). Uses XMLHttpRequest so upload progress can be reported via onProgress(0..1). */
export function uploadFile(file, token, onProgress, uploadId) {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    xhr.open('POST', '/api/Files');
    xhr.setRequestHeader('Authorization', `Bearer ${token}`);
    xhr.setRequestHeader('X-Upload-Id', uploadId);
    xhr.upload.onprogress = (e) => e.lengthComputable && onProgress(e.loaded / e.total);
    xhr.onload = () => {
      try {
        resolve(parseResponse(xhr.status, xhr.responseText));
      } catch (error) {
        reject(error);
      }
    };
    xhr.onerror = () => reject(networkError());
    const form = new FormData();
    form.append('file', file);
    xhr.send(form);
  });
}

/** GET /api/Files/{id} -> the decrypted file (Blob) */
export async function downloadFile(fileId, token) {
  let response;
  try {
    response = await fetch(`/api/Files/${fileId}`, { headers: authHeader(token) });
  } catch {
    throw networkError();
  }
  if (!response.ok) parseResponse(response.status, await response.text()); // always throws
  return response.blob();
}

/** Makes the browser save a Blob under the given file name. */
export function saveBlob(blob, filename) {
  const url = URL.createObjectURL(blob);
  const link = Object.assign(document.createElement('a'), { href: url, download: filename });
  link.click();
  URL.revokeObjectURL(url);
}
