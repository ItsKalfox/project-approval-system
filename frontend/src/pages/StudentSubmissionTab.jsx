import { useState, useEffect, useCallback } from 'react';
import api from '../api';

/* ═══════════════════════════════════════════════════════════════════════════
   Student Submission Tab
   ─────────────────────────────────────────────────────────────────────────
   Shows submission points (active courseworks with open deadlines).
   Student can: create, view, edit, delete a submission, view the PDF.
   After deadline: read-only mode — no create/edit/delete.
   ═══════════════════════════════════════════════════════════════════════════ */

export default function StudentSubmissionTab() {
  // ── state ──────────────────────────────────────────────────────────────
  const [submissionPoints, setSubmissionPoints] = useState([]);
  const [researchAreas, setResearchAreas] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [snack, setSnack] = useState({ msg: '', type: '', show: false });

  // Currently selected submission point
  const [selectedPoint, setSelectedPoint] = useState(null);
  // The student's existing submission for selectedPoint
  const [mySubmission, setMySubmission] = useState(null);
  const [loadingSub, setLoadingSub] = useState(false);

  // Modal / form state
  const [showForm, setShowForm] = useState(false); // create / edit form
  const [isEditing, setIsEditing] = useState(false);
  const [formData, setFormData] = useState({
    Title: '',
    Description: '',
    Abstract: '',
    ResearchAreaId: '',
  });
  const [file, setFile] = useState(null);
  const [saving, setSaving] = useState(false);

  // Confirm delete dialog
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [deleting, setDeleting] = useState(false);

  // PDF viewer
  const [viewingPdf, setViewingPdf] = useState(false);
  const [pdfUrl, setPdfUrl] = useState('');
  const [loadingPdf, setLoadingPdf] = useState(false);

  // ── helpers ────────────────────────────────────────────────────────────
  const flash = useCallback((msg, type = 'success') => {
    setSnack({ msg, type, show: true });
    setTimeout(() => setSnack(s => ({ ...s, show: false })), 3500);
  }, []);

  const isDeadlinePassed = (deadline) => {
    if (!deadline) return false;
    return new Date(deadline) <= new Date();
  };

  const formatDate = (iso) => {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString('en-GB', {
      day: '2-digit', month: 'short', year: 'numeric',
    });
  };

  const formatDateTime = (iso) => {
    if (!iso) return '—';
    return new Date(iso).toLocaleString('en-GB', {
      day: '2-digit', month: 'short', year: 'numeric',
      hour: '2-digit', minute: '2-digit',
    });
  };

  const formatFileSize = (bytes) => {
    if (!bytes) return '—';
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
  };

  // ── data fetching ──────────────────────────────────────────────────────
  const fetchSubmissionPoints = useCallback(async () => {
    try {
      setLoading(true);
      setError('');
      const [spRes, raRes] = await Promise.all([
        api.get('/submissions/submission-points'),
        api.get('/submissions/research-areas'),
      ]);
      setSubmissionPoints(spRes.data.data || []);
      const areas = raRes.data.data || raRes.data || [];
      setResearchAreas(Array.isArray(areas) ? areas : []);
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load submission points.');
    } finally {
      setLoading(false);
    }
  }, []);

  const fetchMySubmission = useCallback(async (courseworkId) => {
    try {
      setLoadingSub(true);
      const res = await api.get(`/submissions/coursework/${courseworkId}`);
      setMySubmission(res.data.data || null);
    } catch (err) {
      if (err.response?.status === 404) {
        setMySubmission(null);
      } else {
        flash(err.response?.data?.message || 'Failed to load submission.', 'error');
      }
    } finally {
      setLoadingSub(false);
    }
  }, [flash]);

  useEffect(() => {
    fetchSubmissionPoints();
  }, [fetchSubmissionPoints]);

  // When a submission point is selected, load the student's submission
  useEffect(() => {
    if (selectedPoint) {
      fetchMySubmission(selectedPoint.courseworkId);
    }
  }, [selectedPoint, fetchMySubmission]);

  // ── SELECT a submission point ──────────────────────────────────────────
  const handleSelectPoint = (point) => {
    setSelectedPoint(point);
    setMySubmission(null);
    setShowForm(false);
    setViewingPdf(false);
    setPdfUrl('');
  };

  const handleBackToList = () => {
    setSelectedPoint(null);
    setMySubmission(null);
    setShowForm(false);
    setViewingPdf(false);
    if (pdfUrl) URL.revokeObjectURL(pdfUrl);
    setPdfUrl('');
    fetchSubmissionPoints();
  };

  // ── CREATE submission ──────────────────────────────────────────────────
  const openCreateForm = () => {
    setIsEditing(false);
    setFormData({ Title: '', Description: '', Abstract: '', ResearchAreaId: '' });
    setFile(null);
    setShowForm(true);
  };

  // ── EDIT submission ────────────────────────────────────────────────────
  const openEditForm = () => {
    if (!mySubmission) return;
    setIsEditing(true);
    setFormData({
      Title: mySubmission.title || '',
      Description: mySubmission.description || '',
      Abstract: mySubmission.abstract || '',
      ResearchAreaId: mySubmission.researchAreaId?.toString() || '',
    });
    setFile(null);
    setShowForm(true);
  };

  // ── SUBMIT form (create or update) ────────────────────────────────────
  const handleFormSubmit = async (e) => {
    e.preventDefault();

    // Validation
    if (!formData.Title.trim()) { flash('Title is required.', 'error'); return; }
    if (!formData.Description.trim()) { flash('Description is required.', 'error'); return; }
    if (!formData.Abstract.trim()) { flash('Abstract is required.', 'error'); return; }
    if (!formData.ResearchAreaId) { flash('Please select a research area.', 'error'); return; }
    if (!isEditing && !file) { flash('Please upload a PDF file.', 'error'); return; }

    const fd = new FormData();
    fd.append('Title', formData.Title.trim());
    fd.append('Description', formData.Description.trim());
    fd.append('Abstract', formData.Abstract.trim());
    fd.append('ResearchAreaId', formData.ResearchAreaId);
    if (file) fd.append('file', file);

    try {
      setSaving(true);
      if (isEditing && mySubmission) {
        await api.put(`/submissions/${mySubmission.projectId}`, fd, {
          headers: { 'Content-Type': 'multipart/form-data' },
        });
        flash('Submission updated successfully!');
      } else {
        await api.post(`/submissions/coursework/${selectedPoint.courseworkId}`, fd, {
          headers: { 'Content-Type': 'multipart/form-data' },
        });
        flash('Submission created successfully!');
      }
      setShowForm(false);
      setFile(null);
      fetchMySubmission(selectedPoint.courseworkId);
      fetchSubmissionPoints();
    } catch (err) {
      flash(err.response?.data?.message || 'Failed to save submission.', 'error');
    } finally {
      setSaving(false);
    }
  };

  // ── DELETE submission ──────────────────────────────────────────────────
  const handleDelete = async () => {
    if (!mySubmission) return;
    try {
      setDeleting(true);
      await api.delete(`/submissions/${mySubmission.projectId}`);
      flash('Submission deleted successfully.');
      setShowDeleteConfirm(false);
      setMySubmission(null);
      fetchSubmissionPoints();
    } catch (err) {
      flash(err.response?.data?.message || 'Failed to delete submission.', 'error');
    } finally {
      setDeleting(false);
    }
  };

  // ── VIEW PDF ───────────────────────────────────────────────────────────
  const handleViewPdf = async () => {
    if (!mySubmission) return;
    try {
      setLoadingPdf(true);
      const res = await api.get(`/submissions/${mySubmission.projectId}/file`, {
        responseType: 'blob',
      });
      const url = URL.createObjectURL(res.data);
      setPdfUrl(url);
      setViewingPdf(true);
    } catch (err) {
      flash(err.response?.data?.message || 'Failed to load PDF.', 'error');
    } finally {
      setLoadingPdf(false);
    }
  };

  const closePdfViewer = () => {
    setViewingPdf(false);
    if (pdfUrl) URL.revokeObjectURL(pdfUrl);
    setPdfUrl('');
  };

  // ═══════════════════════════════════════════════════════════════════════
  //  RENDER
  // ═══════════════════════════════════════════════════════════════════════

  // ── PDF VIEWER (full-screen overlay) ───────────────────────────────────
  if (viewingPdf && pdfUrl) {
    return (
      <div className="tab-content">
        <div className="dash-card" style={{ padding: 0, overflow: 'hidden' }}>
          <div className="sub-pdf-header">
            <button className="btn btn-secondary btn-sm" onClick={closePdfViewer}>
              ← Back
            </button>
            <span className="sub-pdf-title">
              📄 {mySubmission?.proposalFileName || 'Proposal'}
            </span>
            <a href={pdfUrl} download={mySubmission?.proposalFileName || 'proposal.pdf'}
              className="btn btn-primary btn-sm">
              ⬇ Download
            </a>
          </div>
          <iframe
            src={pdfUrl}
            title="PDF Viewer"
            className="sub-pdf-iframe"
          />
        </div>
      </div>
    );
  }

  // ── SUBMISSION FORM (create / edit) ────────────────────────────────────
  if (showForm && selectedPoint) {
    const deadlinePassed = isDeadlinePassed(selectedPoint.deadline);
    return (
      <div className="tab-content">
        <div className="dash-card">
          <div className="sub-form-header">
            <button className="btn btn-secondary btn-sm" onClick={() => setShowForm(false)}>
              ← Back
            </button>
            <h2 className="sub-form-heading">
              {isEditing ? '✏️ Edit Submission' : '📤 New Submission'}
            </h2>
          </div>

          <div className="sub-point-banner">
            <span className="sub-point-banner-title">{selectedPoint.title}</span>
            {selectedPoint.deadline && (
              <span className={`sub-deadline-chip ${deadlinePassed ? 'passed' : ''}`}>
                ⏰ {formatDate(selectedPoint.deadline)}
              </span>
            )}
          </div>

          <form onSubmit={handleFormSubmit} className="sub-form">
            <div className="form-group">
              <label className="form-label">Title *</label>
              <input
                type="text"
                className="form-input"
                value={formData.Title}
                onChange={(e) => setFormData({ ...formData, Title: e.target.value })}
                placeholder="Enter your project title"
                autoFocus
              />
            </div>

            <div className="form-group">
              <label className="form-label">Abstract (Short Description) *</label>
              <textarea
                className="form-input"
                rows={3}
                value={formData.Abstract}
                onChange={(e) => setFormData({ ...formData, Abstract: e.target.value })}
                placeholder="A brief summary of your project (2–3 sentences)"
              />
            </div>

            <div className="form-group">
              <label className="form-label">Description *</label>
              <textarea
                className="form-input"
                rows={5}
                value={formData.Description}
                onChange={(e) => setFormData({ ...formData, Description: e.target.value })}
                placeholder="Detailed description of your project proposal..."
              />
            </div>

            <div className="form-group">
              <label className="form-label">Research Area *</label>
              <select
                className="form-input sub-select"
                value={formData.ResearchAreaId}
                onChange={(e) => setFormData({ ...formData, ResearchAreaId: e.target.value })}
              >
                <option value="">— Select a research area —</option>
                {researchAreas.map((area) => (
                  <option key={area.id} value={area.id}>{area.name}</option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label className="form-label">
                Proposal File (PDF) {isEditing ? '' : '*'}
              </label>
              <div className="sub-file-drop"
                onDragOver={(e) => { e.preventDefault(); e.currentTarget.classList.add('dragover'); }}
                onDragLeave={(e) => { e.currentTarget.classList.remove('dragover'); }}
                onDrop={(e) => {
                  e.preventDefault();
                  e.currentTarget.classList.remove('dragover');
                  const dropped = e.dataTransfer.files[0];
                  if (dropped && dropped.type === 'application/pdf') setFile(dropped);
                  else flash('Only PDF files are allowed.', 'error');
                }}
                onClick={() => document.getElementById('pdf-input').click()}
              >
                <input
                  id="pdf-input"
                  type="file"
                  accept="application/pdf,.pdf"
                  style={{ display: 'none' }}
                  onChange={(e) => {
                    const f = e.target.files?.[0];
                    if (f) setFile(f);
                  }}
                />
                {file ? (
                  <div className="sub-file-info">
                    <span className="sub-file-icon">📄</span>
                    <div>
                      <div className="sub-file-name">{file.name}</div>
                      <div className="sub-file-size">{formatFileSize(file.size)}</div>
                    </div>
                    <button type="button" className="sub-file-remove"
                      onClick={(e) => { e.stopPropagation(); setFile(null); }}>
                      ✕
                    </button>
                  </div>
                ) : (
                  <div className="sub-file-placeholder">
                    <span style={{ fontSize: 28 }}>📎</span>
                    <div>Click or drag a PDF file here</div>
                    <div className="sub-file-hint">Max 10 MB • PDF only</div>
                  </div>
                )}
              </div>
              {isEditing && mySubmission?.proposalFileName && !file && (
                <p className="form-hint">
                  Current file: <strong>{mySubmission.proposalFileName}</strong>
                  ({formatFileSize(mySubmission.fileSize)}).
                  Upload a new file to replace it, or leave empty to keep the existing one.
                </p>
              )}
            </div>

            <div className="sub-form-actions">
              <button type="button" className="btn btn-secondary"
                onClick={() => setShowForm(false)}>
                Cancel
              </button>
              <button type="submit" className="btn btn-primary" disabled={saving}>
                {saving ? (
                  <><span className="spinner" /> Saving...</>
                ) : isEditing ? 'Update Submission' : 'Submit Proposal'}
              </button>
            </div>
          </form>
        </div>
      </div>
    );
  }

  // ── SUBMISSION DETAIL VIEW ─────────────────────────────────────────────
  if (selectedPoint) {
    const deadlinePassed = isDeadlinePassed(selectedPoint.deadline);

    return (
      <div className="tab-content">
        <div className="dash-card">
          {/* Header with back button */}
          <div className="sub-detail-header">
            <button className="btn btn-secondary btn-sm" onClick={handleBackToList}>
              ← Back to Submissions
            </button>
          </div>

          {/* Submission point info banner */}
          <div className="sub-detail-banner">
            <div className="sub-detail-banner-left">
              <h2 className="sub-detail-cw-title">{selectedPoint.title}</h2>
              {selectedPoint.description && (
                <p className="sub-detail-cw-desc">{selectedPoint.description}</p>
              )}
              <div className="sub-detail-meta-row">
                <span className={`submission-type-badge ${selectedPoint.isIndividual ? '' : 'group'}`}>
                  {selectedPoint.isIndividual ? 'Individual' : 'Group'}
                </span>
                {selectedPoint.deadline && (
                  <span className={`sub-deadline-chip ${deadlinePassed ? 'passed' : ''}`}>
                    ⏰ Deadline: {formatDateTime(selectedPoint.deadline)}
                  </span>
                )}
                {deadlinePassed && (
                  <span className="sub-status-badge closed">Closed</span>
                )}
              </div>
            </div>
          </div>

          {/* Loading state */}
          {loadingSub && (
            <div className="loading-state">Loading your submission...</div>
          )}

          {/* No submission yet */}
          {!loadingSub && !mySubmission && (
            <div className="sub-empty-state">
              <div className="sub-empty-icon">📝</div>
              <h3>No Submission Yet</h3>
              <p>You haven't submitted a proposal for this coursework.</p>
              {!deadlinePassed ? (
                <button className="btn btn-primary" onClick={openCreateForm}>
                  📤 Create Submission
                </button>
              ) : (
                <p className="sub-closed-msg">
                  The deadline has passed. Submissions are no longer accepted.
                </p>
              )}
            </div>
          )}

          {/* Existing submission */}
          {!loadingSub && mySubmission && (
            <div className="sub-detail-content">
              <div className="sub-detail-card">
                <div className="sub-detail-card-header">
                  <h3 className="sub-detail-title">{mySubmission.title}</h3>
                  <span className={`sub-status-badge ${mySubmission.status?.toLowerCase()}`}>
                    {mySubmission.status}
                  </span>
                </div>

                <div className="sub-detail-section">
                  <div className="sub-detail-label">Abstract</div>
                  <p className="sub-detail-text">{mySubmission.abstract || '—'}</p>
                </div>

                <div className="sub-detail-section">
                  <div className="sub-detail-label">Description</div>
                  <p className="sub-detail-text">{mySubmission.description || '—'}</p>
                </div>

                <div className="sub-detail-grid">
                  <div className="sub-detail-item">
                    <span className="sub-detail-item-label">Research Area</span>
                    <span className="sub-detail-item-value">
                      {mySubmission.researchAreaName || '—'}
                    </span>
                  </div>
                  <div className="sub-detail-item">
                    <span className="sub-detail-item-label">Submitted At</span>
                    <span className="sub-detail-item-value">
                      {formatDateTime(mySubmission.submittedAt)}
                    </span>
                  </div>
                  {mySubmission.updatedAt && (
                    <div className="sub-detail-item">
                      <span className="sub-detail-item-label">Last Updated</span>
                      <span className="sub-detail-item-value">
                        {formatDateTime(mySubmission.updatedAt)}
                      </span>
                    </div>
                  )}
                  <div className="sub-detail-item">
                    <span className="sub-detail-item-label">Proposal File</span>
                    <span className="sub-detail-item-value sub-file-link"
                      onClick={handleViewPdf}
                      style={{ cursor: 'pointer' }}>
                      📄 {mySubmission.proposalFileName || 'View PDF'}
                      <span className="sub-file-size-inline">
                        ({formatFileSize(mySubmission.fileSize)})
                      </span>
                    </span>
                  </div>
                </div>

                {/* Action buttons */}
                <div className="sub-detail-actions">
                  <button className="btn btn-outline btn-sm"
                    onClick={handleViewPdf}
                    disabled={loadingPdf}>
                    {loadingPdf ? 'Loading...' : '📄 View PDF'}
                  </button>

                  {!deadlinePassed && (
                    <>
                      <button className="btn btn-primary btn-sm" onClick={openEditForm}>
                        ✏️ Edit
                      </button>
                      <button className="btn btn-danger btn-sm"
                        onClick={() => setShowDeleteConfirm(true)}>
                        🗑 Delete
                      </button>
                    </>
                  )}
                </div>
              </div>
            </div>
          )}
        </div>

        {/* ── DELETE CONFIRM DIALOG ──────────────────────────────────── */}
        {showDeleteConfirm && (
          <div className="modal-backdrop confirm-backdrop" onClick={() => setShowDeleteConfirm(false)}>
            <div className="confirm-container" onClick={(e) => e.stopPropagation()}>
              <h3>Delete Submission?</h3>
              <p>
                This will permanently delete your submission and the uploaded PDF.
                This action cannot be undone.
              </p>
              <div className="confirm-actions">
                <button className="btn btn-secondary"
                  onClick={() => setShowDeleteConfirm(false)}
                  disabled={deleting}>
                  Cancel
                </button>
                <button className="btn btn-danger" onClick={handleDelete} disabled={deleting}>
                  {deleting ? 'Deleting...' : 'Yes, Delete'}
                </button>
              </div>
            </div>
          </div>
        )}

        {/* ── SNACKBAR ────────────────────────────────────────────────── */}
        <div className={`snackbar ${snack.type} ${snack.show ? 'show' : ''}`}>
          {snack.msg}
        </div>
      </div>
    );
  }

  // ── SUBMISSION POINTS LIST (main view) ─────────────────────────────────
  return (
    <div className="tab-content">
      <div className="dash-card">
        <div className="dash-card-header">
          <div>
            <div className="dash-card-title">📤 Submission Portal</div>
            <div className="dash-card-subtitle">
              View available submission points and manage your proposals
            </div>
          </div>
          <button className="btn btn-secondary" onClick={fetchSubmissionPoints}>
            Refresh
          </button>
        </div>

        {error && <div className="alert alert-error">{error}</div>}

        {loading ? (
          <div className="loading-state">Loading submission points...</div>
        ) : submissionPoints.length === 0 ? (
          <div className="empty-state">
            <p>No submission points available right now.</p>
            <p>Check back when your module leader creates a submission.</p>
          </div>
        ) : (
          <div className="sub-points-grid">
            {submissionPoints.map((point) => {
              const passed = isDeadlinePassed(point.deadline);
              return (
                <div
                  key={point.courseworkId}
                  className={`sub-point-card ${passed ? 'deadline-passed' : ''}`}
                  onClick={() => handleSelectPoint(point)}
                >
                  <div className="sub-point-top">
                    <div className="sub-point-title">{point.title}</div>
                    <span className={`submission-type-badge ${point.isIndividual ? '' : 'group'}`}>
                      {point.isIndividual ? 'Individual' : 'Group'}
                    </span>
                  </div>

                  {point.description && (
                    <p className="sub-point-desc">{point.description}</p>
                  )}

                  <div className="sub-point-footer">
                    {point.deadline && (
                      <span className={`sub-deadline-chip ${passed ? 'passed' : ''}`}>
                        ⏰ {formatDate(point.deadline)}
                      </span>
                    )}
                    {point.hasSubmitted ? (
                      <span className="sub-status-badge submitted">✓ Submitted</span>
                    ) : passed ? (
                      <span className="sub-status-badge closed">Closed</span>
                    ) : (
                      <span className="sub-status-badge open">Open</span>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* ── SNACKBAR ────────────────────────────────────────────────── */}
      <div className={`snackbar ${snack.type} ${snack.show ? 'show' : ''}`}>
        {snack.msg}
      </div>
    </div>
  );
}
