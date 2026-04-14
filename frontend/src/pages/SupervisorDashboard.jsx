import DashboardShell from './DashboardShell';

const TABS = [
  { id: 'browse',       label: 'Browse'       },
  { id: 'my-expertise', label: 'My Expertise' },
];

export default function SupervisorDashboard() {
  return (
    <DashboardShell
      portalName="SUPERVISOR PORTAL"
      tabs={TABS}
      roleClass="supervisor"
    >
      {({ activeTab }) => (
        <>
          {activeTab === 'browse' && (
            <div className="tab-content">
              <div className="dash-card">
                <div className="dash-card-title">🔍 Browse Students</div>
                <p style={{ color: 'var(--gray-500)', fontSize: 14, lineHeight: 1.7 }}>
                  Browse student project proposals and profiles that have been matched or
                  are awaiting supervisor assignment.
                </p>
              </div>
            </div>
          )}

          {activeTab === 'my-expertise' && (
            <div className="tab-content">
              <div className="dash-card">
                <div className="dash-card-title">🔬 My Expertise</div>
                <p style={{ color: 'var(--gray-500)', fontSize: 14, lineHeight: 1.7 }}>
                  Manage your research areas and areas of expertise so students can find
                  the right supervisor for their project.
                </p>
              </div>
            </div>
          )}
        </>
      )}
    </DashboardShell>
  );
}
