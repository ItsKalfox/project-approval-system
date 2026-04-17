import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import LoginPage            from './pages/LoginPage';
import ForgotPasswordPage   from './pages/ForgotPasswordPage';
import StudentDashboard     from './pages/StudentDashboard';
import SupervisorDashboard  from './pages/SupervisorDashboard';
import ModuleLeaderDashboard from './pages/ModuleLeaderDashboard';
import SystemAdminDashboard  from './pages/SystemAdminDashboard';

function normalizeRole(role) {
  const value = String(role ?? '')
    .replace(/_/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
    .toUpperCase();

  if (value === 'ADMIN' || value === 'SYSTEMADMIN' || value === 'SYSTEM ADMIN') {
    return 'SYSTEM ADMIN';
  }

  if (value === 'MODULELEADER' || value === 'MODULE LEADER') {
    return 'MODULE LEADER';
  }

  return value;
}

function getCurrentUser() {
  const rawUser = localStorage.getItem('pas_user');
  if (!rawUser) return null;

  try {
    return JSON.parse(rawUser);
  } catch {
    return null;
  }
}

/* Guard — redirects to login if session is missing or role is blocked */
function ProtectedRoute({ children, allowedRoles }) {
  const token = localStorage.getItem('pas_token');
  const user = getCurrentUser();

  if (!token || !user) {
    return <Navigate to="/login" replace />;
  }

  if (allowedRoles?.length) {
    const normalizedUserRole = normalizeRole(user.role);
    const normalizedAllowedRoles = allowedRoles.map(normalizeRole);
    if (!normalizedAllowedRoles.includes(normalizedUserRole)) {
      return <Navigate to="/login" replace />;
    }
  }

  return children;
}

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Default → login */}
        <Route path="/" element={<Navigate to="/login" replace />} />

        {/* Auth pages */}
        <Route path="/login"           element={<LoginPage />} />
        <Route path="/forgot-password" element={<ForgotPasswordPage />} />

        {/* Protected dashboards */}
        <Route path="/dashboard/student" element={
          <ProtectedRoute allowedRoles={['STUDENT']}><StudentDashboard /></ProtectedRoute>
        } />
        <Route path="/dashboard/supervisor" element={
          <ProtectedRoute allowedRoles={['SUPERVISOR']}><SupervisorDashboard /></ProtectedRoute>
        } />
        <Route path="/dashboard/module-leader" element={
          <ProtectedRoute allowedRoles={['MODULE LEADER']}><ModuleLeaderDashboard /></ProtectedRoute>
        } />
        <Route path="/dashboard/system-admin" element={
          <ProtectedRoute allowedRoles={['SYSTEM ADMIN']}><SystemAdminDashboard /></ProtectedRoute>
        } />

        {/* Catch-all */}
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
