import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';

/*
 * DashboardShell — shared layout for all portals.
 *
 * Props:
 *   portalName  — e.g. "STUDENT PORTAL"
 *   tabs        — array of { id, label } objects
 *   roleClass   — CSS modifier for role badge colour
 *   children    — receives { activeTab } render prop
 */
export default function DashboardShell({ portalName, tabs = [], roleClass, children }) {
  const navigate = useNavigate();
  const [user, setUser] = useState(null);
  const [activeTab, setActiveTab] = useState(tabs[0]?.id ?? '');

  useEffect(() => {
    const raw = localStorage.getItem('pas_user');
    if (!raw) { navigate('/login'); return; }
    setUser(JSON.parse(raw));
  }, [navigate]);

  // Sync default tab if tabs list changes
  useEffect(() => {
    if (tabs.length && !tabs.find(t => t.id === activeTab)) {
      setActiveTab(tabs[0].id);
    }
  }, [tabs, activeTab]);

  const handleLogout = () => {
    localStorage.removeItem('pas_token');
    localStorage.removeItem('pas_user');
    navigate('/login');
  };

  const initials = user?.name
    ? user.name.split(' ').map(w => w[0]).join('').slice(0, 2).toUpperCase()
    : '?';

  return (
    <div className="dashboard-page">

      {/* ══════════════════════════════════════════════════════════════
          TOP BAR — three zones: left | centre | right
      ══════════════════════════════════════════════════════════════ */}
      <nav className="portal-nav">

        {/* ── LEFT: logo + portal name ── */}
        <div className="portal-nav-left">
          <img
            src="/favicon.png"
            alt="PAS logo"
            className="portal-logo-img"
          />
          <span className="portal-name">{portalName}</span>
        </div>

        {/* ── CENTRE: tabs ── */}
        <div className="portal-nav-tabs">
          {tabs.map(tab => (
            <button
              key={tab.id}
              id={`tab-${tab.id}`}
              className={`portal-tab ${activeTab === tab.id ? 'active' : ''}`}
              onClick={() => setActiveTab(tab.id)}
            >
              {tab.label}
            </button>
          ))}
        </div>

        {/* ── RIGHT: user info + sign out ── */}
        <div className="portal-nav-right">
          {user && (
            <div className="nav-user">
              <div className={`nav-user-avatar ${roleClass}`}>{initials}</div>
              <div className="nav-user-info">
                <div className="nav-user-name">{user.name}</div>
                <div className="nav-user-email">{user.email}</div>
              </div>
            </div>
          )}
          <button
            id="logout-btn"
            className="portal-signout-btn"
            onClick={handleLogout}
          >
            Sign Out
          </button>
        </div>
      </nav>

      {/* ══════════════════════════════════════════════════════════════
          PAGE CONTENT — passes activeTab so each dashboard can render
          the right content per tab
      ══════════════════════════════════════════════════════════════ */}
      <div className="dashboard-content">
        {typeof children === 'function' ? children({ activeTab }) : children}
      </div>
    </div>
  );
}
