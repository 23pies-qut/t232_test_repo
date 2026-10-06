// TenantSwitcher - tenant buttons, coloured per tenant; clicking one switches tenant (issues a new JWT).
import { tenantColor } from '../utils/format.js';

export default function TenantSwitcher({ tenants, current, busy, onPick }) {
  return (
    <div className="tenants">
      {tenants.map((t) => (
        <button key={t.tenantId} className={`chip${t.tenantId === current ? ' on' : ''}`} style={{ '--c': tenantColor(t.tenantId) }}
          disabled={busy || t.tenantId === current} onClick={() => onPick(t)}>
          {t.tenantName}
        </button>
      ))}
    </div>
  );
}
