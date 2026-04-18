import { useState, useEffect } from 'react';
import api from '../api';

export default function ModuleLeaderManagementTab() {
  const [moduleLeaders, setModuleLeaders] = useState([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(false);

  const [selectedModuleLeader, setSelectedModuleLeader] = useState(null);
  const [isEditing, setIsEditing] = useState(false);
  const [editData, setEditData] = useState({});

  const [isCreating, setIsCreating] = useState(false);
  const [createData, setCreateData] = useState({ name: '', email: '' });

  const [confirmDialog, setConfirmDialog] = useState(null);
  const [isProcessing, setIsProcessing] = useState(false);
  const [snackbar, setSnackbar] = useState({ show: false, message: '', type: 'success' });

  const fetchModuleLeaders = async (p = page) => {
    setLoading(true);
    try {
      const res = await api.get(`/module-leaders?page=${p}&pageSize=10`);
      setModuleLeaders(res.data.data.data || []);
      setTotalPages(res.data.data.totalPages || 1);
      setPage(res.data.data.page || 1);
    } catch (err) {
      showSnackbar('Failed to fetch module leaders', 'error');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchModuleLeaders(page);
  }, [page]);

  const showSnackbar = (message, type = 'success') => {
    setSnackbar({ show: true, message, type });
    setTimeout(() => {
      setSnackbar({ show: false, message: '', type: 'success' });
    }, 3000);
  };

  const handleView = (ml) => {
    setSelectedModuleLeader(ml);
    setIsEditing(false);
    setEditData({ name: ml.name, email: ml.email });
  };

  const handleCloseModal = () => {
    if (isProcessing) return;
    setSelectedModuleLeader(null);
    setIsEditing(false);
    setEditData({});
    setIsCreating(false);
    setCreateData({ name: '', email: '' });
  };

  const promptConfirm = (type, id) => {
    setConfirmDialog({ type, id });
  };

  const handleConfirmAction = async () => {
    if (!confirmDialog) return;
    setIsProcessing(true);

    try {
      if (confirmDialog.type === 'DELETE') {
        await api.delete(`/module-leaders/${confirmDialog.id}`);
        showSnackbar('Module leader deleted successfully');
        handleCloseModal();
        fetchModuleLeaders();
      } else if (confirmDialog.type === 'DEACTIVATE') {
        await api.post(`/module-leaders/${confirmDialog.id}/deactivate`);
        showSnackbar('Module leader deactivated successfully');
        fetchModuleLeaders();
      } else if (confirmDialog.type === 'REACTIVATE') {
        await api.post(`/module-leaders/${confirmDialog.id}/reactivate`);
        showSnackbar('Module leader reactivated successfully');
        fetchModuleLeaders();
      } else if (confirmDialog.type === 'SAVE') {
        const res = await api.patch(`/module-leaders/${confirmDialog.id}`, editData);
        showSnackbar(res.data.message || 'Module leader updated successfully');
        setSelectedModuleLeader({ ...selectedModuleLeader, ...editData });
        setIsEditing(false);
        fetchModuleLeaders();
      } else if (confirmDialog.type === 'RESET') {
        const res = await api.post(`/module-leaders/${confirmDialog.id}/reset-password`);
        showSnackbar(res.data.message || 'Password reset link sent');
      } else if (confirmDialog.type === 'CREATE') {
        const createDto = {
          name: createData.name,
          email: createData.email,
          role: 'MODULE LEADER'
        };
        const res = await api.post('/admin/users', createDto);
        showSnackbar(res.data.message || 'Module leader created successfully');
        handleCloseModal();
        fetchModuleLeaders();
      }
    } catch (err) {
      showSnackbar(err.response?.data?.message || 'An error occurred', 'error');
    } finally {
      setIsProcessing(false);
      setConfirmDialog(null);
    }
  };

  return (
    <div className="tab-content">
      <div className="dash-card">
        <div className="dash-card-title">Module Leader Management</div>

        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '16px' }}>
          <p style={{ color: 'var(--gray-500)', fontSize: 14, lineHeight: 1.7 }}>
            Create and manage module leader accounts.
          </p>
          <button className="btn btn-primary" style={{ width: 'auto' }} onClick={() => setIsCreating(true)}>
            + Create Module Leader
          </button>
        </div>

        <div className="table-container">
          {loading ? (
            <div style={{ textAlign: 'center', padding: '40px 0' }}>
              <div className="spinner circle-spinner"></div>
            </div>
          ) : (
            <>
              <table className="data-table">
                <thead>
                  <tr>
                    <th>ID</th>
                    <th>Name</th>
                    <th>Email</th>
                    <th style={{ textAlign: 'right' }}>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {moduleLeaders.map((ml) => (
                    <tr key={ml.userId}>
                      <td>{ml.userId}</td>
                      <td>{ml.name}</td>
                      <td>{ml.email}</td>
                      <td style={{ textAlign: 'right' }}>
                        <button className="btn btn-outline btn-sm" onClick={() => handleView(ml)}>
                          View
                        </button>
                      </td>
                    </tr>
                  ))}
                  {moduleLeaders.length === 0 && (
                    <tr>
                      <td colSpan="4" style={{ textAlign: 'center', padding: '24px', color: 'var(--gray-500)' }}>
                        No module leaders found.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>

              {totalPages > 1 && (
                <div className="pagination">
                  <button 
                    className="btn btn-outline btn-sm" 
                    disabled={page === 1} 
                    onClick={() => setPage(page - 1)}
                  >
                    Previous
                  </button>
                  <span className="page-info">Page {page} of {totalPages}</span>
                  <button 
                    className="btn btn-outline btn-sm" 
                    disabled={page === totalPages} 
                    onClick={() => setPage(page + 1)}
                  >
                    Next
                  </button>
                </div>
              )}
            </>
          )}
        </div>
      </div>

      {isCreating && (
        <div className="modal-backdrop">
          <div className="modal-container slideUp">
            <div className="modal-header">
              <button className="btn btn-primary top-left-action" onClick={() => promptConfirm('CREATE', null)}>
                Save
              </button>
              <h2 className="modal-title">Create New Module Leader</h2>
              <button className="btn-close" onClick={handleCloseModal}>×</button>
            </div>

            <div className="modal-body">
              <div className="form-group">
                <label className="form-label">Name</label>
                <input 
                  className="form-input" 
                  value={createData.name}
                  onChange={(e) => setCreateData({...createData, name: e.target.value})}
                  placeholder="Dr. John Smith"
                />
              </div>
              <div className="form-group">
                <label className="form-label">Email</label>
                <input 
                  className="form-input" 
                  type="email"
                  value={createData.email}
                  onChange={(e) => setCreateData({...createData, email: e.target.value})}
                  placeholder="john.smith@nsbm.ac.lk"
                />
              </div>
            </div>

            <div className="modal-footer" style={{ justifyContent: 'center' }}>
              <p style={{ fontSize: 13, color: 'var(--gray-500)', margin: 0 }}>
                Login credentials will be automatically generated and sent to the module leader's email.
              </p>
            </div>
          </div>
        </div>
      )}

      {selectedModuleLeader && (
        <div className="modal-backdrop">
          <div className="modal-container slideUp">
            <div className="modal-header">
              {isEditing ? (
                <button className="btn btn-primary top-left-action" onClick={() => promptConfirm('SAVE', selectedModuleLeader.userId)}>
                  Save
                </button>
              ) : (
                <button className="btn btn-outline top-left-action" onClick={() => setIsEditing(true)}>
                  Edit
                </button>
              )}
              <h2 className="modal-title">Module Leader Details</h2>
              <button className="btn-close" onClick={handleCloseModal}>×</button>
            </div>

            <div className="modal-body">
              <div className="form-group">
                <label className="form-label">Module Leader ID</label>
                <input className="form-input" disabled value={selectedModuleLeader.userId} />
              </div>
              <div className="form-group">
                <label className="form-label">Name</label>
                <input 
                  className="form-input" 
                  disabled={!isEditing} 
                  value={isEditing ? editData.name : selectedModuleLeader.name}
                  onChange={(e) => setEditData({...editData, name: e.target.value})}
                />
              </div>
              <div className="form-group">
                <label className="form-label">Email</label>
                <input 
                  className="form-input" 
                  disabled={!isEditing} 
                  value={isEditing ? editData.email : selectedModuleLeader.email}
                  onChange={(e) => setEditData({...editData, email: e.target.value})}
                />
              </div>
              {selectedModuleLeader.createdAt && (
                <div className="form-group">
                  <label className="form-label">Created At</label>
                  <input className="form-input" disabled value={new Date(selectedModuleLeader.createdAt).toLocaleDateString()} />
                </div>
              )}
            </div>

            <div className="modal-footer">
              <button className="btn btn-outline" onClick={() => promptConfirm('RESET', selectedModuleLeader.userId)}>
                Reset Password
              </button>
              <button className="btn btn-outline" onClick={() => promptConfirm('DEACTIVATE', selectedModuleLeader.userId)}>
                Deactivate
              </button>
              <button className="btn btn-danger" onClick={() => promptConfirm('DELETE', selectedModuleLeader.userId)}>
                Delete
              </button>
            </div>
          </div>
        </div>
      )}

      {confirmDialog && (
        <div className="modal-backdrop confirm-backdrop" style={{ zIndex: 1000 }}>
          <div className="confirm-container">
            <h3>Confirm Action</h3>
            <p>
              {confirmDialog.type === 'CREATE' && 'Are you sure you want to create this new module leader account?'}
              {confirmDialog.type === 'DELETE' && 'Are you sure you want to delete this module leader? This action cannot be undone.'}
              {confirmDialog.type === 'DEACTIVATE' && 'Are you sure you want to deactivate this module leader?'}
              {confirmDialog.type === 'REACTIVATE' && 'Are you sure you want to reactivate this module leader?'}
              {confirmDialog.type === 'SAVE' && 'Are you sure you want to save these changes?'}
              {confirmDialog.type === 'RESET' && 'Are you sure you want to reset and generate a new password?'}
            </p>
            <div className="confirm-actions">
              <button 
                className="btn btn-outline" 
                onClick={() => setConfirmDialog(null)}
                disabled={isProcessing}
              >
                Cancel
              </button>
              <button 
                className={`btn ${confirmDialog.type === 'DELETE' || confirmDialog.type === 'DEACTIVATE' ? 'btn-danger' : 'btn-primary'} btn-confirm`} 
                onClick={handleConfirmAction}
                disabled={isProcessing}
              >
                {isProcessing ? (
                  <div className="loading-dots">
                    <div></div><div></div><div></div>
                  </div>
                ) : 'Confirm'}
              </button>
            </div>
          </div>
        </div>
      )}

      <div className={`snackbar ${snackbar.show ? 'show' : ''} ${snackbar.type}`}>
        {snackbar.message}
      </div>
    </div>
  );
}