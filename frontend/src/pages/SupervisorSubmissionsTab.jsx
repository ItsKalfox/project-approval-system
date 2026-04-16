import { useState, useEffect, useCallback } from 'react';
import api from '../api';

export default function SupervisorSubmissionsTab() {
  const [matchedProjects, setMatchedProjects] = useState([]);
  const [pendingReviews, setPendingReviews]   = useState([]);
  const [loading, setLoading]                 = useState(false);
  const [error, setError]                     = useState('');
  const [selected, setSelected]               = useState(null);
  const [pdfUrl, setPdfUrl]                   = useState(null);
  const [pdfLoading, setPdfLoading]           = useState(false);

  const fetchSubmissions = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const res = await api.get('/supervisor/dashboard/submissions');
      setMatchedProjects(res.data.data?.matchedProjects ?? []);
      setPendingReviews(res.data.data?.pendingReviews ?? []);
    } catch {
      setError('Failed to load submissions.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchSubmissions(); }, [fetchSubmissions]);

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

  const ProjectCard = ({ project, badge }) => (
    <div className="project-card" onClick={() => openProject(project)}>
      <div className="project-card-header">
        <span className="research-area-badge">{project.researchAreaName || 'General'}</span>
        {project.hasProposalPdf && <span className="pdf-badge">PDF</span>}
        {badge}
      </div>
      <div className="project-card-title">{project.title}</div>
      {project.abstract && (
        <div className="project-card-abstract">{project.abstract}</div>
      )}
      {project.technicalStack && (
        <div className="project-card-stack"><strong>Stack:</strong> {project.technicalStack}</div>
      )}
      <div className="project-card-footer">
        {project.submittedAt && (
          <span className="project-date">
            {new Date(project.submittedAt).toLocaleDateString()}
          </span>
        )}
        <span className={`interest-status ${project.alreadyExpressedInterest ? 'matched' : 'pending'}`}>
          {project.alreadyExpressedInterest ? '✓ Matched' : 'Pending'}
        </span>
      </div>
    </div>
  );

  return (
    <div className="tab-content">
      <div className="dash-card">
        <div className="dash-card-header">
          <div>
            <div className="dash-card-title">Submissions</div>
            <div className="dash-card-subtitle">
              Your matched projects and projects awaiting your review.
            </div>
          </div>
          <button className="btn btn-secondary" onClick={fetchSubmissions} disabled={loading}>
            {loading ? 'Refreshing…' : 'Refresh'}
          </button>
        </div>

        {error && <div className="alert alert-error">{error}</div>}
        {loading && <div className="loading-state">Loading submissions…</div>}

        {!loading && (
          <>
            {/* Matched Projects */}
            <div className="submissions-section">
              <div className="submissions-section-header">
                <span className="submissions-section-title">Matched Projects</span>
                <span className="submissions-count matched-count">{matchedProjects.length}</span>
              </div>

              {matchedProjects.length === 0 ? (
                <div className="submissions-empty">
                  <p>No matched projects yet. Express interest in proposals from the Browse tab.</p>
                </div>
              ) : (
                <div className="project-grid">
                  {matchedProjects.map(p => (
                    <ProjectCard key={p.projectId} project={p} />
                  ))}
                </div>
              )}
            </div>

            {/* Pending Reviews */}
            <div className="submissions-section">
              <div className="submissions-section-header">
                <span className="submissions-section-title">Pending Reviews</span>
                <span className="submissions-count pending-count">{pendingReviews.length}</span>
              </div>

              {pendingReviews.length === 0 ? (
                <div className="submissions-empty">
                  <p>No pending proposals waiting for review.</p>
                </div>
              ) : (
                <div className="project-grid">
                  {pendingReviews.map(p => (
                    <ProjectCard key={p.projectId} project={p} />
                  ))}
                </div>
              )}
            </div>
          </>
        )}
      </div>

      {/* Project Detail Modal */}
      {selected && (
        <div className="modal-overlay modal-blur" onClick={closeProject}>
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
                <span className={`interest-status ${selected.alreadyExpressedInterest ? 'matched' : 'pending'}`}>
                  {selected.alreadyExpressedInterest ? '✓ Matched' : 'Pending'}
                </span>
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
              <button className="btn btn-secondary" onClick={closeProject}>Close</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}