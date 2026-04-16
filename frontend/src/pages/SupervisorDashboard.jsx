import { useState, useEffect } from 'react';
import DashboardShell from './DashboardShell';
import api from '../api';
import SupervisorBrowseTab from './SupervisorBrowseTab';

const TABS = [
  { id: 'browse',       label: 'Browse'       },
  { id: 'submissions', label: 'Submissions'  },
  { id: 'my-expertise', label: 'My Expertise' },
];

export default function SupervisorDashboard() {
  const [submissions, setSubmissions] = useState([]);
  const [loading, setLoading] = useState(false);

  const fetchSubmissions = async () => {
    try {
      setLoading(true);
      const res = await api.get('/courseworks/active');
      const data = Array.isArray(res.data) ? res.data : (res.data.data || []);
      setSubmissions(data);
    } catch (err) {
      console.error('Failed to fetch submissions', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSubmissions();
  }, []);

  return (
    <DashboardShell
      portalName="SUPERVISOR PORTAL"
      tabs={TABS}
      roleClass="supervisor"
    >
      {({ activeTab }) => (
        <>
          {activeTab === 'browse' && <SupervisorBrowseTab />}   

          {activeTab === 'submissions' && (
            <div className="tab-content">
              <div className="dash-card">
                <div className="dash-card-header">
                  <div>
                    <div className="dash-card-title">Submissions</div>
                    <div className="dash-card-subtitle">
                      View active submission tasks for students
                    </div>
                  </div>
                  <button className="btn btn-secondary" onClick={fetchSubmissions}>
                    Refresh
                  </button>
                </div>

                {loading ? (
                  <div className="loading-state">Loading...</div>
                ) : submissions.length === 0 ? (
                  <div className="empty-state">
                    <p>No active submissions.</p>
                  </div>
                ) : (
                  <div className="area-grid">
                    {submissions.map((sub) => (
                      <div key={sub.courseworkId} className="area-card">
                        <div className="area-name">{sub.title}</div>
                        <div className="submission-type-badge">
                          {sub.isIndividual ? 'Individual' : 'Group'}
                        </div>
                        {sub.description && (
                          <div className="area-desc">{sub.description}</div>
                        )}
                        {sub.deadline && (
                          <div className="area-deadline">
                            Deadline: {new Date(sub.deadline).toLocaleDateString()}
                          </div>
                        )}
                        {sub.moduleLeaderName && (
                          <div className="area-meta">
                            Module Leader: {sub.moduleLeaderName}
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          )}

          {activeTab === 'my-expertise' && (
            <div className="tab-content">
              <div className="dash-card">
                <div className="dash-card-title">🔬 My Expertise</div>
                <p style={{ color: 'var(--gray-500)', fontSize: 14, lineHeight: 1.7 }}>
                  Manage your research areas and areas of expertise so students can find
                  the right supervisor for their project.
                </p>
              </div>
            </div>
          )}
        </>
      )}
    </DashboardShell>
  );
}
