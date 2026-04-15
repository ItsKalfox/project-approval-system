import { useState, useEffect } from 'react';

export default function ResearchAreaManagementTab() {
  const token = localStorage.getItem('pas_token');
  const [areas, setAreas] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [editingArea, setEditingArea] = useState(null);
  const [formData, setFormData] = useState({ name: '' });
  const [saving, setSaving] = useState(false);

  const API = import.meta.env.VITE_API_URL;

  useEffect(() => {
    fetchAreas();
  }, []);

  const fetchAreas = async () => {
    try {
      setLoading(true);
      setError('');
      const res = await fetch(`${API}/api/research-areas`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.message || 'Failed to fetch');
      setAreas(data.data || []);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.name.trim()) return;
    
    try {
      setSaving(true);
      const method = editingArea ? 'PATCH' : 'POST';
      const url = editingArea
        ? `${API}/api/research-areas/${editingArea.id}`
        : `${API}/api/research-areas`;
      
      const res = await fetch(url, {
        method,
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify(formData),
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.message || 'Failed to save');
      
      setShowModal(false);
      setEditingArea(null);
      setFormData({ name: '' });
      fetchAreas();
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  const handleEdit = (area) => {
    setEditingArea(area);
    setFormData({ name: area.name });
    setShowModal(true);
  };

  const handleDelete = async (id) => {
    if (!confirm('Delete this research area?')) return;
    
    try {
      const res = await fetch(`${API}/api/research-areas/${id}`, {
        method: 'DELETE',
        headers: { Authorization: `Bearer ${token}` },
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.message || 'Failed to delete');
      fetchAreas();
    } catch (err) {
      setError(err.message);
    }
  };

  const openAddModal = () => {
    setEditingArea(null);
    setFormData({ name: '' });
    setShowModal(true);
  };

  return (
    <div className="tab-content">
      <div className="dash-card">
        <div className="dash-card-header">
          <div>
            <div className="dash-card-title">Research Areas</div>
            <div className="dash-card-subtitle">
              Manage categories students use when submitting projects
            </div>
          </div>
          <button className="btn btn-primary" onClick={openAddModal}>
            + Add Area
          </button>
        </div>

        {error && <div className="alert alert-error">{error}</div>}

        {loading ? (
          <div className="loading-state">Loading...</div>
        ) : areas.length === 0 ? (
          <div className="empty-state">
            <p>No research areas yet.</p>
            <p>Add areas to let students categorize their projects.</p>
          </div>
        ) : (
          <div className="area-grid">
            {areas.map((area) => (
              <div key={area.id} className="area-card">
                <div className="area-name">{area.name}</div>
                <div className="area-actions">
                  <button
                    className="btn btn-sm btn-ghost"
                    onClick={() => handleEdit(area)}
                  >
                    Edit
                  </button>
                  <button
                    className="btn btn-sm btn-ghost btn-danger"
                    onClick={() => handleDelete(area.id)}
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
              {editingArea ? 'Edit Research Area' : 'Add Research Area'}
            </div>
            <form onSubmit={handleSubmit}>
              <div className="form-group">
                <label className="form-label">Name</label>
                <input
                  type="text"
                  className="form-input"
                  value={formData.name}
                  onChange={(e) => setFormData({ name: e.target.value })}
                  placeholder="e.g., Artificial Intelligence"
                  autoFocus
                />
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
                  {saving ? 'Saving...' : editingArea ? 'Update' : 'Add'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}