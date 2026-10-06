export async function getFilesList(token) {
    const response = await fetch('/api/GetFilesList', {
        headers: { 'Authorization:': 'Bearer ' + token }
    });
    if (!response.ok) throw new Error("Could not load files: " + response.status);
    return response.json();
}