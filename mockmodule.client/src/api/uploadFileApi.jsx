export async function uploadFile(file, token) {
    const formData = new FormData();
    formData.append('file', file);

    const response = await fetch('/api/UploadFiles',
        {
            method: 'POST',
            headers: {
                'Authorization': 'Bearer ' + token
            },
            body: formData
        });
    if (!(response.ok)) throw new Error(`File upload failed (${response.status})`);
    return response.json();
}