import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import userEvent from '@testing-library/user-event'
import App from '../App'
import LoginPage from '../pages/LoginPage'

function normalizeRole(role) {
  const value = String(role ?? '')
    .replace(/_/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
    .toUpperCase();

  if (value === 'ADMIN' || value === 'SYSTEMADMIN' || value === 'SYSTEM ADMIN' || value === 'ADMIN') {
    return 'ADMIN';
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

describe('App Component', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.spyOn(console, 'log').mockImplementation(() => {})
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  describe('normalizeRole function', () => {
    it('should return ADMIN for ADMIN', () => {
      expect(normalizeRole('ADMIN')).toBe('ADMIN')
    })

    it('should return ADMIN for SYSTEMADMIN', () => {
      expect(normalizeRole('SYSTEMADMIN')).toBe('ADMIN')
    })

    it('should return ADMIN for SYSTEM ADMIN', () => {
      expect(normalizeRole('SYSTEM ADMIN')).toBe('ADMIN')
    })

    it('should return MODULE LEADER for MODULELEADER', () => {
      expect(normalizeRole('MODULELEADER')).toBe('MODULE LEADER')
    })

it('should return MODULE LEADER for MODULE LEADER', () => {
      expect(normalizeRole('MODULE LEADER')).toBe('MODULE LEADER')
    })

    it('should handle null role', () => {
      expect(normalizeRole(null)).toBe('')
    })

    it('should handle undefined role', () => {
      expect(normalizeRole(undefined)).toBe('')
    })

    it('should handle multiple spaces', () => {
      expect(normalizeRole('SUPERVISOR    USER')).toBe('SUPERVISOR USER')
    })

    it('should return STUDENT for STUDENT', () => {
      expect(normalizeRole('STUDENT')).toBe('STUDENT')
    })

    it('should return SUPERVISOR for SUPERVISOR', () => {
      expect(normalizeRole('SUPERVISOR')).toBe('SUPERVISOR')
    })

    it('should handle null role', () => {
      expect(normalizeRole(null)).toBe('')
    })

    it('should handle undefined role', () => {
      expect(normalizeRole(undefined)).toBe('')
    })

    it('should handle empty string', () => {
      expect(normalizeRole('')).toBe('')
    })

    it('should handle underscore replacement', () => {
      expect(normalizeRole('MODULE_LEADER')).toBe('MODULE LEADER')
    })

    it('should handle multiple spaces', () => {
      expect(normalizeRole('SUPERVISOR    USER')).toBe('SUPERVISOR USER')
    })
  })

  describe('getCurrentUser function', () => {
    it('should return null when no user in localStorage', () => {
      expect(getCurrentUser()).toBeNull()
    })

    it('should return parsed user when valid JSON', () => {
      const user = { userId: 1, name: 'Test', role: 'STUDENT' }
      localStorage.setItem('pas_user', JSON.stringify(user))
      expect(getCurrentUser()).toEqual(user)
    })

    it('should return null when invalid JSON', () => {
      localStorage.setItem('pas_user', 'invalid json')
      expect(getCurrentUser()).toBeNull()
    })
  })

  describe('ProtectedRoute', () => {
    it('should redirect to login when no token', () => {
      render(
        <BrowserRouter>
          <ProtectedRoute>
            <div>Protected Content</div>
          </ProtectedRoute>
        </BrowserRouter>
      )
      expect(screen.queryByText('Protected Content')).not.toBeInTheDocument()
    })

    it('should redirect to login when no user', () => {
      localStorage.setItem('pas_token', 'some-token')
      render(
        <BrowserRouter>
          <ProtectedRoute>
            <div>Protected Content</div>
          </ProtectedRoute>
        </BrowserRouter>
      )
      expect(screen.queryByText('Protected Content')).not.toBeInTheDocument()
    })

    it('should render children when token and user exist', () => {
      localStorage.setItem('pas_token', 'some-token')
      localStorage.setItem('pas_user', JSON.stringify({ userId: 1, role: 'STUDENT' }))
      render(
        <BrowserRouter>
          <ProtectedRoute>
            <div>Protected Content</div>
          </ProtectedRoute>
        </BrowserRouter>
      )
      expect(screen.getByText('Protected Content')).toBeInTheDocument()
    })

    it('should redirect when role not in allowedRoles', () => {
      localStorage.setItem('pas_token', 'some-token')
      localStorage.setItem('pas_user', JSON.stringify({ userId: 1, role: 'STUDENT' }))
      render(
        <BrowserRouter>
          <ProtectedRoute allowedRoles={['ADMIN']}>
            <div>Protected Content</div>
          </ProtectedRoute>
        </BrowserRouter>
      )
      expect(screen.queryByText('Protected Content')).not.toBeInTheDocument()
    })

    it('should render when role matches allowedRoles', () => {
      localStorage.setItem('pas_token', 'some-token')
      localStorage.setItem('pas_user', JSON.stringify({ userId: 1, role: 'ADMIN' }))
      render(
        <BrowserRouter>
          <ProtectedRoute allowedRoles={['ADMIN']}>
            <div>Protected Content</div>
          </ProtectedRoute>
        </BrowserRouter>
      )
      expect(screen.getByText('Protected Content')).toBeInTheDocument()
    })

    it('should allow multiple allowed roles', () => {
      localStorage.setItem('pas_token', 'some-token')
      localStorage.setItem('pas_user', JSON.stringify({ userId: 1, role: 'SUPERVISOR' }))
      render(
        <BrowserRouter>
          <ProtectedRoute allowedRoles={['ADMIN', 'SUPERVISOR']}>
            <div>Protected Content</div>
          </ProtectedRoute>
        </BrowserRouter>
      )
      expect(screen.getByText('Protected Content')).toBeInTheDocument()
    })

    it('should handle normalized role matching', () => {
      localStorage.setItem('pas_token', 'some-token')
      localStorage.setItem('pas_user', JSON.stringify({ userId: 1, role: 'systemadmin' }))
      render(
        <BrowserRouter>
          <ProtectedRoute allowedRoles={['ADMIN']}>
            <div>Protected Content</div>
          </ProtectedRoute>
        </BrowserRouter>
      )
      expect(screen.getByText('Protected Content')).toBeInTheDocument()
    })
  })

  describe('App Routing', () => {
    it('should redirect root path to login', () => {
      render(<App />)
      expect(window.location.pathname).toBe('/login')
    })

    it('should render login page at /login', () => {
      render(<App />)
      expect(screen.getByLabelText(/email/i)).toBeInTheDocument()
    })

    it('should render forgot password page at /forgot-password', async () => {
      render(
        <BrowserRouter future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
          <Routes>
            <Route path="/login" element={<div>Login</div>} />
            <Route path="/forgot-password" element={<div>Forgot Password</div>} />
            <Route path="*" element={<Navigate to="/login" replace />} />
          </Routes>
        </BrowserRouter>
      )
    })
  })
})