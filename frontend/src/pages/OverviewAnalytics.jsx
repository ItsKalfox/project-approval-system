import { useState, useEffect } from 'react';
import api from '../api';

export default function OverviewAnalytics() {
  const [analytics, setAnalytics] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const token = localStorage.getItem('pas_token');
    const userJson = localStorage.getItem('pas_user');
    if (!token) {
      setError('Please log in to view analytics');
      setLoading(false);
      return;
    }
    if (userJson) {
      try {
        const user = JSON.parse(userJson);
        console.log('Current user role:', user.role);
        if (user.role !== 'ADMIN' && user.role !== 'SYSTEMADMIN') {
          console.warn('User role is not ADMIN, analytics may not load');
        }
      } catch {}
    }

    const fetchAnalytics = async () => {
      try {
        const res = await api.get('/admin/dashboard/summary');
        console.log('API Response:', res.data);
        const data = res.data?.data ?? res.data;
        console.log('Parsed data:', data);
        setAnalytics(data);
      } catch (err) {
        console.error('API Error:', err);
        const errorMessage = err.response?.data?.message || err.response?.data?.error || err.message || 'Unknown error';
        setError(errorMessage);
      } finally {
        setLoading(false);
      }
    };
    fetchAnalytics();
  }, []);

  if (loading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '300px' }}>
        <div style={{ width: 40, height: 40, border: '3px solid #059669', borderRightColor: 'transparent', borderRadius: '50%', animation: 'spin 0.7s linear infinite' }}></div>
        <style>{`@keyframes spin { to { transform: rotate(360deg); } }`}</style>
      </div>
    );
  }

  if (error) {
    return (
      <div style={{ padding: '20px', color: '#dc2626' }}>
        <div>Error loading analytics: {error}</div>
        <div style={{ fontSize: '13px', marginTop: '8px' }}>Check console for details</div>
      </div>
    );
  }

  console.log('Analytics full response:', analytics);
  const sys = analytics?.SystemAnalytics || analytics?.systemAnalytics || {};
  console.log('SystemAnalytics extracted:', sys);
  
  const students = sys?.students ?? 0;
  const supervisors = sys?.supervisors ?? 0;
  const individualProjects = sys?.individualProjectApprovals ?? 0;
  const groupProjects = sys?.groupProjectApprovals ?? 0;
  const totalApprovals = individualProjects + groupProjects;

  console.log('Students:', students, 'Supervisors:', supervisors, 'Individual:', individualProjects, 'Group:', groupProjects);

  const cardStyle = (color) => ({
    borderRadius: '12px',
    padding: '24px',
    boxShadow: '0 2px 8px rgba(0,0,0,0.08)',
    borderLeft: `4px solid ${color}`,
    flex: 1,
    minWidth: '200px',
  });

  return (
    <div style={{ padding: '20px' }}>
      <div style={{ display: 'flex', gap: '16px', flexWrap: 'wrap' }}>
        
        <div style={{ ...cardStyle('#3b82f6'), background: '#eff6ff' }}>
          <div style={{ fontSize: '14px', color: '#6b7280', fontWeight: '600' }}>TOTAL STUDENTS</div>
          <div style={{ fontSize: '36px', fontWeight: '700', margin: '8px 0', color: '#3b82f6' }}>{students}</div>
          <div style={{ fontSize: '13px', color: '#9ca3af' }}>Enrolled in the system</div>
        </div>

        <div style={{ ...cardStyle('#8b5cf6'), background: '#f5f3ff' }}>
          <div style={{ fontSize: '14px', color: '#6b7280', fontWeight: '600' }}>SUPERVISORS</div>
          <div style={{ fontSize: '36px', fontWeight: '700', margin: '8px 0', color: '#8b5cf6' }}>{supervisors}</div>
          <div style={{ fontSize: '13px', color: '#9ca3af' }}>Active faculty supervisors</div>
        </div>

        <div style={{ ...cardStyle('#10b981'), background: '#ecfdf5' }}>
          <div style={{ fontSize: '14px', color: '#6b7280', fontWeight: '600' }}>PROJECT APPROVALS</div>
          <div style={{ fontSize: '36px', fontWeight: '700', margin: '8px 0', color: '#10b981' }}>{totalApprovals}</div>
          <div style={{ fontSize: '13px', color: '#9ca3af' }}>
            {individualProjects} individual, {groupProjects} group
          </div>
        </div>

      </div>
    </div>
  );
}