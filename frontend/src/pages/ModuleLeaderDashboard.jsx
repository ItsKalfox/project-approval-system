import DashboardShell from './DashboardShell';
import StudentManagementTab from './StudentManagementTab';
import SupervisorManagementTab from './SupervisorManagementTab';
import ResearchAreaManagementTab from './ResearchAreaManagementTab';

const TABS = [
  { id: 'student-mgmt',     label: 'Student Management'        },
  { id: 'supervisor-mgmt',  label: 'Supervisor Management'     },
  { id: 'research-areas',   label: 'Research Area Management'  },
  { id: 'allocation',       label: 'Allocation Oversight'      },
];

export default function ModuleLeaderDashboard() {
  return (
    <DashboardShell
      portalName="MODULE LEADER PORTAL"
      tabs={TABS}
      roleClass="module-leader"
    >
      {({ activeTab }) => (
        <>
          {activeTab === 'student-mgmt' && (
            <StudentManagementTab />
          )}

          {activeTab === 'supervisor-mgmt' && (
            <SupervisorManagementTab />
          )}

          {activeTab === 'research-areas' && <ResearchAreaManagementTab />}

          {activeTab === 'allocation' && (
            <div className="tab-content">
              <div className="dash-card">
                <div className="dash-card-title">📊 Allocation Oversight</div>
                <p style={{ color: 'var(--gray-500)', fontSize: 14, lineHeight: 1.7 }}>
                  Monitor and manage the allocation of students to supervisors. Review
                  pending matches and approve or adjust assignments.
                </p>
              </div>
            </div>
          )}
        </>
      )}
    </DashboardShell>
  );
}
