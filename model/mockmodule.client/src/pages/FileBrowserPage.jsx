// =============================================================================
// FileBrowserPage - the page for a signed-in user in the current tenant: upload, storage animation, file list.
//
// The storage animation appears in two cases only (see onUpload):
//   1. upload succeeded -> plays the full storage flow;
//   2. upload stalled or failed -> shows the step where it stopped.
//      Stall detection: during an upload, progress is polled every second (GET /api/Files/progress/{uploadId});
//      with no new progress for STUCK_MS ms, the stalled step is shown.
// Refresh, download and delete show no animation. App remounts this page when the user / tenant changes, which clears the animation.
// =============================================================================
import { useCallback, useEffect, useState } from 'react';
import { deleteFile, downloadFile, getUploadProgress, listFiles, saveBlob, uploadFile } from '../api/filesApi.jsx';
import FileTable from '../components/FileTable.jsx';
import StorageAnimation from '../components/StorageAnimation.jsx';
import UploadForm from '../components/UploadForm.jsx';
import { useStorageAnimation } from '../components/useStorageAnimation.js';
import { formatBytes, shortId } from '../utils/format.js';

const MAX_BYTES = 10 * 1024 * 1024; // same as FilesService.MaxBytes in the backend
const STUCK_MS = 5000; // no progress for this long counts as a stall

/** Server-side upload steps in the same order as FilesService.UploadStages in the backend; used to name the stalled step. */
const UPLOAD_STAGES = [
  { node: 'api', title: 'Validate JWT and file' },
  { node: 'proc', title: 'Set session context' },
  { node: 'proc', title: "files_CRUD_JSON  @Action = 'INSERT'" },
  { node: 'api', title: 'Envelope encryption (AES-256-GCM)' },
  { node: 'proc', title: 'files_Content_Put' },
  { node: 'browser', title: 'API → browser: return response' }
];

const browserStep = (title, detail, ms = null) => ({ node: 'browser', title, detail, ms });
const serverMs = (steps) => steps.reduce((sum, s) => sum + (s.ms || 0), 0);

export default function FileBrowserPage({ session, onUnauthorized }) {
  const { token } = session;
  const [files, setFiles] = useState([]);
  const [message, setMessage] = useState(null); // { type: 'ok' | 'error', text }
  const [busy, setBusy] = useState(false);
  const [sending, setSending] = useState(null); // upload in progress: { ratio, text }
  const animation = useStorageAnimation();

  const showError = useCallback((error) => {
    setMessage({ type: 'error', text: error.message });
    if (error.status === 401) onUnauthorized();
  }, [onUnauthorized]);

  const refresh = useCallback(async () => {
    try {
      setFiles(await listFiles(token));
    } catch (error) {
      showError(error);
    }
  }, [token, showError]);

  useEffect(() => {
    // load the file list on mount (and when the token changes)
    refresh();
  }, [refresh]);

  /** Runs a normal action (no animation). */
  async function run(work) {
    setBusy(true);
    setMessage(null);
    try {
      setMessage({ type: 'ok', text: await work() });
    } catch (error) {
      showError(error);
    } finally {
      setBusy(false);
    }
  }

  const onDownload = (file) =>
    run(async () => {
      const blob = await downloadFile(file.fileId, token);
      saveBlob(blob, file.filename);
      return `Downloaded ${file.filename} (${formatBytes(blob.size)})`;
    });

  const onDelete = (file) =>
    window.confirm(`Delete ${file.filename}?`) &&
    run(async () => {
      await deleteFile(file.fileId, token);
      setFiles(await listFiles(token));
      return `Deleted ${file.filename} (soft delete)`;
    });

  /** Upload: plays the full storage flow on success; shows the stopping step when it stalls or fails. */
  async function onUpload(file) {
    if (!file || file.size === 0) return setMessage({ type: 'error', text: 'Please choose a non-empty file first.' });
    if (file.size > MAX_BYTES) return setMessage({ type: 'error', text: 'Files are limited to 10 MB.' });

    const op = { title: `Upload ${file.name}`, tenant: { id: session.tenantId, name: session.tenantName } };
    const uploadId = crypto.randomUUID(); // used to query this upload's progress on the server
    const started = performance.now();
    let ratio = 0; // fraction of the file sent
    let serverSteps = []; // steps completed on the server
    let lastChange = Date.now();
    let finished = false;
    const sentStep = (steps) =>
      browserStep('Browser → API: send file', `POST /api/Files · ${formatBytes(file.size)} · Bearer token`,
        Math.max(0, performance.now() - started - serverMs(steps)));

    setBusy(true);
    setMessage(null);
    setSending({ ratio: 0, text: 'Sending…' });
    animation.clear();

    // stall detection: poll progress every second; with no progress for STUCK_MS, show the stalled step
    const watch = setInterval(async () => {
      if (ratio >= 1) {
        try {
          const steps = await getUploadProgress(uploadId, token);
          if (steps.length > serverSteps.length) {
            serverSteps = steps;
            lastChange = Date.now();
          }
        } catch {
          /* a failed progress query does not affect the upload itself */
        }
      }
      const idle = Date.now() - lastChange;
      if (finished || idle < STUCK_MS) return;
      const seconds = Math.round(idle / 1000);
      const stuck = ratio < 1
        ? { node: 'browser', title: 'Browser → API: send file', detail: `${Math.round(ratio * 100)}% sent, no progress for ${seconds} s (network or backend not responding)` }
        : { ...UPLOAD_STAGES[Math.min(serverSteps.length, UPLOAD_STAGES.length - 1)], detail: `waited ${seconds} s, this step has not finished` };
      const before = ratio < 1 ? [] : [browserStep('Browser → API: send file', 'file fully sent'), ...serverSteps];
      animation.show({ ...op, steps: [...before, { ...stuck, stuck: true }] });
    }, 1000);

    try {
      const result = await uploadFile(file, token, (p) => {
        if (p > ratio) {
          ratio = p;
          lastChange = Date.now();
          setSending({ ratio: p, text: p < 1 ? 'Sending…' : 'Sent, server processing…' });
        }
      }, uploadId);
      finished = true;
      animation.play({
        ...op,
        steps: [sentStep(result.trace), ...result.trace,
          browserStep('API → browser: return response', `file ${shortId(result.fileId)} stored encrypted (UPLOADED)`)]
      });
      setMessage({ type: 'ok', text: `Uploaded ${result.filename} (SHA-256 ${result.checksum.slice(0, 12)}…)` });
      setFiles(await listFiles(token));
    } catch (error) {
      finished = true;
      const trace = error.trace?.length
        ? error.trace
        : [{ node: ratio < 1 ? 'browser' : 'api', title: 'Upload failed', detail: error.message, error: true }];
      animation.show({ ...op, steps: [sentStep(trace), ...trace] }); // stop directly at the failed step
      showError(error);
    } finally {
      clearInterval(watch);
      setSending(null);
      setBusy(false);
    }
  }

  return (
    <>
      {message && <p className={`msg ${message.type}`}>{message.text}</p>}
      <UploadForm busy={busy} sending={sending} onUpload={onUpload} />
      <StorageAnimation view={animation.view} />
      <FileTable tenantName={session.tenantName} files={files} busy={busy}
        onDownload={onDownload} onDelete={onDelete} onRefresh={() => run(async () => { await refresh(); return 'List refreshed'; })} />
    </>
  );
}
