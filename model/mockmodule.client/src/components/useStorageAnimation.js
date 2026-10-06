// =============================================================================
// useStorageAnimation - playback logic of the storage animation (rendering is in StorageAnimation.jsx)
//
// The full storage flow of an upload is laid out as 9 nodes in real execution order; the packet only moves left to right:
//   send file → validate JWT → session context → files_CRUD_JSON INSERT → dbo.files (PENDING)
//   → AES-256-GCM encryption → files_Content_Put → dbo.file_contents (UPLOADED) → back to browser
//
//   play(op)  successful upload: the packet passes every node and stops at "back to browser" with ✅;
//   show(op)  stalled or failed upload: no playback, the packet stops at the problem node (stalled ⏳ / failed ✕);
//   clear()   resets the animation (when the user / tenant changes).
// op = { title, tenant, steps }; steps come from the backend UploadResponse.Trace; stagesOf() maps each step to nodes.
// =============================================================================
import { useCallback, useRef, useState } from 'react';

export const STAGES = [
  { key: 'send', icon: '🖥️', label: 'Browser', sub: 'Send file' },
  { key: 'auth', icon: '🛡️', label: 'API', sub: 'Validate JWT' },
  { key: 'context', icon: '🔑', label: 'Session context', sub: 'Set tenant ID' },
  { key: 'insert', icon: '⚙️', label: 'Write metadata', sub: 'files_CRUD_JSON' },
  { key: 'files', icon: '🗄️', label: 'dbo.files', sub: 'PENDING' },
  { key: 'encrypt', icon: '🔐', label: 'Encrypt', sub: 'AES-256-GCM' },
  { key: 'put', icon: '⚙️', label: 'Write ciphertext', sub: 'files_Content_Put' },
  { key: 'contents', icon: '🗄️', label: 'file_contents', sub: 'UPLOADED' },
  { key: 'reply', icon: '↩️', label: 'Back to browser', sub: 'Upload complete' }
];
const STEP_MS = 650; // how long the packet stays on each node

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
const indexOf = (key) => STAGES.findIndex((s) => s.key === key);

/**
 * Maps a trace step to nodes by its title (titles come from the backend FilesService and from FileBrowserPage).
 * A stored-procedure step that writes a table maps to two nodes: the procedure, then the table.
 */
function stagesOf(step, isFirst) {
  const t = step.title;
  const halted = step.error || step.stuck;
  if (t.includes('return response')) return ['reply'];
  if (t.includes('send file') || (isFirst && step.node === 'browser')) return ['send'];
  if (t.includes('session context')) return ['context'];
  if (t.includes('INSERT')) return halted ? ['insert'] : ['insert', 'files'];
  if (t.includes('encryption')) return ['encrypt'];
  if (t.includes('Content_Put')) return halted ? ['put'] : ['put', 'contents'];
  if (t.includes('Validate') || step.node === 'api') return ['auth'];
  return [step.node === 'browser' ? 'send' : 'auth'];
}

export function useStorageAnimation() {
  const [view, setView] = useState(null);
  const runId = useRef(0);

  const play = useCallback(async (op) => {
    const id = ++runId.current;
    let reached = -1; // rightmost node reached so far: guarantees left-to-right movement
    for (const [i, step] of op.steps.entries()) {
      for (const key of stagesOf(step, i === 0)) {
        const at = indexOf(key);
        if (at <= reached) continue;
        if (runId.current !== id) return; // replaced by a newer upload or clear()
        reached = at;
        setView({ op, at, title: step.title, cipher: step.cipher, status: 'playing' });
        await sleep(STEP_MS);
      }
    }
    if (runId.current === id) setView({ op, at: STAGES.length - 1, status: 'done' });
  }, []);

  const show = useCallback((op) => {
    runId.current++;
    const last = op.steps[op.steps.length - 1];
    const at = indexOf(stagesOf(last, op.steps.length === 1)[0]);
    setView({ op, at, title: last.title, detail: last.detail, cipher: last.cipher, status: last.error ? 'failed' : 'stuck' });
  }, []);

  const clear = useCallback(() => {
    runId.current++;
    setView(null);
  }, []);

  return { view, play, show, clear };
}
