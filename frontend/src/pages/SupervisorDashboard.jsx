import { useState, useEffect } from 'react';
import DashboardShell from './DashboardShell';
import api from '../api';
import SupervisorBrowseTab from './SupervisorBrowseTab';
import SupervisorSubmissionsTab from './SupervisorSubmissionsTab';

const TABS = [
  { id: 'browse',       label: 'Browse'       },
  { id: 'submissions', label: 'Submissions'  },
  { id: 'my-expertise', label: 'My Expertise' },
];

export default function SupervisorDashboard() {
  const [submissions, setSubmissions] = useState([]);
  const [loading, setLoading] = useState(false);
  const [researchAreas, setResearchAreas] = useState([]);
  const [selectedAreas, setSelectedAreas] = useState([]);

  const loadExpertise = () => {
    const saved = localStorage.getItem('supervisor_expertise_areas');
    if (saved) {
      try {
        const parsed = JSON.parse(saved);
        if (Array.isArray(parsed)) {
          setSelectedAreas(parsed);
        }
      } catch {
        setSelectedAreas([]);
      }
    }
  };

  const saveExpertise = async (areaIds) => {
    setSelectedAreas(areaIds);
    localStorage.setItem('supervisor_expertise_areas', JSON.stringify(areaIds));
  };

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

  const fetchResearchAreas = async () => {
    try {
      const res = await api.get('/supervisor/lookup/research-areas');
      setResearchAreas(res.data.data || []);
    } catch (err) {
      console.error('Failed to fetch research areas', err);
    }
  };

  useEffect(() => {
    fetchSubmissions();
    fetchResearchAreas();
    loadExpertise();
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

          {activeTab === 'submissions' && <SupervisorSubmissionsTab selectedAreas={selectedAreas} />}

          {activeTab === 'my-expertise' && (
            <div className="tab-content">
              <div className="dash-card">
                <div className="dash-card-title">My Expertise</div>
                <p style={{ color: 'var(--gray-500)', fontSize: 14, lineHeight: 1.7 }}>
                  Manage your research areas and areas of expertise so students can find
                  the right supervisor for their project.
                </p>
                
                <div style={{ marginTop: 24 }}>
                  <div style={{ fontWeight: 600, marginBottom: 12, color: 'var(--gray-700)' }}>
                    Select your research expertise areas (1-2 areas):
                  </div>
                  <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
                    {researchAreas.map((area) => {
                      const areaId = area.researchAreaId ?? area.id;
                      const isSelected = selectedAreas.includes(areaId);
                      return (
                        <label
                          key={areaId}
                          style={{
                            display: 'flex',
                            alignItems: 'center',
                            gap: 10,
                            padding: '10px 14px',
                            border: `1.5px solid ${isSelected ? 'var(--primary)' : 'var(--gray-200)'}`,
                            borderRadius: 8,
                            background: isSelected ? 'rgba(99, 102, 241, 0.05)' : 'white',
                            cursor: 'pointer',
                            transition: 'all 0.2s ease',
                          }}
                        >
                          <input
                            type="checkbox"
                            checked={isSelected}
                            onChange={(e) => {
                              const newAreas = e.target.checked
                                ? selectedAreas.length < 2
                                  ? [...selectedAreas, areaId]
                                  : selectedAreas
                                : selectedAreas.filter(id => id !== areaId);
                              if (e.target.checked && selectedAreas.length >= 2) {
                                alert('You can select at most 2 research areas.');
                                return;
                              }
                              saveExpertise(newAreas);
                            }}
                            style={{ width: 18, height: 18, accentColor: 'var(--primary)' }}
                          />
                          <span style={{ fontWeight: 500, color: 'var(--gray-800)' }}>{area.name}</span>
                        </label>
                      );
                    })}
                  </div>
                  {researchAreas.length === 0 && (
                    <p style={{ color: 'var(--gray-400)', fontSize: 13, marginTop: 8 }}>
                      No research areas available. Please contact the administrator.
                    </p>
                  )}
                  <p style={{ fontSize: 12, color: 'var(--gray-400)', marginTop: 12 }}>
                    Selected: {selectedAreas.length}/2 areas
                  </p>
                </div>
              </div>
            </div>
          )}
        </>
      )}
    </DashboardShell>
  );
}
