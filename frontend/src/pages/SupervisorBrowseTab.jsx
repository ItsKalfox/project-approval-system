import { useState, useEffect } from 'react';
import api from '../api';

export default function SupervisorBrowseTab() {
  const [courseworks, setCourseworks]               = useState([]);
  const [researchAreas, setResearchAreas]           = useState([]);
  const [selectedCoursework, setSelectedCoursework] = useState(null);
  const [projects, setProjects]                     = useState([]);
  const [selectedArea, setSelectedArea]             = useState('');
  const [loadingCW, setLoadingCW]                   = useState(false);
  const [loadingProjects, setLoadingProjects]       = useState(false);
  const [error, setError]                           = useState('');
  const [selected, setSelected]                     = useState(null);
  const [pdfUrl, setPdfUrl]                         = useState(null);
  const [pdfLoading, setPdfLoading]                 = useState(false);
  const [interestMap, setInterestMap]               = useState({});

  useEffect(() => {
    const fetchLookups = async () => {
      setLoadingCW(true);
      try {
        const [cwRes, raRes] = await Promise.all([
          api.get('/supervisor/lookup/courseworks'),
          api.get('/supervisor/lookup/research-areas')
        ]);
        setCourseworks(cwRes.data.data ?? []);
        setResearchAreas(raRes.data.data ?? []);
      } catch {
        setError('Failed to load courseworks.');
      } finally {
        setLoadingCW(false);
      }
    };
    fetchLookups();
  }, []);

  const loadProjects = async (coursework, areaId = '') => {
    setLoadingProjects(true);
    setError('');
    try {
      const url = areaId
        ? `/supervisor/dashboard/projects?courseworkId=${coursework.courseworkId}&researchAreaId=${areaId}`
        : `/supervisor/dashboard/projects?courseworkId=${coursework.courseworkId}`;
      const res  = await api.get(url);
      const data = Array.isArray(res.data) ? res.data : (res.data.data ?? []);
      setProjects(data);
    } catch {
      setError('Failed to load projects.');
    } finally {
      setLoadingProjects(false);
    }
  };

  const openCoursework = (coursework) => {
    setSelectedCoursework(coursework);
    setSelectedArea('');
    setProjects([]);
    loadProjects(coursework);
  };

  const backToCourseworks = () => {
    setSelectedCoursework(null);
    setProjects([]);
    setSelectedArea('');
    setSelected(null);
    if (pdfUrl) URL.revokeObjectURL(pdfUrl);
    setPdfUrl(null);
  };

  const openProject = (project) => {
    setSelected(project);
    setPdfUrl(null);
  };

  const closeProject = () => {
    if (pdfUrl) URL.revokeObjectURL(pdfUrl);
    setSelected(null);
    setPdfUrl(null);
  };

  const loadPdf = async () => {
    if (!selected || pdfLoading) return;
    setPdfLoading(true);
    try {
      const res = await api.get(
        `/supervisor/dashboard/projects/${selected.projectId}/proposal`,
        { responseType: 'blob' }
      );
      setPdfUrl(URL.createObjectURL(new Blob([res.data], { type: 'application/pdf' })));
    } catch {
      alert('Could not load proposal PDF.');
    } finally {
      setPdfLoading(false);
    }
  };

  const expressInterest = async (projectId) => {
    setInterestMap(m => ({ ...m, [projectId]: 'loading' }));
    try {
      await api.post(`/supervisor/dashboard/projects/${projectId}/interest`);
      setInterestMap(m => ({ ...m, [projectId]: 'done' }));
      setProjects(prev =>
        prev.map(p => p.projectId === projectId ? { ...p, alreadyExpressedInterest: true } : p)
      );
      if (selected?.projectId === projectId)
        setSelected(prev => ({ ...prev, alreadyExpressedInterest: true }));
    } catch (err) {
      setInterestMap(m => ({ ...m, [projectId]: 'error' }));
      alert(err.response?.data?.message ?? 'Could not express interest.');
    }
  };

  const withdrawInterest = async (projectId) => {
    setInterestMap(m => ({ ...m, [projectId]: 'loading' }));
    try {
      await api.delete(`/supervisor/dashboard/projects/${projectId}/interest`);
      setInterestMap(m => ({ ...m, [projectId]: 'idle' }));
      setProjects(prev =>
        prev.map(p => p.projectId === projectId ? { ...p, alreadyExpressedInterest: false } : p)
      );
      if (selected?.projectId === projectId)
        setSelected(prev => ({ ...prev, alreadyExpressedInterest: false }));
    } catch (err) {
      setInterestMap(m => ({ ...m, [projectId]: 'error' }));
      alert(err.response?.data?.message ?? 'Could not withdraw interest.');
    }
  };

  const InterestButton = ({ project }) => {
    const state = interestMap[project.projectId];
    if (project.alreadyExpressedInterest && state !== 'idle') {
      return (
        <button
          className="btn btn-danger btn-sm"
          disabled={state === 'loading'}
          onClick={(e) => { e.stopPropagation(); withdrawInterest(project.projectId); }}
        >
          {state === 'loading' ? 'Withdrawing…' : 'Withdraw Interest'}
        </button>
      );
    }
    return (
      <button
        className="btn btn-primary btn-sm"
        disabled={state === 'loading'}
        onClick={(e) => { e.stopPropagation(); expressInterest(project.projectId); }}
      >
        {state === 'loading' ? 'Sending…' : '+ Express Interest'}
      </button>
    );
  };

  const InterestStatus = ({ project }) => {
    if (project.alreadyExpressedInterest && interestMap[project.projectId] !== 'idle') {
      return <span className="interest-status matched">✓ Matched</span>;
    }
    return <span className="interest-status pending">Pending</span>;
  };

  // ── Coursework cards view ──────────────────────────────────────
  if (!selectedCoursework) {
    return (
      <div className="tab-content">
        <div className="dash-card">
          <div className="dash-card-header">
            <div>
              <div className="dash-card-title">Browse Proposals</div>
              <div className="dash-card-subtitle">
                Select a coursework to view its submitted proposals.
              </div>
            </div>
          </div>

          {error && <div className="alert alert-error">{error}</div>}
          {loadingCW && <div className="loading-state">Loading courseworks…</div>}

          {!loadingCW && courseworks.length === 0 && !error && (
            <div className="empty-state"><p>No courseworks available.</p></div>
          )}

          {!loadingCW && courseworks.length > 0 && (
            <div className="project-grid">
              {courseworks.map(c => (
                <div
                  key={c.courseworkId}
                  className="coursework-card"
                  onClick={() => openCoursework(c)}
                >
                  <div className="coursework-card-title">{c.title}</div>
                  {c.description && (
                    <div className="coursework-card-desc">{c.description}</div>
                  )}
                  <div className="coursework-card-footer">
                    {c.deadline && (
                      <span className="project-date">
                        Deadline: {new Date(c.deadline).toLocaleDateString()}
                      </span>
                    )}
                    <span className="coursework-type-badge">
                      {c.isIndividual ? 'Individual' : 'Group'}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    );
  }
  // ── Projects panel view ────────────────────────────────────────
  return (
    <div className="tab-content">
      <div className="dash-card">
        <div className="dash-card-header">
          <div>
            <button className="btn-back" onClick={backToCourseworks}>
              ← Back to Courseworks
            </button>
            <div className="dash-card-title" style={{ marginTop: 8 }}>
              {selectedCoursework.title}
            </div>
            <div className="dash-card-subtitle">
              Student identities are hidden until a match is confirmed.
            </div>
          </div>
          <button
            className="btn btn-secondary"
            onClick={() => loadProjects(selectedCoursework, selectedArea)}
            disabled={loadingProjects}
          >
            {loadingProjects ? 'Refreshing…' : 'Refresh'}
          </button>
        </div>

        {/* Research Area Filters */}
        <div className="filter-bar">
          <button
            className={`filter-btn ${selectedArea === '' ? 'active' : ''}`}
            onClick={() => { setSelectedArea(''); loadProjects(selectedCoursework); }}
          >
            All
          </button>
          {researchAreas.map(area => (
            <button
              key={area.researchAreaId}
              className={`filter-btn ${selectedArea === String(area.researchAreaId) ? 'active' : ''}`}
              onClick={() => {
  if (selectedArea === String(area.researchAreaId)) {
    setSelectedArea('');
    loadProjects(selectedCoursework);
  } else {
    setSelectedArea(String(area.researchAreaId));
    loadProjects(selectedCoursework, area.researchAreaId);
  }
}}
            >
              {area.name}
            </button>
          ))}
        </div>

        {error && <div className="alert alert-error">{error}</div>}
        {loadingProjects && <div className="loading-state">Loading proposals…</div>}

        {!loadingProjects && !error && projects.length === 0 && (
          <div className="empty-state">
            <p>No submitted proposals found{selectedArea ? ' for this research area' : ''}.</p>
          </div>
        )}

        {!loadingProjects && projects.length > 0 && (
          <div className="project-grid">
            {projects.map(p => (
              <div key={p.projectId} className="project-card" onClick={() => openProject(p)}>
                <div className="project-card-header">
                  <span className="research-area-badge">{p.researchAreaName || 'General'}</span>
                  {p.hasProposalPdf && <span className="pdf-badge">PDF</span>}
                </div>
                <div className="project-card-title">{p.title}</div>
                {p.abstract && (
                  <div className="project-card-abstract">{p.abstract}</div>
                )}
                {p.technicalStack && (
                  <div className="project-card-stack"><strong>Stack:</strong> {p.technicalStack}</div>
                )}
                <div className="project-card-footer">
                  <InterestStatus project={p} />
                  <InterestButton project={p} />
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Project Detail Modal */}
      {selected && (
        <div className="modal-overlay" onClick={closeProject}>
          <div className="modal-box" onClick={e => e.stopPropagation()}>
            <div className="modal-header">
              <div>
                <span className="research-area-badge">{selected.researchAreaName || 'General'}</span>
                <h2 className="modal-title">{selected.title}</h2>
              </div>
              <button className="modal-close" onClick={closeProject}>✕</button>
            </div>

            <div className="modal-body">
              {selected.abstract && (
                <section className="modal-section">
                  <h4>Abstract</h4>
                  <p>{selected.abstract}</p>
                </section>
              )}
              {selected.description && (
                <section className="modal-section">
                  <h4>Description</h4>
                  <p>{selected.description}</p>
                </section>
              )}
              {selected.technicalStack && (
                <section className="modal-section">
                  <h4>Technical Stack</h4>
                  <p>{selected.technicalStack}</p>
                </section>
              )}
              <section className="modal-section">
                <h4>Interest Status</h4>
                <InterestStatus project={selected} />
              </section>
              {selected.hasProposalPdf && (
                <section className="modal-section">
                  <h4>Proposal PDF</h4>
                  {!pdfUrl
                    ? <button className="btn btn-secondary" onClick={loadPdf} disabled={pdfLoading}>
                        {pdfLoading ? 'Loading…' : '📄 View Proposal'}
                      </button>
                    : <iframe src={pdfUrl} title="Proposal PDF" className="pdf-viewer" />
                  }
                </section>
              )}
            </div>

            <div className="modal-footer">
              <InterestButton project={selected} />
              <button className="btn btn-secondary" onClick={closeProject}>Close</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}