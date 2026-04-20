import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import LoginPage from '../pages/LoginPage'

// Import api for mocking
import api from '../api'

// Mock the api module
vi.mock('../api', () => ({
  __esModule: true,
  default: {
    post: vi.fn(),
  }
}))

// Mock useNavigate from react-router-dom
const mockNavigate = vi.fn()
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  }
})

const renderWithRouter = (component) => {
  return render(<MemoryRouter>{component}</MemoryRouter>)
}

describe('LoginPage', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.restoreAllMocks()
    mockNavigate.mockClear()
  })

  const mockUser = {
    userId: 1,
    name: 'John Doe',
    email: 'john@university.ac.uk',
    role: 'STUDENT',
    batch: '2024'
  }

  describe('Component Rendering', () => {
    it('should render the login page with correct title', () => {
      renderWithRouter(<LoginPage />)
      // Text is split by <br />, so check both parts exist
      expect(screen.getByText('PROJECT APPROVAL')).toBeInTheDocument()
      expect(screen.getByText('SYSTEM')).toBeInTheDocument()
      expect(screen.getByText('SIGN IN')).toBeInTheDocument()
    })

    it('should render email and password input fields', () => {
      renderWithRouter(<LoginPage />)
      expect(screen.getByLabelText(/email address/i)).toBeInTheDocument()
      expect(screen.getByLabelText(/password/i)).toBeInTheDocument()
    })

    it('should render the login button', () => {
      renderWithRouter(<LoginPage />)
      expect(screen.getByRole('button', { name: /login/i })).toBeInTheDocument()
    })

    it('should render forgot password link', () => {
      renderWithRouter(<LoginPage />)
      expect(screen.getByText(/forgot password/i)).toBeInTheDocument()
    })

    it('should not show error message initially', () => {
      renderWithRouter(<LoginPage />)
      expect(screen.queryByRole('alert')).not.toBeInTheDocument()
    })
  })

  describe('Form Validation', () => {
    it('should show error when email is empty', async () => {
      renderWithRouter(<LoginPage />)
      const passwordInput = screen.getByLabelText(/password/i)
      fireEvent.change(passwordInput, { target: { value: 'password123' } })

      const loginButton = screen.getByRole('button', { name: /login/i })
      fireEvent.click(loginButton)

      expect(screen.getByText(/please enter your email and password/i)).toBeInTheDocument()
    })

    it('should show error when password is empty', async () => {
      renderWithRouter(<LoginPage />)
      const emailInput = screen.getByLabelText(/email address/i)
      fireEvent.change(emailInput, { target: { value: 'test@university.ac.uk' } })

      const loginButton = screen.getByRole('button', { name: /login/i })
      fireEvent.click(loginButton)

      expect(screen.getByText(/please enter your email and password/i)).toBeInTheDocument()
    })

    it('should show error when both fields are empty', async () => {
      renderWithRouter(<LoginPage />)
      const loginButton = screen.getByRole('button', { name: /login/i })
      fireEvent.click(loginButton)

      expect(screen.getByText(/please enter your email and password/i)).toBeInTheDocument()
    })

    it('should keep error message until next submit attempt', async () => {
      renderWithRouter(<LoginPage />)
      const loginButton = screen.getByRole('button', { name: /login/i })
      fireEvent.click(loginButton)

      expect(screen.getByText(/please enter your email and password/i)).toBeInTheDocument()

      const emailInput = screen.getByLabelText(/email address/i)
      fireEvent.change(emailInput, { target: { value: 'test@university.ac.uk' } })

      // Error should still be there (component only clears on next submit)
      expect(screen.getByText(/please enter your email and password/i)).toBeInTheDocument()

      // Submitting again with valid email but no password still shows error
      fireEvent.click(loginButton)
      expect(screen.getByText(/please enter your email and password/i)).toBeInTheDocument()
    })
  })

  describe('Login Flow - Success Cases', () => {
    it('should successfully login a student and redirect', async () => {
      api.post.mockResolvedValueOnce({
        data: {
          data: {
            token: 'jwt-token-supervisor',
            userId: 2,
            name: 'Dr. Smith',
            email: 'smith@university.ac.uk',
            role: 'SUPERVISOR'
          }
        }
      })

      renderWithRouter(<LoginPage />)

      fireEvent.change(screen.getByLabelText(/email address/i), { target: { value: 'smith@university.ac.uk' } })
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'password123' } })
      fireEvent.click(screen.getByRole('button', { name: /login/i }))

      await waitFor(() => {
        expect(mockNavigate).toHaveBeenCalledWith('/dashboard/supervisor')
      })
    })

    it('should successfully login a module leader and redirect', async () => {
      api.post.mockResolvedValueOnce({
        data: {
          data: {
            token: 'jwt-token-moduleleader',
            userId: 3,
            name: 'Prof. Johnson',
            email: 'johnson@university.ac.uk',
            role: 'MODULE_LEADER'
          }
        }
      })

      renderWithRouter(<LoginPage />)

      fireEvent.change(screen.getByLabelText(/email address/i), { target: { value: 'johnson@university.ac.uk' } })
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'password123' } })
      fireEvent.click(screen.getByRole('button', { name: /login/i }))

      await waitFor(() => {
        expect(mockNavigate).toHaveBeenCalledWith('/dashboard/module-leader')
      })
    })

    it('should successfully login an admin and redirect', async () => {
      api.post.mockResolvedValueOnce({
        data: {
          data: {
            token: 'jwt-token-admin',
            userId: 4,
            name: 'Admin User',
            email: 'admin@university.ac.uk',
            role: 'SYSTEM_ADMIN'
          }
        }
      })

      renderWithRouter(<LoginPage />)

      fireEvent.change(screen.getByLabelText(/email address/i), { target: { value: 'admin@university.ac.uk' } })
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'password123' } })
      fireEvent.click(screen.getByRole('button', { name: /login/i }))

      await waitFor(() => {
        expect(mockNavigate).toHaveBeenCalledWith('/dashboard/system-admin')
      })
    })

    it('should handle PascalCase API payload (Token, UserId, etc.)', async () => {
      api.post.mockResolvedValueOnce({
        data: {
          data: {
            Token: 'jwt-token-pascal',
            UserId: 5,
            Name: 'Test User',
            Email: 'test@test.com',
            Role: 'STUDENT',
            Batch: '2023'
          }
        }
      })

      renderWithRouter(<LoginPage />)

      fireEvent.change(screen.getByLabelText(/email address/i), { target: { value: 'test@test.com' } })
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'password123' } })
      fireEvent.click(screen.getByRole('button', { name: /login/i }))

      await waitFor(() => {
        expect(localStorage.getItem('pas_token')).toBe('jwt-token-pascal')
        expect(localStorage.getItem('pas_user')).toBe(JSON.stringify({
          userId: 5,
          name: 'Test User',
          email: 'test@test.com',
          role: 'STUDENT',
          batch: '2023'
        }))
      })
    })

    it('should normalize SYSTEMADMIN role to ADMIN', async () => {
      api.post.mockResolvedValueOnce({
        data: {
          data: {
            token: 'jwt-token',
            userId: 6,
            name: 'SysAdmin',
            email: 'sysadmin@test.com',
            role: 'SYSTEMADMIN'
          }
        }
      })

      renderWithRouter(<LoginPage />)

      fireEvent.change(screen.getByLabelText(/email address/i), { target: { value: 'sysadmin@test.com' } })
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'password123' } })
      fireEvent.click(screen.getByRole('button', { name: /login/i }))

      await waitFor(() => {
        const user = JSON.parse(localStorage.getItem('pas_user'))
        expect(user.role).toBe('ADMIN')
      })
    })

    it('should normalize MODULELEADER role to MODULE LEADER', async () => {
      api.post.mockResolvedValueOnce({
        data: {
          data: {
            token: 'jwt-token',
            userId: 7,
            name: 'Module Leader',
            email: 'ml@test.com',
            role: 'MODULELEADER'
          }
        }
      })

      renderWithRouter(<LoginPage />)

      fireEvent.change(screen.getByLabelText(/email address/i), { target: { value: 'ml@test.com' } })
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'password123' } })
      fireEvent.click(screen.getByRole('button', { name: /login/i }))

      await waitFor(() => {
        const user = JSON.parse(localStorage.getItem('pas_user'))
        expect(user.role).toBe('MODULE LEADER')
      })
    })

    it('should show loading state during login', async () => {
      api.post.mockImplementation(
        () => new Promise(resolve => setTimeout(() => resolve({
          data: { data: { token: 'jwt', role: 'STUDENT' } }
        }), 100))
      )

      renderWithRouter(<LoginPage />)

      fireEvent.change(screen.getByLabelText(/email address/i), { target: { value: 'test@test.com' } })
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'password123' } })
      fireEvent.click(screen.getByRole('button', { name: /login/i }))

      expect(screen.getByText(/signing in/i)).toBeInTheDocument()
    })
  })

  describe('Login Flow - Error Cases', () => {
    it('should handle wrong credentials error', async () => {
      api.post.mockRejectedValueOnce({
        response: {
          data: { message: 'Invalid credentials' }
        }
      })

      renderWithRouter(<LoginPage />)

      fireEvent.change(screen.getByLabelText(/email address/i), { target: { value: 'wrong@test.com' } })
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'wrongpass' } })
      fireEvent.click(screen.getByRole('button', { name: /login/i }))

      await waitFor(() => {
        expect(screen.getByText('Invalid credentials')).toBeInTheDocument()
      })
    })

    it('should handle network error', async () => {
      api.post.mockRejectedValueOnce(new Error('Network error'))

      renderWithRouter(<LoginPage />)

      fireEvent.change(screen.getByLabelText(/email address/i), { target: { value: 'test@test.com' } })
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'password123' } })
      fireEvent.click(screen.getByRole('button', { name: /login/i }))

      await waitFor(() => {
        expect(screen.getByText('Login failed. Please try again.')).toBeInTheDocument()
      })
    })

    it('should handle missing token in response', async () => {
      api.post.mockResolvedValueOnce({
        data: {
          data: {
            userId: 1,
            name: 'Test',
            email: 'test@test.com',
            role: 'STUDENT'
          }
        }
      })

      renderWithRouter(<LoginPage />)

      fireEvent.change(screen.getByLabelText(/email address/i), { target: { value: 'test@test.com' } })
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'password123' } })
      fireEvent.click(screen.getByRole('button', { name: /login/i }))

      await waitFor(() => {
        expect(screen.getByText(/login response is missing required session fields/i)).toBeInTheDocument()
      })
    })

    it('should handle missing role in response', async () => {
      api.post.mockResolvedValueOnce({
        data: {
          data: {
            token: 'jwt-token',
            userId: 1,
            name: 'Test',
            email: 'test@test.com'
          }
        }
      })

      renderWithRouter(<LoginPage />)

      fireEvent.change(screen.getByLabelText(/email address/i), { target: { value: 'test@test.com' } })
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'password123' } })
      fireEvent.click(screen.getByRole('button', { name: /login/i }))

      await waitFor(() => {
        expect(screen.getByText(/login response is missing required session fields/i)).toBeInTheDocument()
      })
    })

    it('should handle unrecognized role', async () => {
      api.post.mockResolvedValueOnce({
        data: {
          data: {
            token: 'jwt-token',
            userId: 1,
            name: 'Test',
            email: 'test@test.com',
            role: 'UNKNOWN_ROLE'
          }
        }
      })

      renderWithRouter(<LoginPage />)

      fireEvent.change(screen.getByLabelText(/email address/i), { target: { value: 'test@test.com' } })
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'password123' } })
      fireEvent.click(screen.getByRole('button', { name: /login/i }))

      await waitFor(() => {
        expect(screen.getByText(/your account role 'UNKNOWN_ROLE' is not recognized/i)).toBeInTheDocument()
        expect(localStorage.getItem('pas_token')).toBeNull()
        expect(localStorage.getItem('pas_user')).toBeNull()
      })
    })

    it('should handle API server error', async () => {
      api
      api.post.mockRejectedValueOnce({
        response: {
          status: 500,
          data: { error: 'Server error' }
        }
      })

      renderWithRouter(<LoginPage />)

      fireEvent.change(screen.getByLabelText(/email address/i), { target: { value: 'test@test.com' } })
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'password123' } })
      fireEvent.click(screen.getByRole('button', { name: /login/i }))

      await waitFor(() => {
        expect(screen.getByText('Server error')).toBeInTheDocument()
      })
    })
  })

  describe('Login Page Helper Function', () => {
    it('should normalize role with underscores', async () => {
      // The normalizeRole function is internal - we test via behavior
      api.post.mockResolvedValueOnce({
        data: {
          data: {
            token: 'jwt-token',
            userId: 1,
            name: 'Test',
            email: 'test@test.com',
            role: 'SYSTEM_ADMIN'
          }
        }
      })

      renderWithRouter(<LoginPage />)

      fireEvent.change(screen.getByLabelText(/email address/i), { target: { value: 'test@test.com' } })
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'password123' } })
      fireEvent.click(screen.getByRole('button', { name: /login/i }))

      await waitFor(() => {
        expect(mockNavigate).toHaveBeenCalledWith('/dashboard/system-admin')
      })
    })
  })
})
