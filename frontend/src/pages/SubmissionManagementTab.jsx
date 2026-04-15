import { useState, useEffect } from 'react';

const API = 'http://localhost:5000';

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
    const token = localStorage.getItem('pas_token');
    if (!token) {
      setError('Not authenticated. Please log in.');
      setLoading(false);
      return;
    }
    try {
      setLoading(true);
      setError('');
      const res = await fetch(`${API}/api/courseworks`, {
        headers: { 
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
      });
      const text = await res.text();
      if (!res.ok) {
        const data = text ? JSON.parse(text) : { message: 'Request failed' };
        throw new Error(data.message || 'Failed to fetch');
      }
      const data = JSON.parse(text);
      setSubmissions(data.data || []);
    } catch (err) {
      setError(err.message || 'Failed to fetch submissions');
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.title.trim()) return;
    
    const token = localStorage.getItem('pas_token');
    if (!token) {
      setError('Not authenticated. Please log in.');
      return;
    }
    
    try {
      setSaving(true);
      const method = editingSubmission ? 'PUT' : 'POST';
      const url = editingSubmission
        ? `${API}/api/courseworks/${editingSubmission.courseworkId}`
        : `${API}/api/courseworks`;
      
      const res = await fetch(url, {
        method,
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify(formData),
      });
      const text = await res.text();
      if (!res.ok) {
        const data = text ? JSON.parse(text) : { message: 'Failed to save' };
        throw new Error(data.message || 'Failed to save');
      }
      
      setShowModal(false);
      setEditingSubmission(null);
      setFormData({ title: '', description: '', deadline: '', isIndividual: true });
      fetchSubmissions();
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  const handleToggleActive = async (submission) => {
    const token = localStorage.getItem('pas_token');
    if (!token) {
      setError('Not authenticated. Please log in.');
      return;
    }
    
    try {
      const res = await fetch(`${API}/api/courseworks/${submission.courseworkId}/toggle`, {
        method: 'PATCH',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
      });
      if (res.ok) {
        fetchSubmissions();
      }
    } catch (err) {
      setError(err.message);
    }
  };

  const handleDelete = async (id) => {
    if (!confirm('Delete this submission?')) return;
    
    const token = localStorage.getItem('pas_token');
    if (!token) {
      setError('Not authenticated. Please log in.');
      return;
    }
    
    try {
      const res = await fetch(`${API}/api/courseworks/${id}`, {
        method: 'DELETE',
        headers: { 
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
      });
      const text = await res.text();
      if (!res.ok) {
        const data = text ? JSON.parse(text) : { message: 'Failed to delete' };
        throw new Error(data.message || 'Failed to delete');
      }
      fetchSubmissions();
    } catch (err) {
      setError(err.message);
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
          <button className="btn btn-primary" onClick={openAddModal}>
            + Create Submission
          </button>
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