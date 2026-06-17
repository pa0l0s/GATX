import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../shared/auth/AuthContext';

const navItems = [
  { to: '/products', label: 'Products' },
  { to: '/workstations', label: 'Workstations' },
  { to: '/lines', label: 'Assembly lines' }
];

export function Layout() {
  const { username, logout } = useAuth();

  return (
    <div className="app">
      <header className="topbar">
        <div className="topbar-inner">
          <div className="brand">
            <span className="brand-mark">GATX</span>
            <span className="brand-text">Assembly Line Manager</span>
          </div>
          <nav className="nav">
            {navItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) => (isActive ? 'nav-link active' : 'nav-link')}
              >
                {item.label}
              </NavLink>
            ))}
          </nav>
          <div className="session">
            {username && <span className="session-user">{username}</span>}
            <button type="button" className="ghost" onClick={logout}>
              Sign out
            </button>
          </div>
        </div>
      </header>
      <main className="page">
        <Outlet />
      </main>
    </div>
  );
}
