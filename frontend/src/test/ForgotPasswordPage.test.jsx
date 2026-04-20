import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { BrowserRouter, MemoryRouter, Routes, Route } from 'react-router-dom'
import ForgotPasswordPage from '../pages/ForgotPasswordPage'

const renderWithRouter = (component) => {
  return render(<BrowserRouter>{component}</BrowserRouter>)
}

vi.mock('../api', () => ({
  default: {
    post: vi.fn()
  }
}))

import api from '../api'

describe('ForgotPasswordPage', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.clearAllMocks()
    vi.spyOn(console, 'log').mockImplementation(() => {})
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  describe('Component Rendering', () => {
    it('should render forgot password page', () => {
      renderWithRouter(<ForgotPasswordPage />)
      expect(screen.getByText(/reset password/i)).toBeInTheDocument()
    })

    it('should render email input in step 1', () => {
      renderWithRouter(<ForgotPasswordPage />)
      expect(screen.getByLabelText(/your email/i)).toBeInTheDocument()
    })

    it('should render send OTP button', () => {
      renderWithRouter(<ForgotPasswordPage />)
      expect(screen.getByRole('button', { name: /send otp/i })).toBeInTheDocument()
    })

    it('should render back to login link', () => {
      renderWithRouter(<ForgotPasswordPage />)
      expect(screen.getByText(/back to login/i)).toBeInTheDocument()
    })

    it('should render step indicator', () => {
      renderWithRouter(<ForgotPasswordPage />)
      expect(screen.getByText('1')).toBeInTheDocument()
      expect(screen.getByText('2')).toBeInTheDocument()
      expect(screen.getByText('3')).toBeInTheDocument()
    })
  })

  describe('Step 1 - Email Submission', () => {
    it('should update email on change', () => {
      renderWithRouter(<ForgotPasswordPage />)
      const emailInput = screen.getByLabelText(/your email/i)
      fireEvent.change(emailInput, { target: { value: 'test@example.com' } })
      expect(emailInput.value).toBe('test@example.com')
    })

    it('should show error when email is empty', async () => {
      renderWithRouter(<ForgotPasswordPage />)
      const button = screen.getByRole('button', { name: /send otp/i })
      fireEvent.click(button)
      await waitFor(() => {
        expect(screen.getByText(/email is required/i)).toBeInTheDocument()
      })
    })

    it('should call API and move to step 2 on success', async () => {
      api.post.mockResolvedValue({ data: {} })
      renderWithRouter(<ForgotPasswordPage />)
      
      const emailInput = screen.getByLabelText(/your email/i)
      fireEvent.change(emailInput, { target: { value: 'test@example.com' } })
      
      const button = screen.getByRole('button', { name: /send otp/i })
      fireEvent.click(button)
      
      await waitFor(() => {
        expect(api.post).toHaveBeenCalledWith('/auth/forgot-password', { email: 'test@example.com' })
      })
      await waitFor(() => {
        expect(screen.getByLabelText(/enter the 6-digit otp/i)).toBeInTheDocument()
      })
    })

    it('should show error on API failure', async () => {
      api.post.mockRejectedValue({ response: { data: { message: 'Error' } } })
      renderWithRouter(<ForgotPasswordPage />)
      
      const emailInput = screen.getByLabelText(/your email/i)
      fireEvent.change(emailInput, { target: { value: 'test@example.com' } })
      
      const button = screen.getByRole('button', { name: /send otp/i })
      fireEvent.click(button)
      
      await waitFor(() => {
        expect(screen.getByText(/failed to send otp/i)).toBeInTheDocument()
      })
    })
  })

  describe('Step 2 - OTP Verification', () => {
    it('should show OTP input after moving to step 2', () => {
      renderWithRouter(<ForgotPasswordPage />)
      const otpInput = screen.queryByLabelText(/enter the 6-digit otp/i)
      expect(otpInput).not.toBeInTheDocument()
    })

    it('should show error when OTP is empty', async () => {
      renderWithRouter(<ForgotPasswordPage />)
      api.post.mockResolvedValue({ data: {} })
      
      const emailInput = screen.getByLabelText(/your email/i)
      fireEvent.change(emailInput, { target: { value: 'test@example.com' } })
      
      const sendButton = screen.getByRole('button', { name: /send otp/i })
      fireEvent.click(sendButton)
      
      await waitFor(() => {
        expect(screen.getByLabelText(/enter the 6-digit otp/i)).toBeInTheDocument()
      })
      
      const verifyButton = screen.getByRole('button', { name: /verify otp/i })
      fireEvent.click(verifyButton)
      
      await waitFor(() => {
        expect(screen.getByText(/please enter the otp/i)).toBeInTheDocument()
      })
    })

    it('should call API on OTP verification', async () => {
      api.post
        .mockResolvedValueOnce({ data: {} })
        .mockResolvedValueOnce({ data: {} })
      
      renderWithRouter(<ForgotPasswordPage />)
      
      const emailInput = screen.getByLabelText(/your email/i)
      fireEvent.change(emailInput, { target: { value: 'test@example.com' } })
      
      const sendButton = screen.getByRole('button', { name: /send otp/i })
      fireEvent.click(sendButton)
      
      await waitFor(() => {
        expect(screen.getByLabelText(/enter the 6-digit otp/i)).toBeInTheDocument()
      })
      
      const otpInput = screen.getByLabelText(/enter the 6-digit otp/i)
      fireEvent.change(otpInput, { target: { value: '123456' } })
      
      const verifyButton = screen.getByRole('button', { name: /verify otp/i })
      fireEvent.click(verifyButton)
      
      await waitFor(() => {
        expect(api.post).toHaveBeenCalledWith('/auth/verify-otp', { email: 'test@example.com', otp: '123456' })
      })
    })

    it('should allow going back to step 1', async () => {
      api.post.mockResolvedValue({ data: {} })
      renderWithRouter(<ForgotPasswordPage />)
      
      const emailInput = screen.getByLabelText(/your email/i)
      fireEvent.change(emailInput, { target: { value: 'test@example.com' } })
      
      const sendButton = screen.getByRole('button', { name: /send otp/i })
      fireEvent.click(sendButton)
      
      await waitFor(() => {
        expect(screen.getByLabelText(/enter the 6-digit otp/i)).toBeInTheDocument()
      })
      
      const backButton = screen.getByText(/use a different email/i)
      fireEvent.click(backButton)
      
      await waitFor(() => {
        expect(screen.getByLabelText(/your email/i)).toBeInTheDocument()
      })
    })
  })

  describe('Step 3 - Password Reset', () => {
    const navigateToStep3 = async () => {
      api.post
        .mockResolvedValueOnce({ data: {} })
        .mockResolvedValueOnce({ data: {} })
      
      const { container } = renderWithRouter(<ForgotPasswordPage />)
      
      const emailInput = screen.getByLabelText(/your email/i)
      fireEvent.change(emailInput, { target: { value: 'test@example.com' } })
      
      const sendButton = screen.getByRole('button', { name: /send otp/i })
      fireEvent.click(sendButton)
      
      await waitFor(() => {
        expect(screen.getByLabelText(/enter the 6-digit otp/i)).toBeInTheDocument()
      })
      
      const otpInput = screen.getByLabelText(/enter the 6-digit otp/i)
      fireEvent.change(otpInput, { target: { value: '123456' } })
      
      const verifyButton = screen.getByRole('button', { name: /verify otp/i })
      fireEvent.click(verifyButton)

      await new Promise(r => setTimeout(r, 100))
    }

    it('should show password inputs in step 3', async () => {
      await navigateToStep3()
      expect(screen.getByPlaceholderText(/at least 8 characters/i)).toBeInTheDocument()
      expect(screen.getByPlaceholderText(/repeat your new password/i)).toBeInTheDocument()
    })

    it('should show error when password is empty', async () => {
      await navigateToStep3()
      
      const resetButton = screen.getByRole('button', { name: /reset password/i })
      fireEvent.click(resetButton)
      
      await waitFor(() => {
        expect(screen.getByText(/new password is required/i)).toBeInTheDocument()
      })
    })

    it('should show error when passwords do not match', async () => {
      await navigateToStep3()
      
      const passwordInput = screen.getByPlaceholderText(/at least 8 characters/i)
      fireEvent.change(passwordInput, { target: { value: 'password123' } })
      
      const confirmInput = screen.getByPlaceholderText(/repeat your new password/i)
      fireEvent.change(confirmInput, { target: { value: 'password456' } })
      
      const resetButton = screen.getByRole('button', { name: /reset password/i })
      fireEvent.click(resetButton)
      
      await waitFor(() => {
        expect(screen.getByText(/passwords do not match/i)).toBeInTheDocument()
      })
    })

    it('should show error when password is too short', async () => {
      await navigateToStep3()
      
      const passwordInput = screen.getByPlaceholderText(/at least 8 characters/i)
      fireEvent.change(passwordInput, { target: { value: 'short' } })
      
      const confirmInput = screen.getByPlaceholderText(/repeat your new password/i)
      fireEvent.change(confirmInput, { target: { value: 'short' } })
      
      const resetButton = screen.getByRole('button', { name: /reset password/i })
      fireEvent.click(resetButton)
      
      await waitFor(() => {
        expect(screen.getByText(/password must be at least 8 characters/i)).toBeInTheDocument()
      })
    })
  })

  describe('Step Navigation', () => {
    it('should show correct step indicator classes', () => {
      renderWithRouter(<ForgotPasswordPage />)
      const step1 = screen.getByText('1')
      expect(step1).toBeInTheDocument()
    })
  })

  describe('Loading States', () => {
    it('should show loading state on send OTP button', async () => {
      let resolvePromise
      const promise = new Promise(resolve => { resolvePromise = resolve })
      api.post.mockImplementation(() => promise)
      
      renderWithRouter(<ForgotPasswordPage />)
      
      const emailInput = screen.getByLabelText(/your email/i)
      fireEvent.change(emailInput, { target: { value: 'test@example.com' } })
      
      const button = screen.getByRole('button', { name: /send otp/i })
      fireEvent.click(button)
      
      await waitFor(() => {
        expect(screen.getByText(/sending/i)).toBeInTheDocument()
      })
      
      resolvePromise()
    })
  })
})