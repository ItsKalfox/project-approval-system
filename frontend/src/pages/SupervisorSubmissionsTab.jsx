import { useState, useEffect, useCallback } from 'react';
import api from '../api';

export default function SupervisorSubmissionsTab({ selectedAreas = [] }) {
  const [matchedProjects, setMatchedProjects] = useState([]);
  const [pendingReviews, setPendingReviews]   = useState([]);
  const [revealMap, setRevealMap]             = useState({});
  const [loading, setLoading]                 = useState(false);
  const [error, setError]                     = useState('');
  const [selected, setSelected]               = useState(null);
  const [pdfUrl, setPdfUrl]                   = useState(null);
  const [pdfLoading, setPdfLoading]           = useState(false);

  const fetchSubmissions = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const params = new URLSearchParams();
      if (selectedAreas.length > 0) {
        selectedAreas.forEach(areaId => params.append('researchAreaIds', areaId.toString()));
      }
      const queryString = params.toString();
      
      const [subRes, revealRes] = await Promise.all([
        api.get(`/supervisor/dashboard/submissions${queryString ? '?' + queryString : ''}`),
        api.get('/supervisor/dashboard/matched-revealed'),
      ]);
      setMatchedProjects(subRes.data.data?.matchedProjects ?? []);
      setPendingReviews(subRes.data.data?.pendingReviews ?? []);
      const map = {};
      for (const p of (revealRes.data.data ?? [])) {
        map[p.projectId] = { studentName: p.studentName, studentEmail: p.studentEmail, studentBatch: p.studentBatch, matchedAt: p.matchedAt };
      }
      setRevealMap(map);
    } catch {
      setError('Failed to load submissions.');
    } finally {
      setLoading(false);
    }
  }, [selectedAreas]);

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

  const RevealCard = ({ student }) => {
    const [flipped, setFlipped] = useState(false);
    return (
      <div style={{ perspective: 800, marginTop: 8 }}>
        <div style={{
          width: '100%', height: 110, position: 'relative',
          transformStyle: 'preserve-3d',
          transition: 'transform 0.6s cubic-bezier(.4,2,.6,1)',
          transform: flipped ? 'rotateY(180deg)' : 'none',
        }}>
          {/* Front */}
          <div style={{
            position: 'absolute', inset: 0, backfaceVisibility: 'hidden',
            background: 'linear-gradient(135deg,#10b981,#059669)',
            borderRadius: 10, display: 'flex', flexDirection: 'column',
            alignItems: 'center', justifyContent: 'center', gap: 6, color: '#fff',
          }}>
            <span style={{ fontSize: 26 }}>*</span>
            <span style={{ fontWeight: 700, fontSize: 14 }}>Student Identity Revealed!</span>
            <button
              onClick={() => setFlipped(true)}
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
            <div style={{ fontSize: 11, textTransform: 'uppercase', letterSpacing: 1, color: '#94a3b8' }}>Your Student</div>
            <div style={{ fontWeight: 700, fontSize: 16, color: '#1e293b' }}>{student.studentName}</div>
            <div style={{ fontSize: 13, color: '#6366f1' }}>{student.studentEmail}</div>
            {student.studentBatch && (
              <div style={{ fontSize: 12, color: '#64748b' }}>Batch: {student.studentBatch}</div>
            )}
            {student.matchedAt && (
              <div style={{ fontSize: 11, color: '#94a3b8', marginTop: 2 }}>
                Matched on {new Date(student.matchedAt).toLocaleDateString()}
              </div>
            )}
            <button
              onClick={() => setFlipped(false)}
              style={{ marginTop: 6, background: 'none', border: 'none', color: '#94a3b8', cursor: 'pointer', fontSize: 11 }}
            >
              Flip Back
            </button>
          </div>
        </div>
      </div>
    );
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
              Review project proposals and track your matched students.
              Student identities are hidden until a match is confirmed.
            </div>
          </div>
          <button className="btn btn-secondary" onClick={fetchSubmissions} disabled={loading}>
            {loading ? 'Refreshing…' : '↻ Refresh'}
          </button>
        </div>

        {error && <div className="alert alert-error">{error}</div>}
        {loading && <div className="loading-state">Loading submissions…</div>}

        {!loading && (
          <>
            {/* Matched Projects */}
            <div className="submissions-section">
              <div className="submissions-section-header">
                <span className="submissions-section-title" style={{ color: '#6366f1', fontWeight: 700 }}>
                  Matched Projects
                </span>
                <span className="submissions-count matched-count"
                  style={{ background: '#6366f1', color: '#fff', borderRadius: 20, padding: '2px 10px', fontSize: 13 }}>
                  {matchedProjects.length}
                </span>
              </div>

              {matchedProjects.length === 0 ? (
                <div className="submissions-empty">
                  <p>No matched projects yet. Express interest in proposals from the Browse tab.</p>
                </div>
              ) : (
                <div className="project-grid">
                  {matchedProjects.map(p => (
                    <div key={p.projectId}>
                      <ProjectCard project={p} />
                      {revealMap[p.projectId] && (
                        <RevealCard student={revealMap[p.projectId]} />
                      )}
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* Pending Reviews */}
            <div className="submissions-section">
              <div className="submissions-section-header">
                <span className="submissions-section-title" style={{ color: '#f59e0b', fontWeight: 700 }}>
                  Pending Reviews
                </span>
                <span className="submissions-count pending-count"
                  style={{ background: '#f59e0b', color: '#fff', borderRadius: 20, padding: '2px 10px', fontSize: 13 }}>
                  {pendingReviews.length}
                </span>
              </div>

              {pendingReviews.length === 0 ? (
                <div className="submissions-empty">
                  <p>No pending proposals waiting for review.</p>
                </div>
              ) : (
                <>
                  <p style={{ fontSize: 12, color: '#94a3b8', marginBottom: 10 }}>
                    Student identities are hidden. Express interest to reveal them after matching.
                  </p>
                  <div className="project-grid">
                    {pendingReviews.map(p => (
                      <ProjectCard key={p.projectId} project={p} />
                    ))}
                  </div>
                </>
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
                        {pdfLoading ? 'Loading...' : 'View Proposal'}
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