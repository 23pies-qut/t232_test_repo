// FileTable - files of the current tenant (RLS returns only this tenant's rows), with download, delete and refresh.
import { formatBytes, formatTime } from '../utils/format.js';

export default function FileTable({ tenantName, files, busy, onDownload, onDelete, onRefresh }) {
  return (
    <section className="card">
      <div className="bar">
        <h2>Files in {tenantName} ({files.length})</h2>
        <span className="muted">RLS returns only this tenant's rows</span>
        <span className="spacer" />
        <button className="link" disabled={busy} onClick={onRefresh}>Refresh</button>
      </div>
      {files.length === 0 ? (
        <p className="muted">No files yet.</p>
      ) : (
        <table>
          <thead>
            <tr><th>Name</th><th>Size</th><th>Status</th><th>Updated</th><th /></tr>
          </thead>
          <tbody>
            {files.map((f) => (
              <tr key={f.fileId}>
                <td className="name" title={f.mimeType}>{f.filename}</td>
                <td>{formatBytes(f.sizeBytes)}</td>
                <td><span className={`status ${f.status.toLowerCase()}`}>{f.status}</span></td>
                <td>{formatTime(f.updatedUtc)}</td>
                <td className="actions">
                  {f.status === 'UPLOADED' && <button className="link" disabled={busy} onClick={() => onDownload(f)}>Download</button>}
                  <button className="link danger" disabled={busy} onClick={() => onDelete(f)}>Delete</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
