import { useEffect, useState } from "react";
import {
  deleteFile,
  downloadFile,
  getFiles,
  saveDownloadedFile,
  uploadFile,
} from "../api/fileApi.js";
import FileList from "../components/FileList.jsx";
import FileUploadForm from "../components/FileUploadForm.jsx";
import "./FilePage.css";

function FilePage() {
  // Page state: file list, search text, busy flags and feedback messages.
  const [files, setFiles] = useState([]);
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [downloadingFileId, setDownloadingFileId] =
    useState("");
  const [deletingFileId, setDeletingFileId] =
    useState("");
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  // Clear both the success and the error banner.
  const clearFeedback = () => {
    setMessage("");
    setError("");
  };

  // Show the message from the API, or a fallback text.
  const showError = (requestError, defaultMessage) => {
    setError(requestError.message || defaultMessage);
  };

  // Fetch the file list.
  const loadFiles = async () => {
    try {
      const data = await getFiles();

      setError("");
      setFiles(data);
    } catch (requestError) {
      showError(
        requestError,
        "Unable to load files."
      );
    } finally {
      setLoading(false);
    }
  };

  // Used by the refresh button and after a successful upload.
  const handleRefresh = async () => {
    setLoading(true);

    await loadFiles();
  };

  // Upload one file, then reload the list.
  const handleUpload = async (file) => {
    try {
      setUploading(true);
      clearFeedback();

      await uploadFile(file);

      setMessage(
        `${file.name} uploaded and encrypted successfully.`
      );

      await handleRefresh();

      return true;
    } catch (requestError) {
      showError(
        requestError,
        "Unable to upload the file."
      );

      return false;
    } finally {
      setUploading(false);
    }
  };

  // Download the file and save it to the user's disk.
  const handleDownload = async (file) => {
    try {
      setDownloadingFileId(file.fileId);
      clearFeedback();

      const result = await downloadFile(
        file.fileId
      );

      const fileName =
        result.fileName === "download"
          ? file.fileName
          : result.fileName;

      saveDownloadedFile(
        result.blob,
        fileName
      );

      setMessage(
        `${file.fileName} downloaded successfully.`
      );
    } catch (requestError) {
      showError(
        requestError,
        "Unable to download the file."
      );
    } finally {
      setDownloadingFileId("");
    }
  };

  // Confirm, delete, then remove the row from the list.
  const handleDelete = async (file) => {
    const confirmed = window.confirm(
      `Delete "${file.fileName}"?`
    );

    if (!confirmed) {
      return;
    }

    try {
      setDeletingFileId(file.fileId);
      clearFeedback();

      await deleteFile(file.fileId);

      setFiles((currentFiles) =>
        currentFiles.filter(
          (currentFile) =>
            currentFile.fileId !== file.fileId
        )
      );

      setMessage(
        `${file.fileName} deleted successfully.`
      );
    } catch (requestError) {
      showError(
        requestError,
        "Unable to delete the file."
      );
    } finally {
      setDeletingFileId("");
    }
  };

  // Load the list once when the page opens.
  useEffect(() => {
    loadFiles();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // The three summary cards shown at the top.
  const statistics = [
    {
      value: files.length,
      label: "Stored files",
      colour: "purple",
      icon: "F",
    },
    {
      value: "AES-GCM",
      label: "Encryption standard",
      colour: "blue",
      icon: "E",
    },
    {
      value: "SHA-256",
      label: "Integrity verification",
      colour: "green",
      icon: "V",
    },
  ];

  return (
    <main className="file-page">
      <div className="file-page-container">
        <header className="file-page-header">
          <div className="file-page-heading">
            <div className="file-page-logo">
              🔒
            </div>

            <div>
              <h1 className="file-page-title">
                Encrypted File Storage
              </h1>
            </div>
          </div>

          <div className="file-page-status">
            <span className="file-page-status-dot" />
            System Online
          </div>
        </header>

        <section className="file-page-statistics">
          {statistics.map((statistic) => (
            <article
              className="file-page-statistic"
              key={statistic.label}
            >
              <div
                className={
                  `file-page-statistic-icon ` +
                  statistic.colour
                }
              >
                {statistic.icon}
              </div>

              <div>
                <span className="file-page-statistic-value">
                  {statistic.value}
                </span>

                <span className="file-page-statistic-label">
                  {statistic.label}
                </span>
              </div>
            </article>
          ))}
        </section>

        {message && (
          <Notification
            type="message"
            text={message}
            onClose={clearFeedback}
          />
        )}

        {error && (
          <Notification
            type="error"
            text={error}
            onClose={clearFeedback}
          />
        )}

        <FileUploadForm
          uploading={uploading}
          onUpload={handleUpload}
        />

        <FileList
          files={files}
          search={search}
          loading={loading}
          downloadingFileId={downloadingFileId}
          deletingFileId={deletingFileId}
          onSearchChange={setSearch}
          onRefresh={handleRefresh}
          onDownload={handleDownload}
          onDelete={handleDelete}
        />

        <footer className="file-page-footer">
          <p>
            React · ASP.NET Core · SQL Server ·
            AES-GCM
          </p>
        </footer>
      </div>
    </main>
  );
}

// Small banner used for the success and error messages.
function Notification({
  type,
  text,
  onClose,
}) {
  const isError = type === "error";

  return (
    <div
      className={
        isError
          ? "file-page-error"
          : "file-page-message"
      }
      role={isError ? "alert" : "status"}
    >
      <span>
        {isError ? "!" : "✓"}
      </span>

      <span>{text}</span>

      <button
        className="file-page-notification-close"
        type="button"
        onClick={onClose}
        aria-label="Close notification"
      >
        ×
      </button>
    </div>
  );
}

export default FilePage;