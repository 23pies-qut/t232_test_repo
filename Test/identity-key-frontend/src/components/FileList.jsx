import FileItem from "./FileItem.jsx";
import "./FileList.css";

// The list card: header with refresh, the search box, then one row per file.
function FileList({
  files,
  search,
  loading,
  downloadingFileId,
  deletingFileId,
  onSearchChange,
  onRefresh,
  onDownload,
  onDelete,
}) {
  // Filter by name, MIME type or status (case-insensitive).
  const searchValue =
    search.trim().toLowerCase();

  const filteredFiles = files.filter((file) => {
    if (!searchValue) {
      return true;
    }

    const fileName = file.fileName.toLowerCase();
    const mimeType = file.mimeType.toLowerCase();
    const status = file.status.toLowerCase();

    return (
      fileName.includes(searchValue) ||
      mimeType.includes(searchValue) ||
      status.includes(searchValue)
    );
  });

  return (
    <section className="file-list-card">
      <div className="file-list-header">
        <div>
          <h2 className="file-list-title">
            Your Files
          </h2>

          <p className="file-list-count">
            {files.length}{" "}
            {files.length === 1
              ? "encrypted file"
              : "encrypted files"}
          </p>
        </div>

        <button
          className="file-list-refresh-button"
          type="button"
          onClick={onRefresh}
          disabled={loading}
        >
          <svg
            className={
              loading
                ? "file-list-refresh-icon-spinning"
                : ""
            }
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path
              d="M20 11a8 8 0 1 0-2.3 5.7M20 5v6h-6"
              fill="none"
              stroke="currentColor"
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth="1.8"
            />
          </svg>

          {loading
            ? "Refreshing..."
            : "Refresh"}
        </button>
      </div>

      <div className="file-list-toolbar">
        <div className="file-list-search-wrapper">
          <svg
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <circle
              cx="11"
              cy="11"
              r="6.5"
              fill="none"
              stroke="currentColor"
              strokeWidth="1.8"
            />

            <path
              d="m16 16 4 4"
              fill="none"
              stroke="currentColor"
              strokeLinecap="round"
              strokeWidth="1.8"
            />
          </svg>

          <input
            className="file-list-search"
            type="search"
            placeholder="Search by name, type or status"
            value={search}
            onChange={(event) =>
              onSearchChange(
                event.target.value
              )
            }
          />

          {search && (
            <button
              className="file-list-clear-search"
              type="button"
              onClick={() =>
                onSearchChange("")
              }
              aria-label="Clear search"
            >
              ×
            </button>
          )}
        </div>

        <div className="file-list-result-count">
          {filteredFiles.length} result
          {filteredFiles.length === 1
            ? ""
            : "s"}
        </div>
      </div>

      {/* Shown while the list is loading */}
      {loading && (
        <div className="file-list-loading">
          <span className="file-list-loading-spinner" />

          <strong>Loading files</strong>

          <span className="file-list-hint">
            Retrieving encrypted file metadata...
          </span>
        </div>
      )}

      {/* Shown when there are no files, or the search matched nothing */}
      {!loading &&
        filteredFiles.length === 0 && (
          <div className="file-list-empty">
            <div className="file-list-empty-icon">
              <svg
                viewBox="0 0 24 24"
                aria-hidden="true"
              >
                <path
                  d="M4 5.5A1.5 1.5 0 0 1 5.5 4h4l2 2h7A1.5 1.5 0 0 1 20 7.5v10a1.5 1.5 0 0 1-1.5 1.5h-13A1.5 1.5 0 0 1 4 17.5v-12Z"
                  fill="none"
                  stroke="currentColor"
                  strokeLinejoin="round"
                  strokeWidth="1.7"
                />

                <path
                  d="m9 12 6 6m0-6-6 6"
                  fill="none"
                  stroke="currentColor"
                  strokeLinecap="round"
                  strokeWidth="1.7"
                />
              </svg>
            </div>

            <strong>
              {searchValue
                ? "No matching files"
                : "No files uploaded"}
            </strong>

            <span className="file-list-hint">
              {searchValue
                ? "Try using a different search term."
                : "Select a file above to begin secure storage."}
            </span>
          </div>
        )}

      {/* One row per matching file */}
      {!loading &&
        filteredFiles.length > 0 && (
          <div className="file-list-items">
            {filteredFiles.map(
              (file) => (
                <FileItem
                  key={file.fileId}
                  file={file}
                  downloading={
                    downloadingFileId ===
                    file.fileId
                  }
                  deleting={
                    deletingFileId ===
                    file.fileId
                  }
                  onDownload={onDownload}
                  onDelete={onDelete}
                />
              )
            )}
          </div>
        )}
    </section>
  );
}

export default FileList;