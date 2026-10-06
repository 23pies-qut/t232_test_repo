// UploadForm - choose a file and upload it; shows the send progress during the upload.
import { useState } from 'react';

export default function UploadForm({ busy, sending, onUpload }) {
  const [file, setFile] = useState(null);
  const [inputKey, setInputKey] = useState(0); // changing the key clears the <input type="file">

  return (
    <form className="card upload" onSubmit={(e) => {
      e.preventDefault();
      onUpload(file);
      setFile(null);
      setInputKey((k) => k + 1);
    }}>
      <input key={inputKey} type="file" disabled={busy} onChange={(e) => setFile(e.target.files?.[0] ?? null)} />
      <button disabled={busy || !file}>{busy ? 'Working…' : 'Upload & encrypt'}</button>
      {sending && <span className="muted">{sending.text} {Math.round(sending.ratio * 100)}%</span>}
    </form>
  );
}
