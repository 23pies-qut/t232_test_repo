// =============================================================================
// StorageAnimation - renders the storage animation as a left-to-right flow chart (playback logic is in useStorageAnimation.js).
// Shows content only after a successful upload (full playback) or when an upload stalls / fails (stops at the problem node).
// =============================================================================
import { tenantColor } from '../utils/format.js';
import { STAGES } from './useStorageAnimation.js';
import './StorageAnimation.css';

const BADGE = { playing: 'Storing', done: 'Uploaded', stuck: 'Stalled', failed: 'Failed' };

export default function StorageAnimation({ view }) {
  if (!view) {
    return (
      <section className="card viz">
        <h2>Storage animation</h2>
        <p className="muted">After a successful upload, the full storage flow is shown here from left to right; if an upload stalls or fails, this shows the step where it stopped.</p>
      </section>
    );
  }

  const { op, at, title, detail, cipher, status } = view;
  const position = (at / (STAGES.length - 1)) * 100;
  const icon = { done: '✅', stuck: '⏳', failed: '✕' }[status] ?? (cipher ? '🔒' : '📦');

  return (
    <section className="card viz" style={{ '--tenant': tenantColor(op.tenant?.id) }}>
      <div className="bar">
        <h2>Storage animation</h2>
        <span className={`badge ${status}`}>{BADGE[status]}</span>
        <span className="muted">{op.title}</span>
      </div>

      <div className="flow">
        <div className="lanes">
          <div className="track">
            <div className="fill" style={{ width: `${position}%` }} />
            <div className={`packet ${status}${cipher ? ' locked' : ''}`} style={{ left: `${position}%` }}>{icon}</div>
          </div>
          <div className="nodes">
            {STAGES.map((s, i) => (
              <div key={s.key} className={`node${i < at ? ' passed' : ''}${i === at ? ` on ${status}` : ''}`}>
                <span className="icon">{s.icon}</span>
                <span className="label">{s.label}</span>
                <span className="sub">{s.sub}</span>
              </div>
            ))}
          </div>
        </div>
      </div>

      {status === 'playing' && <p className="muted center">{title}</p>}
      {(status === 'stuck' || status === 'failed') && (
        <p className={`halt ${status}`}>
          {status === 'stuck' ? 'Stalled at' : 'Failed at'}: <b>{title}</b> - {detail}
        </p>
      )}
    </section>
  );
}
