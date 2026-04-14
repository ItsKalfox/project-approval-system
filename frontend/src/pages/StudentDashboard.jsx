import DashboardShell from './DashboardShell';

const TABS = [
  { id: 'my-project',  label: 'My Project'  },
  { id: 'submission',  label: 'Submission'   },
];

export default function StudentDashboard() {
  return (
    <DashboardShell
      portalName="STUDENT PORTAL"
      tabs={TABS}
      roleClass="student"
    >
      {({ activeTab }) => (
        <>
          {activeTab === 'my-project' && (
            <div className="tab-content">
              <div className="dash-card">
                <div className="dash-card-title">📁 My Project</div>
                <p style={{ color: 'var(--gray-500)', fontSize: 14, lineHeight: 1.7 }}>
                  Your assigned project details, supervisor information, and group members
                  will appear here once configured.
                </p>
              </div>
            </div>
          )}

          {activeTab === 'submission' && (
            <div className="tab-content">
              <div className="dash-card">
                <div className="dash-card-title">📤 Submission Portal</div>
                <p style={{ color: 'var(--gray-500)', fontSize: 14, lineHeight: 1.7 }}>
                  Upload and manage your project deliverables, reports, and milestone
                  submissions here.
                </p>
              </div>
            </div>
          )}
        </>
      )}
    </DashboardShell>
  );
}
