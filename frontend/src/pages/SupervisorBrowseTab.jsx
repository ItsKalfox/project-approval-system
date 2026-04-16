import { useState, useEffect, useCallback } from 'react';
import api from '../api';

export default function SupervisorBrowseTab() {
  const [projects, setProjects]       = useState([]);
  const [loading, setLoading]         = useState(false);
  const [error, setError]             = useState('');
  const [selected, setSelected]       = useState(null);
  const [pdfUrl, setPdfUrl]           = useState(null);
  const [pdfLoading, setPdfLoading]   = useState(false);
  const [interestMap, setInterestMap] = useState({});

  const fetchProjects = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const res  = await api.get('/supervisor/dashboard/projects');
      const data = Array.isArray(res.data) ? res.data : (res.data.data ?? []);
      setProjects(data);
    } catch {
      setError('Failed to load projects. Please try again.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchProjects(); }, [fetchProjects]);

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

  return (
    <div className="tab-content">
      <div className="dash-card">
        <div className="dash-card-header">
          <div>
            <div className="dash-card-title">Browse Proposals</div>
            <div className="dash-card-subtitle">
              Student identities are hidden until a match is confirmed.
            </div>
          </div>
          <button className="btn btn-secondary" onClick={fetchProjects} disabled={loading}>
            {loading ? 'Refreshing…' : 'Refresh'}
          </button>
        </div>

        {error && <div className="alert alert-error">{error}</div>}
        {loading && <div className="loading-state">Loading proposals…</div>}

        {!loading && !error && projects.length === 0 && (
          <div className="empty-state"><p>No submitted proposals available right now.</p></div>
        )}

        {!loading && projects.length > 0 && (
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
                  {p.submittedAt && (
                    <span className="project-date">
                      {new Date(p.submittedAt).toLocaleDateString()}
                    </span>
                  )}
                  <InterestButton project={p} />
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

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