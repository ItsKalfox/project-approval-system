import DashboardShell from './DashboardShell';
import StudentSubmissionTab from './StudentSubmissionTab';
import StudentProjectTab from './StudentProjectTab';

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
            <StudentProjectTab />
          )}

          {activeTab === 'submission' && (
            <StudentSubmissionTab />
          )}
        </>
      )}
    </DashboardShell>
  );
}
