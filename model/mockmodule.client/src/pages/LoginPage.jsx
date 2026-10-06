// LoginPage - choose a test user and tenant; after getting a JWT the app shows FileBrowserPage.
import TenantSwitcher from '../components/TenantSwitcher.jsx';

export default function LoginPage({ users, busy, onPick }) {
  return (
    <section className="card">
      <h2>Choose a test user and tenant</h2>
      <p className="muted">The backend issues a JWT for the selected user + tenant. A user can belong to several tenants and can switch at any time in the top-right corner.</p>
      {users.filter((u) => u.tenants.length).map((u) => (
        <div key={u.userId} className="pick">
          <div><b>{u.name}</b> <span className="muted">{u.email}</span></div>
          <TenantSwitcher tenants={u.tenants} busy={busy} onPick={(t) => onPick(u, t)} />
        </div>
      ))}
    </section>
  );
}
