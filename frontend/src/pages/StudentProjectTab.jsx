import React, { useState, useEffect } from 'react';
import api from '../api';
import './StudentProjectTab.css';

/**
 * StudentProjectTab Component
 * Displays a list of submissions for the logged-in student.
 * Features expandable rows and a "tap to reveal" 3D flip card for supervisor details.
 */
export default function StudentProjectTab() {
  const [submissions, setSubmissions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [expandedRow, setExpandedRow] = useState(null);
  const [refreshing, setRefreshing] = useState(false);

  useEffect(() => {
    fetchSubmissions();
  }, []);

  const fetchSubmissions = async (isRefresh = false) => {
    try {
      if (isRefresh) setRefreshing(true);
      else setLoading(true);
      const res = await api.get('/submissions/my-submissions');
      setSubmissions(res.data.data || []);
    } catch (err) {
      console.error('Error fetching submissions:', err);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  const toggleExpand = (projectId) => {
    setExpandedRow(expandedRow === projectId ? null : projectId);
  };

  if (loading) {
    return (
      <div className="loading-container">
        <div className="spinner"></div>
        <p>Loading your project status...</p>
      </div>
    );
  }

  if (submissions.length === 0) {
    return (
      <div className="tab-content">
        <div className="dash-card no-data">
          <h3>No Submissions Yet</h3>
          <p>Your submitted projects will appear here once you make a submission.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="project-tab-container">
      {/* Summary banner */}
      <div style={{
        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
        marginBottom: 16, padding: '10px 16px',
        background: 'linear-gradient(135deg,#10b981,#059669)',
        borderRadius: 10, color: '#fff',
      }}>
        <div style={{ display: 'flex', gap: 24 }}>
          <div>
            <div style={{ fontSize: 11, opacity: 0.8, textTransform: 'uppercase', letterSpacing: 1 }}>Total</div>
            <div style={{ fontWeight: 700, fontSize: 20 }}>{submissions.length}</div>
          </div>
          <div>
            <div style={{ fontSize: 11, opacity: 0.8, textTransform: 'uppercase', letterSpacing: 1 }}>Matched</div>
            <div style={{ fontWeight: 700, fontSize: 20 }}>
              {submissions.filter(s => s.matchedSupervisor).length}
            </div>
          </div>
        </div>
        <button
          onClick={() => fetchSubmissions(true)}
          disabled={refreshing}
          style={{
            padding: '6px 14px', borderRadius: 20, border: '1.5px solid rgba(255,255,255,0.6)',
            background: 'transparent', color: '#fff', cursor: 'pointer', fontSize: 12, fontWeight: 600,
          }}
        >
          {refreshing ? 'Refreshing…' : '↻ Refresh'}
        </button>
      </div>

      <div className="project-table-wrapper">
        <table className="project-table">
          <thead>
            <tr>
              <th>Submission Point</th>
              <th>Upload Date</th>
              <th>Deadline</th>
              <th>Status</th>
              <th style={{ width: 50 }}></th>
            </tr>
          </thead>
          <tbody>
            {submissions.map((sub) => (
              <React.Fragment key={sub.projectId}>
                <tr className="project-row">
                  <td>
                    <div style={{ fontWeight: 600 }}>{sub.courseworkTitle}</div>
                    <div style={{ fontSize: 12, color: 'var(--gray-500)' }}>{sub.title}</div>
                    {sub.groupName && (
                      <div style={{ fontSize: 11, color: 'var(--primary-color)', fontWeight: 600, marginTop: 2 }}>
                        {sub.groupName}
                      </div>
                    )}
                  </td>
                  <td>{new Date(sub.submittedAt).toLocaleDateString()}</td>
                  <td>{sub.deadline ? new Date(sub.deadline).toLocaleDateString() : 'N/A'}</td>
                  <td>
                    <span className={`status-badge status-${sub.status.toLowerCase().replace(/\s+/g, '-')}`}>
                      {sub.status}
                    </span>
                  </td>
                  <td>
                    {sub.matchedSupervisor && (
                      <button 
                        className={`expand-btn ${expandedRow === sub.projectId ? 'active' : ''}`}
                        onClick={() => toggleExpand(sub.projectId)}
                        title="View Details"
                      >
                        <ChevronDownIcon />
                      </button>
                    )}
                  </td>
                </tr>

                {/* Sub-row for supervisor reveal */}
                {sub.matchedSupervisor && (
                  <tr className="expansion-row">
                    <td colSpan="5">
                      <div className={`expansion-content ${expandedRow === sub.projectId ? 'active' : ''}`}>
                        <RevealCard supervisor={sub.matchedSupervisor} />
                      </div>
                    </td>
                  </tr>
                )}
              </React.Fragment>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

// ── RevealCard Sub-component ──────────────────────────────────────────

function RevealCard({ supervisor }) {
  const [isFlipped, setIsFlipped] = useState(false);

  return (
    <div style={{ perspective: 800, marginTop: 8 }}>
      <div style={{
        width: '100%', height: 140, position: 'relative',
        transformStyle: 'preserve-3d',
        transition: 'transform 0.6s cubic-bezier(.4,2,.6,1)',
        transform: isFlipped ? 'rotateY(180deg)' : 'none',
      }}>
        {/* Front */}
        <div style={{
          position: 'absolute', inset: 0, backfaceVisibility: 'hidden',
          background: 'linear-gradient(135deg,#10b981,#059669)',
          borderRadius: 10, display: 'flex', flexDirection: 'column',
          alignItems: 'center', justifyContent: 'center', gap: 6, color: '#fff',
        }}>
          <span style={{ fontSize: 26 }}>*</span>
          <span style={{ fontWeight: 700, fontSize: 14 }}>Supervisor Matched!</span>
          <button
            onClick={() => setIsFlipped(true)}
            style={{
              marginTop: 4, padding: '4px 16px', borderRadius: 20,
              border: '1.5px solid #fff', background: 'transparent',
              color: '#fff', cursor: 'pointer', fontSize: 12, fontWeight: 600,
            }}
          >
            Tap to reveal
          </button>
        </div>
        {/* Back */}
        <div style={{
          position: 'absolute', inset: 0, backfaceVisibility: 'hidden',
          transform: 'rotateY(180deg)',
          background: '#fff', border: '1.5px solid #e2e8f0',
          borderRadius: 10, display: 'flex', flexDirection: 'column',
          alignItems: 'center', justifyContent: 'center', gap: 4,
        }}>
          <div style={{ fontSize: 11, textTransform: 'uppercase', letterSpacing: 1, color: '#94a3b8' }}>Your Supervisor</div>
          <div style={{ fontWeight: 700, fontSize: 16, color: '#1e293b' }}>{supervisor.name}</div>
          <div style={{ fontSize: 13, color: '#6366f1' }}>{supervisor.email}</div>
          <button
            onClick={() => setIsFlipped(false)}
            style={{ marginTop: 6, background: 'none', border: 'none', color: '#94a3b8', cursor: 'pointer', fontSize: 11 }}
          >
            Flip Back
          </button>
        </div>
      </div>
    </div>
  );
}

// ── Icons ─────────────────────────────────────────────────────────────

function ChevronDownIcon() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <polyline points="6 9 12 15 18 9"></polyline>
    </svg>
  );
}
