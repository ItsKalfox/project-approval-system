import DashboardShell from './DashboardShell';
import StudentManagementTab from './StudentManagementTab';
import ModuleLeaderManagementTab from './ModuleLeaderManagementTab';
import SupervisorManagementTab from './SupervisorManagementTab';
import OverviewAnalytics from './OverviewAnalytics';

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
            <OverviewAnalytics />
          )}

          {activeTab === 'student-mgmt' && (
            <StudentManagementTab />
          )}

          {activeTab === 'module-leader-mgmt' && (
            <ModuleLeaderManagementTab />
          )}

          {activeTab === 'supervisor-mgmt' && (
            <SupervisorManagementTab />
          )}
        </div>
      )}
    </DashboardShell>
  );
}
