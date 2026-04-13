import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';

/*
 * Shared dashboard shell used by StudentDashboard, SupervisorDashboard,
 * and ModuleLeaderDashboard. Each page passes its own `role` label + accent.
 */
export default function DashboardShell({ roleLabel, roleClass, accentEmoji, children }) {
  const navigate = useNavigate();
  const [user, setUser] = useState(null);

  useEffect(() => {
    const raw = localStorage.getItem('pas_user');
    if (!raw) { navigate('/login'); return; }
    setUser(JSON.parse(raw));
  }, [navigate]);

  const handleLogout = () => {
    localStorage.removeItem('pas_token');
    localStorage.removeItem('pas_user');
    navigate('/login');
  };

  const initials = user?.name
    ? user.name.split(' ').map((w) => w[0]).join('').slice(0, 2).toUpperCase()
    : '?';

  return (
    <div className="dashboard-page">
      {/* ── Nav bar ── */}
      <nav className="dashboard-nav">
        <div className="nav-brand">
          <div className="nav-brand-icon">P</div>
          Project Approval System
        </div>

        <div className="nav-right">
          {user && (
            <div className="nav-user">
              <div className="nav-user-avatar">{initials}</div>
              <div className="nav-user-info">
                <div className="nav-user-name">{user.name}</div>
                <div className="nav-user-email">{user.email}</div>
              </div>
            </div>
          )}
          <button
            id="logout-btn"
            className="btn btn-outline"
            style={{ padding: '7px 14px', fontSize: 13 }}
            onClick={handleLogout}
          >
            Sign out
          </button>
        </div>
      </nav>

      {/* ── Content ── */}
      <div className="dashboard-content">
        {/* Header */}
        <div style={{ marginBottom: 28 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 6 }}>
            <h1 style={{ fontSize: 24, fontWeight: 700, color: 'var(--gray-900)' }}>
              {accentEmoji} {roleLabel} Dashboard
            </h1>
            <span className={`role-badge ${roleClass}`}>{roleLabel}</span>
          </div>
          <p style={{ color: 'var(--gray-500)', fontSize: 14 }}>
            Welcome back, <strong style={{ color: 'var(--gray-700)' }}>{user?.name}</strong>. 
            This is your {roleLabel.toLowerCase()} portal.
          </p>
        </div>

        {/* User details card */}
        <div className="dash-card">
          <div className="dash-card-title">👤 Account Information</div>
          <div className="detail-row">
            <span className="detail-label">Full Name</span>
            <span className="detail-value">{user?.name ?? '—'}</span>
          </div>
          <div className="detail-row">
            <span className="detail-label">Email</span>
            <span className="detail-value">{user?.email ?? '—'}</span>
          </div>
          <div className="detail-row">
            <span className="detail-label">Role</span>
            <span className="detail-value">
              <span className={`role-badge ${roleClass}`}>{user?.role ?? '—'}</span>
            </span>
          </div>
          {user?.batch && (
            <div className="detail-row">
              <span className="detail-label">Batch</span>
              <span className="detail-value">{user.batch}</span>
            </div>
          )}
          <div className="detail-row">
            <span className="detail-label">User ID</span>
            <span className="detail-value" style={{ color: 'var(--gray-400)', fontSize: 13 }}>
              #{user?.userId}
            </span>
          </div>
        </div>

        {/* Role-specific placeholder */}
        {children}
      </div>
    </div>
  );
}
