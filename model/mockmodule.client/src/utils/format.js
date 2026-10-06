// Small display helpers.

export const formatBytes = (n) =>
  n < 1024 ? `${n} B` : n < 1048576 ? `${(n / 1024).toFixed(1)} KB` : `${(n / 1048576).toFixed(2)} MB`;

/** SQL Server returns UTC times without a zone suffix; append Z. */
export const formatTime = (v) => new Date(/Z|[+-]\d\d:\d\d$/.test(v) ? v : v + 'Z').toLocaleString();

export const shortId = (id) => id.slice(0, 8) + '…';

const PALETTE = ['#2f6fed', '#0d9488', '#d97706', '#9333ea', '#db2777', '#0891b2'];

/** A fixed colour per tenant (the same id always gets the same colour). */
export function tenantColor(id = '') {
  let hash = 0;
  for (const c of id) hash = (hash * 31 + c.charCodeAt(0)) >>> 0;
  return PALETTE[hash % PALETTE.length];
}
