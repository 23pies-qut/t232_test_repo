// =============================================================================
// App - top-level component: header (user, tenant switcher, sign out) + page switching.
//   signed out -> LoginPage (choose a test user and tenant)
//   signed in  -> FileBrowserPage (upload, storage animation, file list)
// Signing in and switching tenant both call set-user, which issues a new JWT for the user + tenant.
// =============================================================================
import { useCallback, useEffect, useState } from 'react';
import { getHealth } from './api/healthApi.jsx';
import { getUsers, setUserContext } from './api/userContextApi.jsx';
import TenantSwitcher from './components/TenantSwitcher.jsx';
import FileBrowserPage from './pages/FileBrowserPage.jsx';
import LoginPage from './pages/LoginPage.jsx';
import { tenantColor } from './utils/format.js';
import './App.css';

function App() {
  const [users, setUsers] = useState([]);
  const [session, setSession] = useState(null); // set-user response: token, current tenant, list of tenants
  const [problem, setProblem] = useState(null);
  const [error, setError] = useState(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    getHealth()
      .then((h) => !h.ok && setProblem(h.message))
      .catch((e) => setProblem(e.message));
    getUsers()
      .then(setUsers)
      .catch((e) => setProblem(e.status === 404 ? 'Standalone mode is off: test users are available only in Development with MockModuleStandalone:Enabled = true.' : e.message));
  }, []);

  /** Signs in or switches tenant: gets a new JWT for the user + tenant. */
  async function setUser(user, tenant) {
    setBusy(true);
    setError(null);
    try {
      setSession(await setUserContext(user.userId, tenant.tenantId));
    } catch (e) {
      setError(e.message);
    } finally {
      setBusy(false);
    }
  }

  const signOut = useCallback(() => setSession(null), []);

  return (
    <main className="page" style={{ '--tenant': tenantColor(session?.tenantId) }}>
      <header className="top">
        <div>
          <h1>Mock Module · Model v8</h1>
          <p className="muted">Browser → API (JWT) → stored procedures → tables (RLS) · AES-256-GCM envelope encryption</p>
        </div>
        {session && (
          <div className="who">
            <select value={session.userId} disabled={busy} onChange={(e) => {
              const user = users.find((u) => u.userId === e.target.value);
              setUser(user, user.tenants[0]);
            }}>
              {users.filter((u) => u.tenants.length).map((u) => (
                <option key={u.userId} value={u.userId}>{u.name}</option>
              ))}
            </select>
            <TenantSwitcher tenants={session.tenants} current={session.tenantId} busy={busy} onPick={(t) => setUser(session, t)} />
            <button className="link" onClick={signOut}>Sign out</button>
          </div>
        )}
      </header>

      {problem && <p className="banner">{problem}</p>}
      {error && <p className="msg error">{error}</p>}

      {session
        ? <FileBrowserPage key={session.token} session={session} onUnauthorized={signOut} />
        : <LoginPage users={users} busy={busy} onPick={setUser} />}
    </main>
  );
}

export default App;
