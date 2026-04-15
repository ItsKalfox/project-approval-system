import { useState, useEffect } from 'react';

const API = 'http://localhost:5000';

export default function AllocationOversightTab() {
  const [allocations, setAllocations] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [filter, setFilter] = useState('all');
  const [showIntervention, setShowIntervention] = useState(false);
  const [interventionMode, setInterventionMode] = useState(false);
  const [matchedStudents, setMatchedStudents] = useState([]);
  const [availableProjects, setAvailableProjects] = useState([]);
  const [availableSupervisors, setAvailableSupervisors] = useState([]);
  const [selectedStudent, setSelectedStudent] = useState('');
  const [selectedProject, setSelectedProject] = useState('');
  const [selectedSupervisor, setSelectedSupervisor] = useState('');
  const [confirmModal, setConfirmModal] = useState({ show: false, studentId: '', studentName: '', projectId: '', projectName: '', supervisorId: '', supervisorName: '' });
  const [interventionLoading, setInterventionLoading] = useState(false);
  const [snackbar, setSnackbar] = useState({ message: '', type: '' });

  useEffect(() => {
    fetchAllocations();
  }, []);

  const fetchAllocations = async () => {
    const token = localStorage.getItem('pas_token');
    if (!token) {
      setError('Not authenticated. Please log in.');
      setLoading(false);
      return;
    }
    try {
      setLoading(true);
      setError('');
      const res = await fetch(`${API}/api/allocations`, {
        headers: { 
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
      });
      const text = await res.text();
      if (!res.ok) {
        if (res.status === 404) {
          setError('Blind match endpoint not implemented yet. Create the allocations controller.');
          setLoading(false);
          return;
        }
        const data = text ? JSON.parse(text) : { message: 'Failed to fetch allocations' };
        throw new Error(data.message || 'Failed to fetch');
      }
      const data = JSON.parse(text);
      setAllocations(data.data || []);
    } catch (err) {
      if (err.message.includes('fetch') || err.message.includes('Failed to')) {
        setError('Blind match endpoint not implemented yet. Create the allocations controller.');
      } else {
        setError(err.message || 'Failed to load allocations');
      }
    } finally {
      setLoading(false);
    }
  };

  const handleStatusChange = async (id, newStatus) => {
    const token = localStorage.getItem('pas_token');
    if (!token) {
      setError('Not authenticated. Please log in.');
      return;
    }
    try {
      const res = await fetch(`${API}/api/allocations/${id}`, {
        method: 'PATCH',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({ status: newStatus }),
      });
      const text = await res.text();
      if (!res.ok) {
        const data = text ? JSON.parse(text) : { message: 'Failed to update status' };
        throw new Error(data.message || 'Failed to update');
      }
      fetchAllocations();
    } catch (err) {
      setError(err.message);
    }
  };

  const filteredAllocations = filter === 'all' 
    ? allocations 
    : allocations.filter(a => a.status === filter);

  const getStatusBadge = (status) => {
    const statusClasses = {
      'matched': 'badge badge-success',
      'pending': 'badge badge-warning',
      'under review': 'badge badge-info',
    };
    return statusClasses[status?.toLowerCase()] || 'badge badge-default';
  };

  const openInterventionTools = async () => {
    setShowIntervention(true);
    setInterventionMode(true);
    setInterventionLoading(true);
    setSnackbar({ message: '', type: '' });
    const token = localStorage.getItem('pas_token');
    if (!token) {
      setError('Not authenticated. Please log in.');
      setInterventionLoading(false);
      return;
    }
    try {
      const [studentsRes, projectsRes, supervisorsRes] = await Promise.all([
        fetch(`${API}/api/allocations/students-with-matches`, { headers: { Authorization: `Bearer ${token}` } }),
        fetch(`${API}/api/projects/available`, { headers: { Authorization: `Bearer ${token}` } }),
        fetch(`${API}/api/supervisors`, { headers: { Authorization: `Bearer ${token}` } })
      ]);
      
      let studentsData = [];
      let projectsData = [];
      let supervisorsData = [];
      
      try {
        const studentsJson = await studentsRes.json();
        studentsData = Array.isArray(studentsJson?.data) ? studentsJson.data : [];
      } catch { studentsData = []; }
      
      try {
        const projectsJson = await projectsRes.json();
        projectsData = Array.isArray(projectsJson?.data) ? projectsJson.data : [];
      } catch { projectsData = []; }
      
      try {
        const supervisorsJson = await supervisorsRes.json();
        const supData = supervisorsJson?.data;
        supervisorsData = Array.isArray(supData) ? supData : (Array.isArray(supData?.Data) ? supData.Data : []);
      } catch { supervisorsData = []; }
      
      setMatchedStudents(studentsData);
      setAvailableProjects(projectsData);
      setAvailableSupervisors(supervisorsData);
    } catch (err) {
      setSnackbar({ message: 'Failed to load intervention data', type: 'error' });
      setMatchedStudents([]);
      setAvailableProjects([]);
      setAvailableSupervisors([]);
    } finally {
      setInterventionLoading(false);
    }
  };

  const handleReassignClick = () => {
    if (!selectedStudent || !selectedProject || !selectedSupervisor) {
      setSnackbar({ message: 'Please select student, project, and supervisor', type: 'error' });
      return;
    }
    const student = matchedStudents.find(s => s.id === selectedStudent);
    const project = availableProjects.find(p => p.id === selectedProject);
    const supervisor = availableSupervisors.find(s => s.id === selectedSupervisor);
    setConfirmModal({
      show: true,
      studentId: selectedStudent,
      studentName: student?.name || student?.studentName || 'Unknown',
      projectId: selectedProject,
      projectName: project?.name || 'Unknown',
      supervisorId: selectedSupervisor,
      supervisorName: supervisor?.name || 'Unknown'
    });
  };

  const confirmReassign = async () => {
    const token = localStorage.getItem('pas_token');
    if (!token) {
      setSnackbar({ message: 'Not authenticated', type: 'error' });
      return;
    }
    try {
      setInterventionLoading(true);
      const res = await fetch(`${API}/api/allocations/reassign`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify({
          studentId: confirmModal.studentId,
          projectId: confirmModal.projectId,
          supervisorId: confirmModal.supervisorId
        })
      });
      if (!res.ok) {
        const data = await res.json().catch(() => ({ message: 'Failed to reassign' }));
        throw new Error(data.message || 'Failed to reassign');
      }
      setSnackbar({ message: 'Project reassigned successfully', type: 'success' });
      setConfirmModal({ show: false, studentId: '', studentName: '', projectId: '', projectName: '', supervisorId: '', supervisorName: '' });
      setSelectedStudent('');
      setSelectedProject('');
      setSelectedSupervisor('');
      fetchAllocations();
    } catch (err) {
      setSnackbar({ message: err.message, type: 'error' });
    } finally {
      setInterventionLoading(false);
    }
  };

  return (
    <div className="tab-content">
      <div className="dash-card">
        <div className="dash-card-header">
          <div>
            <div className="dash-card-title">Allocation Oversight</div>
            <div className="dash-card-subtitle">
              Monitor and manage supervisor-student project allocations
            </div>
          </div>
          <div style={{ display: 'flex', gap: 8 }}>
            <select 
              className="form-input" 
              style={{ width: 'auto' }}
              value={filter}
              onChange={(e) => setFilter(e.target.value)}
            >
              <option value="all">All Status</option>
              <option value="matched">Matched</option>
              <option value="pending">Pending</option>
              <option value="under review">Under Review</option>
            </select>
            <button className="btn btn-secondary" onClick={fetchAllocations}>
              Refresh
            </button>
            <button className="btn btn-primary" onClick={openInterventionTools}>
              Intervention Tools
            </button>
          </div>
        </div>

        {snackbar.message && (
          <div className={`alert alert-${snackbar.type}`} style={{ marginBottom: 16 }}>
            {snackbar.message}
          </div>
        )}

        {showIntervention && (
          <div className="dash-card" style={{ marginBottom: 24, border: '2px solid #2563eb' }}>
            <div className="dash-card-header">
              <div>
                <div className="dash-card-title">Allocation Intervention Tools</div>
                <div className="dash-card-subtitle">
                  Manually intervene or reassign projects if necessary
                </div>
              </div>
              <button className="btn btn-ghost" onClick={() => setShowIntervention(false)}>
                Close
              </button>
            </div>
            
            {interventionLoading ? (
              <div className="loading-state">Loading...</div>
            ) : (
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr auto', gap: 16, alignItems: 'end', padding: '16px 0' }}>
                <div>
                  <label className="form-label">Select Student</label>
                  <select
                    className="form-input"
                    value={selectedStudent}
                    onChange={(e) => setSelectedStudent(e.target.value)}
                  >
                    <option value="">-- Select Student --</option>
                    {matchedStudents.map(s => (
                      <option key={s.id} value={s.id}>{s.name || s.studentName}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="form-label">Select New Project</label>
                  <select
                    className="form-input"
                    value={selectedProject}
                    onChange={(e) => setSelectedProject(e.target.value)}
                  >
                    <option value="">-- Select Project --</option>
                    {availableProjects.map(p => (
                      <option key={p.id} value={p.id}>{p.name}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="form-label">Select New Supervisor</label>
                  <select
                    className="form-input"
                    value={selectedSupervisor}
                    onChange={(e) => setSelectedSupervisor(e.target.value)}
                  >
                    <option value="">-- Select Supervisor --</option>
                    {availableSupervisors.map(s => (
                      <option key={s.id} value={s.id}>{s.name}</option>
                    ))}
                  </select>
                </div>
                <button
                  className="btn btn-warning"
                  onClick={handleReassignClick}
                  disabled={!selectedStudent || !selectedProject || !selectedSupervisor}
                >
                  ReAssign
                </button>
              </div>
            )}
          </div>
        )}

        {error && <div className="alert alert-error">{error}</div>}

        {loading ? (
          <div className="loading-state">Loading allocations...</div>
        ) : filteredAllocations.length === 0 ? (
          <div className="empty-state">
            <p>No allocations found.</p>
            <p>Blind matching hasn't been done yet or no allocations match the filter.</p>
          </div>
        ) : (
          <div className="table-container">
            <table className="table">
              <thead>
                <tr>
                  <th>Supervisor</th>
                  <th>Student</th>
                  <th>Project</th>
                  <th>Match Date</th>
                  <th>Status</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {filteredAllocations.map((alloc) => (
                  <tr key={alloc.id}>
                    <td>{alloc.supervisorName || alloc.supervisor?.name || '-'}</td>
                    <td>{alloc.studentName || alloc.student?.name || '-'}</td>
                    <td>{alloc.projectName || alloc.project?.name || '-'}</td>
                    <td>{alloc.matchDate ? new Date(alloc.matchDate).toLocaleDateString() : '-'}</td>
                    <td>
                      <span className={getStatusBadge(alloc.status)}>
                        {alloc.status || 'pending'}
                      </span>
                    </td>
                    <td>
                      <div style={{ display: 'flex', gap: 4 }}>
                        <button
                          className="btn btn-sm btn-ghost"
                          title="Mark as Under Review"
                          onClick={() => handleStatusChange(alloc.id, 'under review')}
                          disabled={alloc.status === 'under review'}
                        >
                          Review
                        </button>
                        <button
                          className="btn btn-sm btn-primary"
                          title="Approve Match"
                          onClick={() => handleStatusChange(alloc.id, 'matched')}
                          disabled={alloc.status === 'matched'}
                        >
                          Approve
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {confirmModal.show && (
        <div className="modal-overlay">
          <div className="modal">
            <div className="modal-header">
              <div className="modal-title">Confirm Reassignment</div>
            </div>
            <div className="modal-body">
              <p>Are you sure you want to reassign this project?</p>
              <div style={{ marginTop: 16, padding: 16, background: '#f3f4f6', borderRadius: 8 }}>
                <p><strong>Student:</strong> {confirmModal.studentName}</p>
                <p><strong>New Project:</strong> {confirmModal.projectName}</p>
                <p><strong>New Supervisor:</strong> {confirmModal.supervisorName}</p>
              </div>
              <p style={{ marginTop: 16, color: '#dc2626', fontWeight: 600 }}>
                This action will override the previous allocation.
              </p>
            </div>
            <div className="modal-footer">
              <button 
                className="btn btn-ghost" 
                onClick={() => setConfirmModal({ ...confirmModal, show: false })}
                disabled={interventionLoading}
              >
                Cancel
              </button>
              <button 
                className="btn btn-success" 
                onClick={confirmReassign}
                disabled={interventionLoading}
              >
                {interventionLoading ? 'Processing...' : 'Confirm ReAssign'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}