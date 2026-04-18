import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../api';
import './SystemAdminDashboard.css';

const REFRESH_INTERVAL_MS = 10000;

function formatDate(value) {
  if (!value) return '-';

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '-';

  return date.toLocaleDateString('en-US', {
    month: 'long',
    day: '2-digit',
    year: 'numeric',
  });
}

function formatTime(value) {
  if (!value) return '-';

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '-';

  return date.toLocaleTimeString([], {
    hour: '2-digit',
    minute: '2-digit',
  });
}

function normalizeResponse(data) {
  return data?.data ?? data ?? {};
}

function AdminMark({ status }) {
  const className = status === 'Changed'
    ? 'system-admin-pill success'
    : status === 'Pending'
      ? 'system-admin-pill warning'
      : 'system-admin-pill muted';

  return <span className={className}>{status}</span>;
}

export default function SystemAdminDashboard() {
  const navigate = useNavigate();
  const [summary, setSummary] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [menuOpen, setMenuOpen] = useState(false);
  const [lastUpdated, setLastUpdated] = useState(null);

  const currentUser = useMemo(() => {
    const raw = localStorage.getItem('pas_user');
    if (!raw) return null;

    try {
      return JSON.parse(raw);
    } catch {
      return null;
    }
  }, []);

  const handleLogout = () => {
    localStorage.removeItem('pas_token');
    localStorage.removeItem('pas_user');
    navigate('/login');
  };

  useEffect(() => {
    const handleClickAway = () => setMenuOpen(false);
    window.addEventListener('click', handleClickAway);
    return () => window.removeEventListener('click', handleClickAway);
  }, []);

  useEffect(() => {
    let cancelled = false;

    const loadSummary = async () => {
      try {
        const response = await api.get('/admin/dashboard/summary');
        const payload = normalizeResponse(response.data);

        if (cancelled) return;

        setSummary(payload);
        setError('');
        setLastUpdated(new Date());
      } catch (requestError) {
        if (cancelled) return;

        setError('Live dashboard data is unavailable right now.');
        setSummary({
          systemAnalytics: {
            supervisors: 0,
            students: 0,
            individualProjectApprovals: 0,
            groupProjectApprovals: 0,
          },
          passwordResetRequests: [],
        });
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    loadSummary();
    const intervalId = window.setInterval(loadSummary, REFRESH_INTERVAL_MS);

    return () => {
      cancelled = true;
      window.clearInterval(intervalId);
    };
  }, []);

  const analytics = summary?.systemAnalytics ?? {
    supervisors: 0,
    students: 0,
    individualProjectApprovals: 0,
    groupProjectApprovals: 0,
  };

  const requests = summary?.passwordResetRequests ?? [];

  return (
    <div className="system-admin-page">
      <header className="system-admin-topbar">
        <div className="system-admin-brand">
          <div className="system-admin-logo" aria-hidden="true">
            <svg viewBox="0 0 64 64" role="presentation">
              <path d="M10 14h28v18H10z" fill="none" stroke="currentColor" strokeWidth="4" />
              <path d="M18 40h12" fill="none" stroke="currentColor" strokeWidth="4" strokeLinecap="round" />
              <path d="M22 32h24l6 10H28z" fill="none" stroke="currentColor" strokeWidth="4" strokeLinejoin="round" />
              <path d="M24 24h8" fill="none" stroke="currentColor" strokeWidth="4" strokeLinecap="round" />
            </svg>
          </div>
          <span className="system-admin-brand-name">SYSTEM ADMIN PORTAL</span>
        </div>

        <div className="system-admin-account">
          <button
            type="button"
            className="system-admin-account-button"
            onClick={(event) => {
              event.stopPropagation();
              setMenuOpen((value) => !value);
            }}
          >
            <span>Account</span>
            <span className="system-admin-chevron">⌄</span>
          </button>

          {menuOpen && (
            <div className="system-admin-menu" onClick={(event) => event.stopPropagation()}>
              <div className="system-admin-menu-user">
                <strong>{currentUser?.name ?? 'System Administrator'}</strong>
                <span>{currentUser?.email ?? 'system.admin@pas.local'}</span>
              </div>
              <button type="button" className="system-admin-menu-action" onClick={handleLogout}>
                Sign out
              </button>
            </div>
          )}
        </div>
      </header>

      <main className="system-admin-content">
        <section className="system-admin-section">
          <h1 className="system-admin-section-title">REQUESTS FOR CHANGING PASSWORDS</h1>

          <div className="system-admin-panel">
            <div className="system-admin-table-wrap">
              <table className="system-admin-table">
                <thead>
                  <tr>
                    <th>Student ID</th>
                    <th>Requested Date</th>
                    <th>Actions</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  {loading && !requests.length ? (
                    <tr>
                      <td colSpan="4" className="system-admin-empty">
                        Loading live requests...
                      </td>
                    </tr>
                  ) : requests.length ? (
                    requests.map((request) => (
                      <tr key={request.id}>
                        <td>{request.studentId}</td>
                        <td>{request.requestedDate}</td>
                        <td>
                          <button
                            type="button"
                            className={`system-admin-action ${request.actionKind}`}
                            disabled={request.actionKind !== 'primary'}
                          >
                            {request.actionLabel}
                          </button>
                        </td>
                        <td>
                          <AdminMark status={request.status} />
                        </td>
                      </tr>
                    ))
                  ) : (
                    <tr>
                      <td colSpan="4" className="system-admin-empty">
                        No password reset requests found.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </section>

        <section className="system-admin-section analytics-section">
          <div className="system-admin-section-header">
            <h2 className="system-admin-section-title secondary">System Analytics</h2>
            {lastUpdated && (
              <span className="system-admin-refresh-note">
                Last refreshed {formatTime(lastUpdated)}
              </span>
            )}
          </div>

          {error && <div className="system-admin-banner">{error}</div>}

          <div className="system-admin-analytics-grid">
            <div className="system-admin-metric metric-purple">
              <div className="system-admin-metric-label">Supervisors</div>
              <div className="system-admin-metric-value">{analytics.supervisors}</div>
            </div>

            <div className="system-admin-metric metric-green">
              <div className="system-admin-metric-label">Students</div>
              <div className="system-admin-metric-value">{analytics.students}</div>
            </div>

            <div className="system-admin-metric metric-yellow">
              <div className="system-admin-metric-label">Individual Project approvals</div>
              <div className="system-admin-metric-value">{analytics.individualProjectApprovals}</div>
            </div>

            <div className="system-admin-metric metric-yellow light">
              <div className="system-admin-metric-label">Group Project approvals</div>
              <div className="system-admin-metric-value">{analytics.groupProjectApprovals}</div>
            </div>
          </div>
        </section>
      </main>
    </div>
  );
}