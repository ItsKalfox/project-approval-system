import DashboardShell from './DashboardShell';
import StudentManagementTab from './StudentManagementTab';
import SupervisorManagementTab from './SupervisorManagementTab';
import ResearchAreaManagementTab from './ResearchAreaManagementTab';
import AllocationOversightTab from './AllocationOversightTab';
import SubmissionManagementTab from './SubmissionManagementTab';

const TABS = [
  { id: 'student-mgmt',     label: 'Student Management'        },
  { id: 'supervisor-mgmt',  label: 'Supervisor Management'     },
  { id: 'research-areas',   label: 'Research Area Management'  },
  { id: 'allocation',       label: 'Allocation Oversight'      },
  { id: 'submissions',      label: 'Create Submission'         },
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

          {activeTab === 'allocation' && <AllocationOversightTab />}

          {activeTab === 'submissions' && <SubmissionManagementTab />}
        </>
      )}
    </DashboardShell>
  );
}
