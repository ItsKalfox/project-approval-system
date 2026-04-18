import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import api from '../api';

// Hero image from Figma
const heroImage = "https://www.figma.com/api/mcp/asset/bb9f4434-3775-48a2-96ec-63b6c9c704c4";

function normalizeRole(role) {
  const value = String(role ?? '')
    .replace(/_/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
    .toUpperCase();

  if (value === 'ADMIN' || value === 'SYSTEMADMIN' || value === 'SYSTEM ADMIN') {
    return 'ADMIN';
  }

  if (value === 'MODULELEADER' || value === 'MODULE LEADER') {
    return 'MODULE LEADER';
  }

  return value;
}

export default function LoginPage() {
  const navigate = useNavigate();
  const [form, setForm] = useState({ email: '', password: '' });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [userType, setUserType] = useState('STUDENT'); // Individual or Group Lead

  const handleChange = (e) =>
    setForm((f) => ({ ...f, [e.target.name]: e.target.value }));

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    if (!form.email || !form.password) {
      setError('Please enter your email and password.');
      return;
    }

    setLoading(true);
    try {
      const res = await api.post('/auth/login', form);
      const payload = res.data?.data ?? {};

      // Support both camelCase and PascalCase API payloads.
      const token = payload.token ?? payload.Token;
      const userId = payload.userId ?? payload.UserId;
      const name = payload.name ?? payload.Name;
      const email = payload.email ?? payload.Email;
      const role = payload.role ?? payload.Role;
      const batch = payload.batch ?? payload.Batch;

      if (!token || !role) {
        throw new Error('Login response is missing required session fields.');
      }

      const normalizedRole = normalizeRole(role);

      // Persist session
      localStorage.setItem('pas_token', token);
      localStorage.setItem('pas_user', JSON.stringify({ userId, name, email, role: normalizedRole, batch }));

      // Route by role
      if (normalizedRole === 'STUDENT') navigate('/dashboard/student');
      else if (normalizedRole === 'SUPERVISOR') navigate('/dashboard/supervisor');
      else if (normalizedRole === 'MODULE LEADER') navigate('/dashboard/module-leader');
      else if (normalizedRole === 'ADMIN') navigate('/dashboard/system-admin');
      else {
        localStorage.removeItem('pas_token');
        localStorage.removeItem('pas_user');
        setError(`Your account role '${role}' is not recognized for this app.`);
      }
    } catch (err) {
      const msg = err.response?.data?.message || err.message || 'Login failed. Please try again.';
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-page">
      <div className="auth-card auth-card-split">
        
        {/* LEFT SIDE - Hero Section */}
        <div className="auth-split-left">
          <h1 className="auth-hero-title">
            PROJECT APPROVAL
            <br />
            SYSTEM
          </h1>
          <div className="auth-hero-image-container">
            <img 
              src={heroImage} 
              alt="Project Approval System" 
              className="auth-hero-image"
            />
          </div>
        </div>

        {/* RIGHT SIDE - Login Form */}
        <div className="auth-split-right">
          <h1 className="auth-title-split">SIGN IN</h1>

          {error && <div className="alert alert-error">{error}</div>}

          <form onSubmit={handleSubmit} noValidate className="auth-form-split">
            <div className="form-group">
              <label className="form-label-split" htmlFor="email">
                Email Address
              </label>
              <input
                id="email"
                name="email"
                type="email"
                autoComplete="email"
                className="form-input-split"
                value={form.email}
                onChange={handleChange}
              />
            </div>

            <div className="form-group">
              <label className="form-label-split" htmlFor="password">
                Password
              </label>
              <input
                id="password"
                name="password"
                type="password"
                autoComplete="current-password"
                className="form-input-split"
                value={form.password}
                onChange={handleChange}
              />
            </div>

            {/* Forgot Password link */}
            <div className="forgot-link-row-split">
              <Link to="/forgot-password" className="auth-link-split">
                Forgot Password?
              </Link>
            </div>

            <button
              type="submit"
              className="btn-login-split"
              disabled={loading}
            >
              {loading ? (
                <>
                  <span className="spinner" /> Signing in…
                </>
              ) : (
                'LOGIN'
              )}
            </button>

          </form>
        </div>
      </div>
    </div>
  );
}