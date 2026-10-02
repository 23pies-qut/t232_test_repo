export async function setUserContext(userName) {
    const response = await fetch('/api/UserContext/set-user',
        {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({userName})
        });
    if(!(response.ok)) throw new Error(`Set-user request failed (${response.status})`);
    return response.json();
}