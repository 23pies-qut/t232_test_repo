
/* ========================================
   FILE UPLOAD FUNCTIONALITY
   ======================================== */

// Find the file input and file table in HTML
const fileInput = document.getElementById("fileInput");
const fileTable = document.querySelector(".file-table");
let authToken = null;

//for profile navigation to show tenant isolation in demo
const accountButton = document.getElementById("accountButton");
const accountDropdown = document.getElementById("accountDropdown");
const accountOptions = document.querySelectorAll(".account-option");

// Open or close the account dropdown
accountButton.addEventListener("click", () => {
    const isOpening = accountDropdown.hidden;

    accountDropdown.hidden = !isOpening;
    accountButton.setAttribute("aria-expanded", String(isOpening));
});

/* ========================================
   Account Switch
   ======================================== */

// Switch to the selected test account
async function switchAccount(userName) {
    try {
        // Check that a username was provided
        console.log("Switching account:", userName);

        if (!userName) {
            throw new Error("No username was provided.");
        }

        // Build the request body for the API
        const requestBody = {
            UserName: userName
        };

        console.log("Request body:", JSON.stringify(requestBody));

        // Request a new token for the selected account
        const response = await fetch("/api/UserContext/set-user", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(requestBody)
        });

        // Handle an unsuccessful API response
        if (!response.ok) {
            const errorDetails = await response.text();

            console.error(
                "Account switch failed:",
                response.status,
                errorDetails
            );

            throw new Error(
                `Login failed: ${response.status} - ${errorDetails}`
            );
        }

        // Read the response containing the new token
        const loginData = await response.json();

        if (!loginData.token) {
            throw new Error("The API did not return a token.");
        }

        // Replace the current token with the selected user's token
        authToken = loginData.token;

        console.log("New token received for:", userName);

        // Update the checkmark in the account dropdown
        accountOptions.forEach(option => {
            const checkmark = option.querySelector(".account-check");

            if (checkmark) {
                checkmark.hidden =
                    option.dataset.username !== userName;
            }
        });

        // Close the dropdown
        accountDropdown.hidden = true;
        accountButton.setAttribute("aria-expanded", "false");

        // Reload the files using the new user's token
        await loadFilesFromAPI(authToken);

        console.log(`Successfully switched to ${userName}`);

    } catch (error) {
        console.error("Could not switch account:", error);

        alert(
            `Could not switch account to ${userName || "the selected user"}. ` +
            "Check the browser console for details."
        );
    }
}
// Handle clicks on Andy and Bob
accountOptions.forEach(option => {
    option.addEventListener("click", () => {
        switchAccount(option.dataset.username);
    });
});


//This section: Waits for you to select one or more files.
//Checks that the authentication token is available.
//Sends each file to your existing uploadFileToAPI() function.
//Displays a message when the API acknowledges receipt, or an error if the request fails.
//Resets the file picker so you can select the same file again.

// Listen for when the user selects files
fileInput.addEventListener("change", async function () {

    // Get the selected files
    const selectedFiles = fileInput.files;

    // Check that we have an authentication token
    if (!authToken) {
        alert("Please wait for the file module to finish loading.");
        fileInput.value = "";
        return;
    }

    // Send each selected file to the API
    for (const file of selectedFiles) {
        try {
            const result = await uploadFileToAPI(file, authToken);

            alert(`The API received "${file.name}".`);
        } catch (error) {
            console.error("Upload failed:", error);
            alert(`Could not upload "${file.name}". Check the Console for details.`);
        }
    }

    // Allow the same file to be selected again
    fileInput.value = "";

    // Reload the file list from the API
    await loadFilesFromAPI(authToken);
});


/* ========================================
   ADD A FILE TO THE TABLE
   ======================================== */

function addFileToTable(file) {

    const displayName = file.name.length > 15
        ? file.name.substring(0, 15) + "..."
        : file.name;

    // Create a new row
    const fileRow = document.createElement("div");
    fileRow.className = "file-row";
    fileRow.dataset.fileId = file.fileId;
    fileRow.dataset.fileName = file.name;
  

    // Determine which SVG icon to display
    const fileIcon = getFileIcon(file.name);

    // Get the file's current date and time
    const modifiedDate = file.modifiedDate
        ? new Date(file.modifiedDate).toLocaleString("en-AU")
        : new Date().toLocaleString("en-AU");

    // Convert file size into a readable format
    const fileSize = formatFileSize(file.size);

    // Create the row contents
    fileRow.innerHTML = `
    <div class="select-column">
        <input type="checkbox" class="file-checkbox">
    </div>

    <div class="name-column file-name">
        <img src="${fileIcon}"
             alt=""
             class="file-type-icon">

        <span class="filename">${displayName}</span>
    </div>

    <div class="modified-column">
        ${modifiedDate}
    </div>

    <div class="size-column">
        ${fileSize}
    </div>

    <button class="manage-button" type="button">
        •••
    </button>
`;

    // Add the new row to the bottom of the table
    fileTable.appendChild(fileRow);
}


/* ========================================
   SELECT FILE TYPE ICON
   ======================================== */

function getFileIcon(fileName) {

    const extension = fileName.split(".").pop().toLowerCase();

    if (["jpg", "jpeg", "png", "gif", "webp"].includes(extension)) {
        return "assets/icons/image-file.svg";
    }

    if (["mp3", "wav", "ogg"].includes(extension)) {
        return "assets/icons/music-file.svg";
    }

    if (["mp4", "mov", "avi"].includes(extension)) {
        return "assets/icons/video-file.svg";
    }

    if (["doc", "docx", "pdf", "txt"].includes(extension)) {
        return "assets/icons/doc-file.svg";
    }

    // Default icon for OBJ and miscellaneous files
    return "assets/icons/file.svg";
}


/* ========================================
   FORMAT FILE SIZE
   ======================================== */

function formatFileSize(bytes) {

    if (bytes < 1024) {
        return bytes + " B";
    }

    if (bytes < 1024 * 1024) {
        return (bytes / 1024).toFixed(1) + " KB";
    }

    if (bytes < 1024 * 1024 * 1024) {
        return (bytes / (1024 * 1024)).toFixed(1) + " MB";
    }

    return (bytes / (1024 * 1024 * 1024)).toFixed(1) + " GB";
}

/* ========================================
   LOAD FILES FROM THE API
   ======================================== */

async function loadFilesFromAPI(token) {
    try {
        const response = await fetch("/api/GetFilesList", {
            method: "GET",
            headers: {
                "Accept": "application/json",
                "Authorization": `Bearer ${token}`
            }
        });

        if (!response.ok) {
            throw new Error(`API request failed: ${response.status}`);
        }

        const files = await response.json();

        // Clear existing rows before displaying API records
        fileTable.querySelectorAll(".file-row").forEach(row => row.remove());

        // Display each file returned by the API
        for (const file of files) {
            addFileToTable({
                fileId: file.fileId,
                name: file.filename,
                size: file.sizeBytes,
                modifiedDate: file.updatedUtc
            });
        }

        console.log("Files received from API:", files);

    } catch (error) {
        console.error("Could not load files:", error);
    }
}

async function startFileModule() {
    try {
        // Get a test token for Andy
        const loginResponse = await fetch("/api/UserContext/set-user", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                UserName: "Andy"
            })
        });

        if (!loginResponse.ok) {
            throw new Error(`Login failed: ${loginResponse.status}`);
        }

        const loginData = await loginResponse.json();
        authToken = loginData.token;

        console.log("Test token received");

        // Use the token to load the user's files
        await loadFilesFromAPI(loginData.token);
        console.log("File table rows:", fileTable.querySelectorAll(".file-row").length);
        console.log("File table HTML:", fileTable.innerHTML);

    } catch (error) {
        console.error("Could not start file module:", error);
    }
}

/* ========================================
   File Upload
   ======================================== */

async function uploadFileToAPI(file, token) {
    const formData = new FormData();
    formData.append("file", file);

    const response = await fetch("/api/UploadFiles", {
        method: "POST",
        headers: {
            "Authorization": `Bearer ${token}`
        },
        body: formData
    });

    const result = await response.json();

    if (!response.ok) {
        throw new Error(
            `Upload failed (${response.status}): ${JSON.stringify(result)}`
        );
    }

    console.log("Upload response:", result);
    return result;
}

/* ========================================
   File Download
   ======================================== */

async function downloadFileFromAPI(fileId, token) {
    const response = await fetch(
        `/api/FileDownload/${encodeURIComponent(fileId)}/download`,
        {
            method: "GET",
            headers: {
                "Authorization": `Bearer ${token}`
            }
        }
    );

    if (!response.ok) {
        const error = await response.text();
        throw new Error(
            `Download failed (${response.status}): ${error}`
        );
    }

    const blob = await response.blob();
    return blob;
}

//Reads the files whose checkboxes are ticked.
//Shows a message if no files are selected.
//Sends a download request for each selected file.
//Saves each successful response using its original filename.
const downloadButton = document.getElementById("downloadButton");

downloadButton.addEventListener("click", async function () {
    const selectedRows = fileTable.querySelectorAll(
        ".file-row .file-checkbox:checked"
    );

    if (selectedRows.length === 0) {
        alert("Please select at least one file to download.");
        return;
    }

    for (const checkbox of selectedRows) {
        const fileRow = checkbox.closest(".file-row");
        const fileId = fileRow.dataset.fileId;
        const filename = fileRow.dataset.fileName;

        try {
            const blob = await downloadFileFromAPI(fileId, authToken);

            const downloadUrl = URL.createObjectURL(blob);
            const link = document.createElement("a");

            link.href = downloadUrl;
            link.download = filename;
            document.body.appendChild(link);
            link.click();
            link.remove();

            URL.revokeObjectURL(downloadUrl);
        } catch (error) {
            console.error(`Download failed for "${filename}":`, error);
            alert(
                `Could not download "${filename}". ` +
                "The backend may not have implemented decryption yet."
            );
        }
    }
});

//Requires at least one selected file.
//Asks for confirmation before deleting.
//Sends a DELETE request for each selected file.
//Removes a row from the table only when the API reports success.
//Logs errors if a delete request fails.
//Logs errors if a delete request fails

const deleteButton = document.getElementById("deleteButton");

deleteButton.addEventListener("click", async function () {
    const selectedRows = fileTable.querySelectorAll(
        ".file-row .file-checkbox:checked"
    );

    if (selectedRows.length === 0) {
        alert("Please select at least one file to delete.");
        return;
    }

    if (!confirm(`Are you sure you want to delete ${selectedRows.length} selected file(s)?`)) {
        return;
    }

    for (const checkbox of selectedRows) {
        const fileRow = checkbox.closest(".file-row");
        const fileId = fileRow.dataset.fileId;
        const filename = fileRow.querySelector(".filename").textContent;

        try {
            const response = await fetch(
                `/files/${encodeURIComponent(fileId)}`,
                {
                    method: "DELETE",
                    headers: {
                        "Authorization": `Bearer ${authToken}`
                    }
                }
            );

            if (!response.ok) {
                const error = await response.text();
                throw new Error(`Delete failed (${response.status}): ${error}`);
            }

            fileRow.remove();
            console.log(`Deleted "${filename}".`);
        } catch (error) {
            console.error(`Could not delete "${filename}":`, error);
            alert(`Could not delete "${filename}". Check the Console for details.`);
        }
    }
});

//Start the file module when the page loads
startFileModule();