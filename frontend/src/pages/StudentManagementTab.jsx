import { useState, useEffect } from 'react';
import api from '../api';

export default function StudentManagementTab() {
  const [students, setStudents] = useState([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(false);

  const [selectedStudent, setSelectedStudent] = useState(null);
  const [isEditing, setIsEditing] = useState(false);
  const [editData, setEditData] = useState({});

  const [isCreating, setIsCreating] = useState(false);
  const [createData, setCreateData] = useState({ name: '', email: '', batch: '' });

  // Confirm dialog state
  const [confirmDialog, setConfirmDialog] = useState(null); // { type: 'DELETE'|'SAVE'|'RESET', id }
  const [isProcessing, setIsProcessing] = useState(false);

  // Snackbar state
  const [snackbar, setSnackbar] = useState({ show: false, message: '', type: 'success' });

  const fetchStudents = async (p = page) => {
    setLoading(true);
    try {
      const res = await api.get(`/students?page=${p}&pageSize=10`);
      setStudents(res.data.data.data || []);
      setTotalPages(res.data.data.totalPages || 1);
      setPage(res.data.data.page || 1);
    } catch (err) {
      showSnackbar('Failed to fetch students', 'error');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchStudents(page);
  }, [page]);

  const showSnackbar = (message, type = 'success') => {
    setSnackbar({ show: true, message, type });
    setTimeout(() => {
      setSnackbar({ show: false, message: '', type: 'success' });
    }, 3000);
  };

  const handleView = (student) => {
    setSelectedStudent(student);
    setIsEditing(false);
    setEditData({ name: student.name, email: student.email, batch: student.batch });
  };

  const handleCloseModal = () => {
    if (isProcessing) return;
    setSelectedStudent(null);
    setIsEditing(false);
    setEditData({});
    setIsCreating(false);
    setCreateData({ name: '', email: '', batch: '' });
  };

  const promptConfirm = (type, id) => {
    setConfirmDialog({ type, id });
  };

  const handleConfirmAction = async () => {
    if (!confirmDialog) return;
    setIsProcessing(true);

    try {
      if (confirmDialog.type === 'DELETE') {
        const res = await api.delete(`/students/${confirmDialog.id}`);
        showSnackbar(res.data.message || 'Student deleted successfully');
        handleCloseModal();
        fetchStudents();
      } else if (confirmDialog.type === 'SAVE') {
        const res = await api.patch(`/students/${confirmDialog.id}`, editData);
        showSnackbar(res.data.message || 'Student updated successfully');
        // Update local object
        setSelectedStudent({ ...selectedStudent, ...editData });
        setIsEditing(false);
        fetchStudents();
      } else if (confirmDialog.type === 'RESET') {
        const res = await api.post(`/students/${confirmDialog.id}/reset-password`);
        showSnackbar(res.data.message || 'Password reset link sent');
      } else if (confirmDialog.type === 'CREATE') {
        const res = await api.post('/students', createData);
        showSnackbar(res.data.message || 'Student created successfully');
        handleCloseModal();
        fetchStudents();
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
        <div className="dash-card-title">Student Management</div>

        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '16px' }}>
          <p style={{ color: 'var(--gray-500)', fontSize: 14, lineHeight: 1.7, justifyContent: 'center', display: 'flex', alignItems: 'center' }}>
            View, update, and delete student records. Reset student passwords from here.
          </p>
          <button className="btn btn-primary" style={{ width: 'auto' }} onClick={() => setIsCreating(true)}>
            + Create Student
          </button>
        </div>

        {/* --- STUDENT LIST --- */}
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
                  {students.map((st) => (
                    <tr key={st.userId}>
                      <td>{st.userId}</td>
                      <td>{st.name}</td>
                      <td>{st.email}</td>
                      <td style={{ textAlign: 'right' }}>
                        <button className="btn btn-outline btn-sm" onClick={() => handleView(st)}>
                          View
                        </button>
                      </td>
                    </tr>
                  ))}
                  {students.length === 0 && (
                    <tr>
                      <td colSpan="4" style={{ textAlign: 'center', padding: '24px', color: 'var(--gray-500)' }}>
                        No students found.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>

              {/* PAGINATION */}
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

      {/* --- CREATE STUDENT MODAL --- */}
      {isCreating && (
        <div className="modal-backdrop">
          <div className="modal-container slideUp">
            
            <div className="modal-header">
              <button className="btn btn-primary top-left-action" onClick={() => promptConfirm('CREATE', null)}>
                Save
              </button>
              <h2 className="modal-title">Create New Student</h2>
              <button className="btn-close" onClick={handleCloseModal}>×</button>
            </div>

            <div className="modal-body">
              <div className="form-group">
                <label className="form-label">Name</label>
                <input 
                  className="form-input" 
                  value={createData.name}
                  onChange={(e) => setCreateData({...createData, name: e.target.value})}
                  placeholder="John Doe"
                />
              </div>
              <div className="form-group">
                <label className="form-label">Email</label>
                <input 
                  className="form-input" 
                  type="email"
                  value={createData.email}
                  onChange={(e) => setCreateData({...createData, email: e.target.value})}
                  placeholder="student@example.com"
                />
              </div>
              <div className="form-group">
                <label className="form-label">Batch</label>
                <input 
                  className="form-input" 
                  value={createData.batch}
                  onChange={(e) => setCreateData({...createData, batch: e.target.value})}
                  placeholder="e.g. Y3S1"
                />
              </div>
            </div>

            <div className="modal-footer" style={{ justifyContent: 'center' }}>
              <p style={{ fontSize: 13, color: 'var(--gray-500)', margin: 0 }}>
                Login credentials will be automatically generated and sent to the student's email.
              </p>
            </div>

          </div>
        </div>
      )}

      {/* --- STUDENT DETAILS MODAL --- */}
      {selectedStudent && (
        <div className="modal-backdrop">
          <div className="modal-container slideUp">
            
            {/* Modal Header */}
            <div className="modal-header">
              {isEditing ? (
                <button className="btn btn-primary top-left-action" onClick={() => promptConfirm('SAVE', selectedStudent.userId)}>
                  Save
                </button>
              ) : (
                <button className="btn btn-outline top-left-action" onClick={() => setIsEditing(true)}>
                  Edit
                </button>
              )}
              <h2 className="modal-title">Student Details</h2>
              <button className="btn-close" onClick={handleCloseModal}>×</button>
            </div>

            {/* Modal Body */}
            <div className="modal-body">
              <div className="form-group">
                <label className="form-label">Student ID</label>
                <input className="form-input" disabled value={selectedStudent.userId} />
              </div>
              <div className="form-group">
                <label className="form-label">Name</label>
                <input 
                  className="form-input" 
                  disabled={!isEditing} 
                  value={isEditing ? editData.name : selectedStudent.name}
                  onChange={(e) => setEditData({...editData, name: e.target.value})}
                />
              </div>
              <div className="form-group">
                <label className="form-label">Email</label>
                <input 
                  className="form-input" 
                  disabled={!isEditing} 
                  value={isEditing ? editData.email : selectedStudent.email}
                  onChange={(e) => setEditData({...editData, email: e.target.value})}
                />
              </div>
              <div className="form-group">
                <label className="form-label">Batch</label>
                <input 
                  className="form-input" 
                  disabled={!isEditing} 
                  value={isEditing ? editData.batch : selectedStudent.batch || ''}
                  onChange={(e) => setEditData({...editData, batch: e.target.value})}
                />
              </div>
            </div>

            {/* Modal Footer */}
            <div className="modal-footer">
              <button className="btn btn-outline" onClick={() => promptConfirm('RESET', selectedStudent.userId)}>
                Reset Password
              </button>
              <button className="btn btn-danger" onClick={() => promptConfirm('DELETE', selectedStudent.userId)}>
                Delete
              </button>
            </div>

          </div>
        </div>
      )}

      {/* --- CONFIRMATION DIALOG --- */}
      {confirmDialog && (
        <div className="modal-backdrop confirm-backdrop" style={{ zIndex: 1000 }}>
          <div className="confirm-container">
            <h3>Confirm Action</h3>
            <p>
              {confirmDialog.type === 'CREATE' && 'Are you sure you want to create this new student record?'}
              {confirmDialog.type === 'DELETE' && 'Are you sure you want to delete this student? This action cannot be undone.'}
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
                className={`btn ${confirmDialog.type === 'DELETE' ? 'btn-danger' : 'btn-primary'} btn-confirm`} 
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

      {/* --- SNACKBAR --- */}
      <div className={`snackbar ${snackbar.show ? 'show' : ''} ${snackbar.type}`}>
        {snackbar.message}
      </div>

    </div>
  );
}
