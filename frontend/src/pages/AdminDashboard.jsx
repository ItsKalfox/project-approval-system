import DashboardShell from './DashboardShell';

const TABS = [
  { id: 'overview',     label: 'Overview' },
  { id: 'student-mgmt', label: 'Student Management' },
  { id: 'module-leader-mgmt', label: 'Module Leader Management' },
  { id: 'supervisor-mgmt',  label: 'Supervisor Management' },
];

export default function AdminDashboard() {
  return (
    <DashboardShell
      portalName="ADMIN PORTAL"
      tabs={TABS}
      roleClass="admin"
    >
      {({ activeTab }) => (
        <div className="tab-placeholder-content" style={{ padding: '2rem', textAlign: 'center', background: '#fff', borderRadius: '8px', minHeight: '300px', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#666' }}>
          {activeTab === 'overview' && (
            <h3>Overview Tab (To be implemented)</h3>
          )}

          {activeTab === 'student-mgmt' && (
            <h3>Student Management Tab (To be implemented)</h3>
          )}

          {activeTab === 'module-leader-mgmt' && (
            <h3>Module Leader Management Tab (To be implemented)</h3>
          )}

          {activeTab === 'supervisor-mgmt' && (
            <h3>Supervisor Management Tab (To be implemented)</h3>
          )}
        </div>
      )}
    </DashboardShell>
  );
}
