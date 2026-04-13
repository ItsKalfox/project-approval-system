import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import api from '../api';

// Hero image from Figma (same as login page)
const heroImage = "https://www.figma.com/api/mcp/asset/bb9f4434-3775-48a2-96ec-63b6c9c704c4";

/*
 * Multi-step forgot password flow:
 *  Step 1 — Enter email → send OTP
 *  Step 2 — Enter OTP   → verify
 *  Step 3 — New password → reset + auto-redirect to login
 */
export default function ForgotPasswordPage() {
  const navigate = useNavigate();
  const [step, setStep]       = useState(1);   // 1 | 2 | 3 | 'done'
  const [email, setEmail]     = useState('');
  const [otp, setOtp]         = useState('');
  const [password, setPassword]   = useState('');
  const [password2, setPassword2] = useState('');
  const [error, setError]     = useState('');
  const [info, setInfo]       = useState('');
  const [loading, setLoading] = useState(false);
  const [countdown, setCountdown] = useState(null);

  /* ── Step 1: send OTP ──────────────────────────────────────────── */
  const handleSendOtp = async (e) => {
    e.preventDefault();
    setError(''); setInfo('');
    if (!email.trim()) { setError('Email is required.'); return; }
    setLoading(true);
    try {
      await api.post('/auth/forgot-password', { email });
      setInfo('If that email is registered, an OTP has been sent. Check your inbox.');
      setStep(2);
    } catch {
      setError('Failed to send OTP. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  /* ── Step 2: verify OTP ────────────────────────────────────────── */
  const handleVerifyOtp = async (e) => {
    e.preventDefault();
    setError(''); setInfo('');
    if (!otp.trim()) { setError('Please enter the OTP.'); return; }
    setLoading(true);
    try {
      await api.post('/auth/verify-otp', { email, otp });
      setStep(3);
    } catch (err) {
      setError(err.response?.data?.message || 'Invalid or expired OTP.');
    } finally {
      setLoading(false);
    }
  };

  /* ── Step 3: reset password ────────────────────────────────────── */
  const handleResetPassword = async (e) => {
    e.preventDefault();
    setError('');
    if (!password) { setError('New password is required.'); return; }
    if (password !== password2) { setError('Passwords do not match.'); return; }
    if (password.length < 8)    { setError('Password must be at least 8 characters.'); return; }
    setLoading(true);
    try {
      await api.post('/auth/reset-password', { email, otp, newPassword: password });
      setStep('done');
      let secs = 3;
      setCountdown(secs);
      const iv = setInterval(() => {
        secs--;
        if (secs <= 0) { clearInterval(iv); navigate('/login'); }
        else setCountdown(secs);
      }, 1000);
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to reset password. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  /* ── Step labels ─────────────────────────────────────────────────── */
  const steps = [
    { n: 1, label: 'Email' },
    { n: 2, label: 'OTP'   },
    { n: 3, label: 'Reset' },
  ];

  const dotClass = (n) => {
    if (step === 'done' || n < step) return 'done';
    if (n === step) return 'active';
    return 'inactive';
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

        {/* RIGHT SIDE - Forgot Password Form */}
        <div className="auth-split-right">
          <h1 className="auth-title-split">RESET PASSWORD</h1>
          <p className="auth-subtitle-split">Follow the steps to regain access to your account</p>

          {/* Step indicator */}
          {step !== 'done' && (
            <div className="step-indicator">
              {steps.map((s, i) => (
                <>
                  <div key={s.n} className={`step-dot ${dotClass(s.n)}`}>{s.n}</div>
                  {i < steps.length - 1 && (
                    <div key={`line-${i}`} className={`step-line ${step > s.n || step === 'done' ? 'done' : ''}`} />
                  )}
                </>
              ))}
            </div>
          )}

          {error && <div className="alert alert-error">{error}</div>}
          {info  && <div className="alert alert-info">{info}</div>}

          {/* ── Done ── */}
          {step === 'done' && (
            <div className="text-center">
              <div className="success-icon">✅</div>
              <h2 style={{ fontSize: 18, fontWeight: 700, marginBottom: 8 }}>Password Reset!</h2>
              <p style={{ color: 'var(--gray-500)', fontSize: 14, marginBottom: 20 }}>
                Your password has been updated successfully.
              </p>
              <div className="alert alert-success">
                Redirecting to login in {countdown} second{countdown !== 1 ? 's' : ''}…
              </div>
            </div>
          )}

          {/* ── Step 1: Email ── */}
          {step === 1 && (
            <form onSubmit={handleSendOtp} noValidate className="auth-form-split">
              <div className="form-group">
                <label className="form-label-split" htmlFor="fp-email">Your email address</label>
                <input
                  id="fp-email"
                  type="email"
                  className="form-input-split"
                  placeholder="you@university.ac.uk"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                />
              </div>
              <button id="send-otp-btn" type="submit" className="btn-login-split" disabled={loading}>
                {loading ? <><span className="spinner" /> Sending…</> : 'Send OTP'}
              </button>
              <div style={{ textAlign: 'center', marginTop: 20 }}>
                <Link to="/login" className="auth-link-split">← Back to Login</Link>
              </div>
            </form>
          )}

          {/* ── Step 2: OTP ── */}
          {step === 2 && (
            <form onSubmit={handleVerifyOtp} noValidate className="auth-form-split">
              <div className="form-group">
                <label className="form-label-split" htmlFor="fp-otp">Enter the 6-digit OTP</label>
                <input
                  id="fp-otp"
                  type="text"
                  inputMode="numeric"
                  maxLength={6}
                  className="form-input-split"
                  style={{ fontSize: 22, letterSpacing: 8, textAlign: 'center' }}
                  placeholder="000000"
                  value={otp}
                  onChange={(e) => setOtp(e.target.value.replace(/\D/g, ''))}
                />
                <p style={{ fontSize: 12, color: 'var(--gray-400)', marginTop: 6 }}>
                  Check your inbox at <strong>{email}</strong>. Valid for 10 minutes.
                </p>
              </div>
              <button id="verify-otp-btn" type="submit" className="btn-login-split" disabled={loading}>
                {loading ? <><span className="spinner" /> Verifying…</> : 'Verify OTP'}
              </button>
              <div style={{ textAlign: 'center', marginTop: 16 }}>
                <button
                  type="button"
                  className="auth-link-split"
                  style={{ background: 'none', border: 'none', cursor: 'pointer' }}
                  onClick={() => { setStep(1); setOtp(''); setError(''); setInfo(''); }}
                >
                  ← Use a different email
                </button>
              </div>
            </form>
          )}

          {/* ── Step 3: New password ── */}
          {step === 3 && (
            <form onSubmit={handleResetPassword} noValidate className="auth-form-split">
              <div className="form-group">
                <label className="form-label-split" htmlFor="fp-pass">New password</label>
                <input
                  id="fp-pass"
                  type="password"
                  className="form-input-split"
                  placeholder="At least 8 characters"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                />
              </div>
              <div className="form-group">
                <label className="form-label-split" htmlFor="fp-pass2">Confirm new password</label>
                <input
                  id="fp-pass2"
                  type="password"
                  className="form-input-split"
                  placeholder="Repeat your new password"
                  value={password2}
                  onChange={(e) => setPassword2(e.target.value)}
                />
              </div>
              <button id="reset-password-btn" type="submit" className="btn-login-split" disabled={loading}>
                {loading ? <><span className="spinner" /> Saving…</> : 'Reset Password'}
              </button>
            </form>
          )}
        </div>
      </div>
    </div>
  );
}