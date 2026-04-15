import { useState, useEffect } from 'react';
import api from '../api';

export default function SubmissionManagementTab() {
  const [submissions, setSubmissions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [editingSubmission, setEditingSubmission] = useState(null);
  const [formData, setFormData] = useState({
    title: '',
    description: '',
    deadline: '',
    isIndividual: true
  });
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    fetchSubmissions();
  }, []);

  const fetchSubmissions = async () => {
    try {
      setLoading(true);
      setError('');
      const res = await api.get('/courseworks');
      const submissionsArray = Array.isArray(res.data) ? res.data : (res.data.data || []);
      setSubmissions(submissionsArray);
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to fetch submissions');
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.title.trim()) return;
    
    try {
      setSaving(true);
      if (editingSubmission) {
        await api.put(`/courseworks/${editingSubmission.courseworkId}`, formData);
      } else {
        await api.post('/courseworks', formData);
      }
      
      setShowModal(false);
      setEditingSubmission(null);
      setFormData({ title: '', description: '', deadline: '', isIndividual: true });
      fetchSubmissions();
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to save');
    } finally {
      setSaving(false);
    }
  };

  const handleToggleActive = async (submission) => {
    try {
      await api.patch(`/courseworks/${submission.courseworkId}/toggle`);
      fetchSubmissions();
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to toggle status');
    }
  };

  const handleDelete = async (id) => {
    if (!confirm('Delete this submission?')) return;
    
    try {
      await api.delete(`/courseworks/${id}`);
      fetchSubmissions();
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to delete');
    }
  };

  const openAddModal = () => {
    setEditingSubmission(null);
    setFormData({ title: '', description: '', deadline: '', isIndividual: true });
    setShowModal(true);
  };

  const openEditModal = (submission) => {
    setEditingSubmission(submission);
    setFormData({
      title: submission.title,
      description: submission.description || '',
      deadline: submission.deadline ? submission.deadline.split('T')[0] : '',
      isIndividual: submission.isIndividual
    });
    setShowModal(true);
  };

  return (
    <div className="tab-content">
      <div className="dash-card">
        <div className="dash-card-header">
          <div>
            <div className="dash-card-title">Submissions</div>
            <div className="dash-card-subtitle">
              Create and manage student submission tasks (e.g., Final Year Project Proposal)
            </div>
          </div>
          <div style={{ display: 'flex', gap: 8 }}>
            <button className="btn btn-secondary" onClick={fetchSubmissions}>
              Refresh
            </button>
            <button className="btn btn-primary" onClick={openAddModal}>
              + Create Submission
            </button>
          </div>
        </div>

        {error && <div className="alert alert-error">{error}</div>}

        {loading ? (
          <div className="loading-state">Loading...</div>
        ) : submissions.length === 0 ? (
          <div className="empty-state">
            <p>No submissions yet.</p>
            <p>Create a submission task to let students submit their work.</p>
          </div>
        ) : (
          <div className="area-grid">
            {submissions.map((submission) => (
              <div key={submission.courseworkId} className="area-card">
                <div className="area-name">{submission.title}</div>
                <div className="submission-type-badge">
                  {submission.isIndividual ? 'Individual' : 'Group'}
                </div>
                {submission.description && (
                  <div className="area-desc">{submission.description}</div>
                )}
                {submission.deadline && (
                  <div className="area-deadline">
                    Deadline: {new Date(submission.deadline).toLocaleDateString()}
                  </div>
                )}
                <div className="area-actions">
                  <button
                    className={`btn btn-sm ${submission.isActive ? 'btn-warning' : 'btn-success'}`}
                    onClick={() => handleToggleActive(submission)}
                  >
                    {submission.isActive ? 'Deactivate' : 'Activate'}
                  </button>
                  <button
                    className="btn btn-sm btn-ghost"
                    onClick={() => openEditModal(submission)}
                  >
                    Edit
                  </button>
                  <button
                    className="btn btn-sm btn-ghost btn-danger"
                    onClick={() => handleDelete(submission.courseworkId)}
                  >
                    Delete
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {showModal && (
        <div className="modal-overlay" onClick={() => setShowModal(false)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-title">
              {editingSubmission ? 'Edit Submission' : 'Create Submission'}
            </div>
            <form onSubmit={handleSubmit}>
              <div className="form-group">
                <label className="form-label">Title</label>
                <input
                  type="text"
                  className="form-input"
                  value={formData.title}
                  onChange={(e) => setFormData({ ...formData, title: e.target.value })}
                  placeholder="e.g., Final Year Project Proposal"
                  autoFocus
                />
              </div>
              <div className="form-group">
                <label className="form-label">Description (optional)</label>
                <textarea
                  className="form-input"
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  placeholder="Describe the submission requirements..."
                  rows={3}
                />
              </div>
              <div className="form-group">
                <label className="form-label">Deadline (optional)</label>
                <input
                  type="date"
                  className="form-input"
                  value={formData.deadline}
                  onChange={(e) => setFormData({ ...formData, deadline: e.target.value })}
                />
              </div>
              <div className="form-group">
                <label className="form-label">Submission Type</label>
                <div className="radio-group">
                  <label className="radio-label">
                    <input
                      type="radio"
                      name="isIndividual"
                      checked={formData.isIndividual}
                      onChange={() => setFormData({ ...formData, isIndividual: true })}
                    />
                    Individual
                  </label>
                  <label className="radio-label">
                    <input
                      type="radio"
                      name="isIndividual"
                      checked={!formData.isIndividual}
                      onChange={() => setFormData({ ...formData, isIndividual: false })}
                    />
                    Group
                  </label>
                </div>
                {!formData.isIndividual && (
                  <p className="form-hint">
                    Students will be able to form groups for this submission.
                  </p>
                )}
              </div>
              <div className="modal-actions">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => setShowModal(false)}
                >
                  Cancel
                </button>
                <button type="submit" className="btn btn-primary" disabled={saving}>
                  {saving ? 'Saving...' : editingSubmission ? 'Update' : 'Create'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}