
/* ========================================
   FILE UPLOAD FUNCTIONALITY
   ======================================== */

// Find the file input and file table in HTML
const fileInput = document.getElementById("fileInput");
const fileTable = document.querySelector(".file-table");

// Listen for when the user selects a file
fileInput.addEventListener("change", function () {

    // Get the selected files
    const selectedFiles = fileInput.files;

    // Go through each selected file
    for (const file of selectedFiles) {

        // Add the file to the table
        addFileToTable(file);
    }

    // Reset the input so the same file can be selected again
    fileInput.value = "";
});


/* ========================================
   ADD A FILE TO THE TABLE
   ======================================== */

function addFileToTable(file) {

    // Create a new row
    const fileRow = document.createElement("div");
    fileRow.className = "file-row";

    // Determine which SVG icon to display
    const fileIcon = getFileIcon(file.name);

    // Get the file's current date and time
    const modifiedDate = new Date().toLocaleString("en-AU");

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

            <span class="filename"></span>
        </div>

        <div class="modified-column">
            ${modifiedDate}
        </div>

        <div class="size-column">
            ${fileSize}
        </div>

        <div class="manage-column">
            <button class="manage-button" type="button">
                •••
            </button>
        </div>
    `;

    // Safely insert the filename as text
    fileRow.querySelector(".filename").textContent = file.name;

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
