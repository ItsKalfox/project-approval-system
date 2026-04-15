import { useState, useEffect } from 'react';

const API = 'http://localhost:5000';

export default function AllocationOversightTab() {
  const [allocations, setAllocations] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [filter, setFilter] = useState('all');

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
          </div>
        </div>

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
    </div>
  );
}