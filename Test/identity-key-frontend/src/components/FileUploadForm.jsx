import { useRef, useState } from "react";
import { formatFileSize, getFileExtension } from "../utils/fileFormat.js";
import "./FileUploadForm.css";

// Same size limit as the API (25 MB).
const MAXIMUM_FILE_SIZE =
  25 * 1024 * 1024;

// Upload card: drop zone, file validation and the upload button.
function FileUploadForm({
  onUpload,
  uploading,
}) {
  const fileInputRef = useRef(null);
  const [selectedFile, setSelectedFile] =
    useState(null);
  const [dragging, setDragging] =
    useState(false);
  const [validationError, setValidationError] =
    useState("");

  // Returns false when the file was rejected, so the input can be cleared.
  const validateAndSelectFile = (file) => {
    setValidationError("");

    if (!file) {
      setSelectedFile(null);
      return true;
    }

    if (file.size === 0) {
      setSelectedFile(null);
      setValidationError(
        "The selected file is empty."
      );
      return false;
    }

    if (file.size > MAXIMUM_FILE_SIZE) {
      setSelectedFile(null);
      setValidationError(
        "The maximum file size is 25 MB."
      );
      return false;
    }

    setSelectedFile(file);
    return true;
  };

  // File picked in the file dialog.
  const handleFileChange = (event) => {
    const file = event.target.files?.[0];

    if (!validateAndSelectFile(file)) {
      event.target.value = "";
    }
  };

  // File dropped on the drop zone.
  const handleDrop = (event) => {
    event.preventDefault();
    setDragging(false);

    if (uploading) {
      return;
    }

    const file =
      event.dataTransfer.files?.[0];

    validateAndSelectFile(file);
  };

  // Highlight the drop zone while a file is dragged over it.
  const handleDragOver = (event) => {
    event.preventDefault();

    if (!uploading) {
      setDragging(true);
    }
  };

  // Remove the highlight when the drag leaves.
  const handleDragLeave = (event) => {
    event.preventDefault();
    setDragging(false);
  };

  // The drop zone acts as a button that opens the file dialog.
  const handleChooseFile = () => {
    if (!uploading) {
      fileInputRef.current?.click();
    }
  };

  // Clear the selection and the hidden input.
  const handleRemoveFile = () => {
    setSelectedFile(null);
    setValidationError("");

    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  };

  // Validate, then hand the file to the parent to upload.
  const handleSubmit = async (event) => {
    event.preventDefault();

    if (!selectedFile) {
      setValidationError(
        "Please select a file."
      );
      return;
    }

    setValidationError("");

    const successful =
      await onUpload(selectedFile);

    if (successful) {
      handleRemoveFile();
    }
  };

  return (
    <section className="file-upload-card">
      <div className="file-upload-header">
        <div>
          <h2 className="file-upload-title">
            Upload File
          </h2>
        </div>

        <div className="file-upload-header-icon">
          <svg
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path
              d="M12 16V4m0 0L7.5 8.5M12 4l4.5 4.5"
              fill="none"
              stroke="currentColor"
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth="1.8"
            />

            <path
              d="M5 14v4a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2v-4"
              fill="none"
              stroke="currentColor"
              strokeLinecap="round"
              strokeWidth="1.8"
            />
          </svg>
        </div>
      </div>

      <form onSubmit={handleSubmit}>
        <input
          ref={fileInputRef}
          id="file-upload-input"
          className="file-upload-hidden-input"
          type="file"
          onChange={handleFileChange}
          disabled={uploading}
        />

        <div
          className={
            "file-upload-drop-zone" +
            (dragging
              ? " file-upload-drop-zone-active"
              : "") +
            (selectedFile
              ? " file-upload-drop-zone-selected"
              : "")
          }
          onClick={handleChooseFile}
          onDrop={handleDrop}
          onDragOver={handleDragOver}
          onDragLeave={handleDragLeave}
          role="button"
          tabIndex={uploading ? -1 : 0}
          onKeyDown={(event) => {
            if (
              event.key === "Enter" ||
              event.key === " "
            ) {
              event.preventDefault();
              handleChooseFile();
            }
          }}
        >
          <div className="file-upload-drop-icon">
            <svg
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <path
                d="M7 18a4 4 0 0 1-.7-7.9A6 6 0 0 1 18 8.5v.2a4.7 4.7 0 0 1-.7 9.3"
                fill="none"
                stroke="currentColor"
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth="1.7"
              />

              <path
                d="M12 17V9m0 0-3 3m3-3 3 3"
                fill="none"
                stroke="currentColor"
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth="1.7"
              />
            </svg>
          </div>

          <div className="file-upload-drop-content">
            <strong>
              {dragging
                ? "Drop the file here"
                : "Drag and drop a file here"}
            </strong>

            <span>
              or click to browse from your device
            </span>

            <small>
              Maximum file size: 25 MB
            </small>
          </div>
        </div>

        {/* The chosen file, with a button to remove it */}
        {selectedFile && (
          <div className="file-upload-selection">
            <div className="file-upload-selection-icon">
              {getFileExtension(
                selectedFile.name
              )}
            </div>

            <div className="file-upload-selection-content">
              <div className="file-upload-selection-name">
                {selectedFile.name}
              </div>

              <div className="file-upload-selection-details">
                <span>
                  {formatFileSize(
                    selectedFile.size
                  )}
                </span>

                <span>
                  {selectedFile.type ||
                    "application/octet-stream"}
                </span>
              </div>
            </div>

            <button
              className="file-upload-remove-button"
              type="button"
              onClick={(event) => {
                event.stopPropagation();
                handleRemoveFile();
              }}
              disabled={uploading}
              aria-label="Remove selected file"
            >
              ×
            </button>
          </div>
        )}

        {/* Validation message, for example "file is empty" */}
        {validationError && (
          <div
            className="file-upload-error"
            role="alert"
          >
            <svg
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <path
                d="M12 8v5m0 3h.01M10.3 3.8 2.6 17.2A2 2 0 0 0 4.3 20h15.4a2 2 0 0 0 1.7-2.8L13.7 3.8a2 2 0 0 0-3.4 0Z"
                fill="none"
                stroke="currentColor"
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth="1.8"
              />
            </svg>

            <span>{validationError}</span>
          </div>
        )}

        <div className="file-upload-footer">
          <button
            className="file-upload-button"
            type="submit"
            disabled={
              uploading || !selectedFile
            }
          >
            {uploading ? (
              <>
                <span className="file-upload-spinner" />
                Encrypting and uploading...
              </>
            ) : (
              <>
                <svg
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path
                    d="M12 16V4m0 0L7.5 8.5M12 4l4.5 4.5"
                    fill="none"
                    stroke="currentColor"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth="1.8"
                  />

                  <path
                    d="M5 14v4a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2v-4"
                    fill="none"
                    stroke="currentColor"
                    strokeLinecap="round"
                    strokeWidth="1.8"
                  />
                </svg>

                Upload File
              </>
            )}
          </button>
        </div>
      </form>
    </section>
  );
}

export default FileUploadForm;