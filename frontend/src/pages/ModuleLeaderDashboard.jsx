import DashboardShell from './DashboardShell';

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
            <div className="tab-content">
              <div className="dash-card">
                <div className="dash-card-title">👥 Student Management</div>
                <p style={{ color: 'var(--gray-500)', fontSize: 14, lineHeight: 1.7 }}>
                  Create, view, update, and delete student records. Reset student passwords
                  and manage batch assignments from here.
                </p>
              </div>
            </div>
          )}

          {activeTab === 'supervisor-mgmt' && (
            <div className="tab-content">
              <div className="dash-card">
                <div className="dash-card-title">👨‍🏫 Supervisor Management</div>
                <p style={{ color: 'var(--gray-500)', fontSize: 14, lineHeight: 1.7 }}>
                  Manage supervisor accounts, view their profiles, and oversee their
                  availability and capacity for student supervision.
                </p>
              </div>
            </div>
          )}

          {activeTab === 'research-areas' && (
            <div className="tab-content">
              <div className="dash-card">
                <div className="dash-card-title">🔬 Research Area Management</div>
                <p style={{ color: 'var(--gray-500)', fontSize: 14, lineHeight: 1.7 }}>
                  Define and manage the research areas available for student project selection.
                  Link supervisors to their areas of expertise.
                </p>
              </div>
            </div>
          )}

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
