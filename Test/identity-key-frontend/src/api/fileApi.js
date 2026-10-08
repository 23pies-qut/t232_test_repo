// Empty base keeps requests on the same origin (Vite proxies /api to the API).
const API_BASE_URL = "";

// Tenant and member IDs that are sent with every request.
const DEFAULT_TENANT_ID =
  "00000000000000000000000000000001";

const DEFAULT_MEMBER_ID =
  "00000000000000000000000000000001";

// Build the headers every API call needs.
function getRequestHeaders(additionalHeaders = {}) {
  return {
    "X-Tenant-ID": DEFAULT_TENANT_ID,
    "X-Member-ID": DEFAULT_MEMBER_ID,
    ...additionalHeaders,
  };
}

// Turn a failed response into a readable error message.
async function getErrorMessage(response) {
  try {
    const errorData = await response.json();

    return (
      errorData.message ||
      errorData.error ||
      `Request failed with status ${response.status}.`
    );
  } catch {
    return `Request failed with status ${response.status}.`;
  }
}

// Throw for a non-2xx response, otherwise return the JSON body.
async function handleJsonResponse(response) {
  if (!response.ok) {
    const message = await getErrorMessage(response);
    throw new Error(message);
  }

  return response.json();
}

// GET the list of files that belong to the current tenant.
export async function getFiles() {
  const response = await fetch(
    `${API_BASE_URL}/api/files`,
    {
      method: "GET",
      headers: getRequestHeaders({
        Accept: "application/json",
      }),
    }
  );

  const data = await handleJsonResponse(response);

  return Array.isArray(data) ? data : [];
}

// POST one file; the backend encrypts it before storing.
export async function uploadFile(file) {
  const formData = new FormData();

  formData.append("file", file);

  const response = await fetch(
    `${API_BASE_URL}/api/files`,
    {
      method: "POST",
      headers: getRequestHeaders(),
      body: formData,
    }
  );

  return handleJsonResponse(response);
}

// GET the decrypted content of a file, plus its original filename.
export async function downloadFile(fileId) {
  const response = await fetch(
    `${API_BASE_URL}/api/files/${fileId}/download`,
    {
      method: "GET",
      headers: getRequestHeaders(),
    }
  );

  if (!response.ok) {
    const message = await getErrorMessage(response);
    throw new Error(message);
  }

  const fileBlob = await response.blob();

  return {
    blob: fileBlob,
    fileName: getDownloadFileName(
      response.headers.get("Content-Disposition")
    ),
  };
}

// DELETE a file (the server marks it as deleted).
export async function deleteFile(fileId) {
  const response = await fetch(
    `${API_BASE_URL}/api/files/${fileId}`,
    {
      method: "DELETE",
      headers: getRequestHeaders({
        Accept: "application/json",
      }),
    }
  );

  return handleJsonResponse(response);
}

// Save downloaded data to the user's disk.
export function saveDownloadedFile(blob, fileName) {
  const downloadUrl = URL.createObjectURL(blob);

  const link = document.createElement("a");

  link.href = downloadUrl;
  link.download = fileName || "download";
  link.style.display = "none";

  document.body.appendChild(link);
  link.click();
  link.remove();

  URL.revokeObjectURL(downloadUrl);
}

// Read the filename out of the Content-Disposition response header.
function getDownloadFileName(contentDisposition) {
  if (!contentDisposition) {
    return "download";
  }

  const utf8Match = contentDisposition.match(
    /filename\*=UTF-8''([^;]+)/
  );

  if (utf8Match?.[1]) {
    return decodeURIComponent(utf8Match[1]);
  }

  const regularMatch = contentDisposition.match(
    /filename="?([^"]+)"?/
  );

  if (regularMatch?.[1]) {
    return regularMatch[1];
  }

  return "download";
}
