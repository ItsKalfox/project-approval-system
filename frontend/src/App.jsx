import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import LoginPage            from './pages/LoginPage';
import ForgotPasswordPage   from './pages/ForgotPasswordPage';
import StudentDashboard     from './pages/StudentDashboard';
import SupervisorDashboard  from './pages/SupervisorDashboard';
import ModuleLeaderDashboard from './pages/ModuleLeaderDashboard';

/* Guard — redirects to login if no token in storage */
function ProtectedRoute({ children }) {
  const token = localStorage.getItem('pas_token');
  return token ? children : <Navigate to="/login" replace />;
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
          <ProtectedRoute><StudentDashboard /></ProtectedRoute>
        } />
        <Route path="/dashboard/supervisor" element={
          <ProtectedRoute><SupervisorDashboard /></ProtectedRoute>
        } />
        <Route path="/dashboard/module-leader" element={
          <ProtectedRoute><ModuleLeaderDashboard /></ProtectedRoute>
        } />

        {/* Catch-all */}
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
