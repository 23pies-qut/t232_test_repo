import { formatFileSize, getFileExtension } from "../utils/fileFormat.js";
import "./FileItem.css";

// One row of the file list: badge, name, status, details and actions.
function FileItem({
  file,
  downloading,
  deleting,
  onDownload,
  onDelete,
}) {
  const extension = getFileExtension(
    file.fileName
  );

  const iconClass = getFileTypeClass(
    extension
  );

  const isBusy = downloading || deleting;

  return (
    <article className="file-item">
      {/* File type badge, for example TXT or PDF */}
      <div
        className={`file-item-icon ${iconClass}`}
      >
        {extension}
      </div>

      <div className="file-item-information">
        {/* Name plus the UPLOADED / PENDING / FAILED badge */}
        <div className="file-item-name-row">
          <h3 className="file-item-name">
            {file.fileName || "Unnamed file"}
          </h3>

          <span
            className={`file-item-status file-item-status-${file.status.toLowerCase()}`}
          >
            <span className="file-item-status-dot" />
            {file.status}
          </span>
        </div>

        {/* Size, type and last updated time */}
        <div className="file-item-details">
          <span>
            Size: {formatFileSize(file.sizeBytes)}
          </span>

          <span>
            Type: {file.mimeType || "Unknown"}
          </span>

          <span>
            Updated: {formatDate(file.updatedUtc)}
          </span>
        </div>
      </div>

      {/* Actions for this row */}
      <div className="file-item-actions">
        <button
          className="file-item-download-button"
          type="button"
          onClick={() => onDownload(file)}
          disabled={isBusy}
        >
          {downloading
            ? "Downloading..."
            : "Download"}
        </button>

        <button
          className="file-item-delete-button"
          type="button"
          onClick={() => onDelete(file)}
          disabled={isBusy}
        >
          {deleting
            ? "Deleting..."
            : "Delete"}
        </button>
      </div>
    </article>
  );
}

// Pick the badge colour that matches the file extension.
function getFileTypeClass(extension) {
  const fileTypes = {
    pdf: "pdf",
    png: "image",
    jpg: "image",
    jpeg: "image",
    gif: "image",
    webp: "image",
    doc: "document",
    docx: "document",
    txt: "document",
    xls: "spreadsheet",
    xlsx: "spreadsheet",
    csv: "spreadsheet",
    zip: "archive",
    rar: "archive",
    "7z": "archive",
  };

  const type =
    fileTypes[extension.toLowerCase()] ||
    "default";

  return `file-item-icon-${type}`;
}

// Show a timestamp in the user's local format.
function formatDate(dateValue) {
  if (!dateValue) {
    return "Unknown";
  }

  const date = new Date(dateValue);

  if (Number.isNaN(date.getTime())) {
    return "Unknown";
  }

  return date.toLocaleString();
}

export default FileItem;
