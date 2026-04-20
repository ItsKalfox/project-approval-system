import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { BrowserRouter } from 'react-router-dom'
import SupervisorDashboard from '../pages/SupervisorDashboard'

// Import api for mocking
import api from '../api'

// Mock the api module
vi.mock('../api', () => ({
  __esModule: true,
  default: {
    get: vi.fn(),
  }
}))

const mockNavigate = vi.fn()
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  }
})

const renderWithRouter = (component) => {
  return render(<BrowserRouter>{component}</BrowserRouter>)
}

describe('SupervisorDashboard', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.restoreAllMocks()
    vi.spyOn(console, 'error').mockImplementation(() => {})
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  const mockUser = {
    userId: 1,
    name: 'Dr. Smith',
    email: 'smith@university.ac.uk',
    role: 'SUPERVISOR'
  }

  describe('Initial Render & Data Fetching', () => {
    it('should render supervisor dashboard with portal name', async () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      localStorage.setItem('pas_token', 'token')
      api
      api.get.mockResolvedValueOnce({ data: { data: [] } })
      api.get.mockResolvedValueOnce({ data: { data: [] } })

      renderWithRouter(<SupervisorDashboard />)

      expect(screen.getByText('SUPERVISOR PORTAL')).toBeInTheDocument()
    })

    it('should fetch submissions and research areas on mount', async () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      localStorage.setItem('pas_token', 'token')
      api
      api.get.mockResolvedValueOnce({ data: { data: [] } })
      api.get.mockResolvedValueOnce({ data: { data: [] } })

      renderWithRouter(<SupervisorDashboard />)

      await waitFor(() => {
        expect(api.get).toHaveBeenCalledWith('/courseworks/active')
        expect(api.get).toHaveBeenCalledWith('/supervisor/lookup/research-areas')
      })
    })

    it('should load saved expertise from localStorage', async () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('supervisor_expertise_areas', JSON.stringify([1, 2]))
      api
      api.get.mockResolvedValueOnce({ data: { data: [] } })
      api.get.mockResolvedValueOnce({ data: { data: [] } })

      renderWithRouter(<SupervisorDashboard />)

      // Checkboxes should be checked
      const checkboxes = screen.getAllByRole('checkbox')
      expect(checkboxes[0]).toBeChecked()
      expect(checkboxes[1]).toBeChecked()
    })

    it('should handle malformed saved expertise JSON', async () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('supervisor_expertise_areas', 'invalid-json')
      api
      api.get.mockResolvedValueOnce({ data: { data: [] } })
      api.get.mockResolvedValueOnce({ data: { data: [] } })

      renderWithRouter(<SupervisorDashboard />)

      // Should not throw, checkboxes should be unchecked
      const checkboxes = screen.getAllByRole('checkbox')
      checkboxes.forEach(cb => expect(cb).not.toBeChecked())
    })
  })

  describe('Tab Navigation', () => {
    it('should render all three tabs', async () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      localStorage.setItem('pas_token', 'token')
      api
      api.get.mockResolvedValueOnce({ data: { data: [] } })
      api.get.mockResolvedValueOnce({ data: { data: [] } })

      renderWithRouter(<SupervisorDashboard />)

      expect(screen.getByText('Browse')).toBeInTheDocument()
      expect(screen.getByText('Submissions')).toBeInTheDocument()
      expect(screen.getByText('My Expertise')).toBeInTheDocument()
    })

    it('should show Browse tab by default', async () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      localStorage.setItem('pas_token', 'token')
      api
      api.get.mockResolvedValueOnce({ data: { data: [] } })
      api.get.mockResolvedValueOnce({ data: { data: [] } })

      renderWithRouter(<SupervisorDashboard />)

      // The Browse tab content should be visible - it renders a search bar
      expect(screen.getByPlaceholderText(/search/i)).toBeInTheDocument()
    })

    it('should switch to Submissions tab on click', async () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      localStorage.setItem('pas_token', 'token')
      api
      api.get.mockResolvedValueOnce({ data: { data: [] } })
      api.get.mockResolvedValueOnce({ data: { data: [] } })

      renderWithRouter(<SupervisorDashboard />)

      fireEvent.click(screen.getByText('Submissions'))

      await waitFor(() => {
        expect(screen.getByText('Review Submissions')).toBeInTheDocument()
      })
    })

    it('should switch to My Expertise tab on click', async () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      localStorage.setItem('pas_token', 'token')
      api
      api.get.mockResolvedValueOnce({ data: { data: [] } })
      api.get.mockResolvedValueOnce({ data: { data: [] } })

      renderWithRouter(<SupervisorDashboard />)

      fireEvent.click(screen.getByText('My Expertise'))

      await waitFor(() => {
        expect(screen.getByText(/manage your research areas/i)).toBeInTheDocument()
      })
    })
  })

  describe('Browse Tab', () => {
    it('should render search functionality', async () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      localStorage.setItem('pas_token', 'token')
      api
      api.get.mockResolvedValueOnce({ data: { data: [] } })
      api.get.mockResolvedValueOnce({ data: { data: [] } })

      renderWithRouter(<SupervisorDashboard />)

      expect(screen.getByPlaceholderText(/search/i)).toBeInTheDocument()
      expect(screen.getByText('Search all project proposals…')).toBeInTheDocument()
    })

    it('should render filter dropdowns', async () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      localStorage.setItem('pas_token', 'token')
      api
      api.get.mockResolvedValueOnce({ data: { data: [] } })
      api.get.mockResolvedValueOnce({ data: { data: [] } })

      renderWithRouter(<SupervisorDashboard />)

      expect(screen.getByText('Department')).toBeInTheDocument()
    })

    it('should render sort dropdown', async () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      localStorage.setItem('pas_token', 'token')
      api
      api.get.mockResolvedValueOnce({ data: { data: [] } })
      api.get.mockResolvedValueOnce({ data: { data: [] } })

      renderWithRouter(<SupervisorDashboard />)

      expect(screen.getByText('Sort by')).toBeInTheDocument()
    })
  })

  describe('Submissions Tab', () => {
    it('should render submissions tab header', async () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      localStorage.setItem('pas_token', 'token')
      api
      api.get.mockResolvedValueOnce({ data: { data: [] } })
      api.get.mockResolvedValueOnce({ data: { data: [] } })

      renderWithRouter(<SupervisorDashboard />)

      fireEvent.click(screen.getByText('Submissions'))

      await waitFor(() => {
        expect(screen.getByText('Review Submissions')).toBeInTheDocument()
        expect(screen.getByText('Review and evaluate student project proposals assigned to you.')).toBeInTheDocument()
      })
    })

    it('should render quick filters in submissions tab', async () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      localStorage.setItem('pas_token', 'token')
      api
      api.get.mockResolvedValueOnce({ data: { data: [] } })
      api.get.mockResolvedValueOnce({ data: { data: [] } })

      renderWithRouter(<SupervisorDashboard />)

      fireEvent.click(screen.getByText('Submissions'))

      await waitFor(() => {
        expect(screen.getByText('All Submissions')).toBeInTheDocument()
        expect(screen.getByText('Pending Review')).toBeInTheDocument()
        expect(screen.getByText('Requires Revision')).toBeInTheDocument()
        expect(screen.getByText('Approved')).toBeInTheDocument()
        expect(screen.getByText('Rejected')).toBeInTheDocument()
      })
    })
  })

  describe('My Expertise Tab', () => {
    it('should render expertise management content', async () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      localStorage.setItem('pas_token', 'token')
      api
      api.get.mockResolvedValueOnce({ data: { data: [] } })
      api.get.mockResolvedValueOnce({
        data: {
          data: [
            { researchAreaId: 1, id: 1, name: 'Artificial Intelligence' },
            { researchAreaId: 2, id: 2, name: 'Machine Learning' },
            { researchAreaId: 3, id: 3, name: 'Data Science' }
          ]
        }
      })

      renderWithRouter(<SupervisorDashboard />)

      fireEvent.click(screen.getByText('My Expertise'))

      await waitFor(() => {
        expect(screen.getByText('My Expertise')).toBeInTheDocument()
        expect(screen.getByText(/manage your research areas/i)).toBeInTheDocument()
      })
    })

    it('should render research area checkboxes', async () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      localStorage.setItem('pas_token', 'token')
      api
      api.get.mockResolvedValueOnce({ data: { data: [] } })
      api.get.mockResolvedValueOnce({
        data: {
          data: [
            { researchAreaId: 1, name: 'AI' },
            { researchAreaId: 2, name: 'ML' }
          ]
        }
      })

      renderWithRouter(<SupervisorDashboard />)

      fireEvent.click(screen.getByText('My Expertise'))

      await waitFor(() => {
        expect(screen.getByText('AI')).toBeInTheDocument()
        expect(screen.getByText('ML')).toBeInTheDocument()
      })
    })

    it('should limit selection to 2 research areas', async () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      localStorage.setItem('pas_token', 'token')
      api
      api.get.mockResolvedValueOnce({ data: { data: [] } })
      api.get.mockResolvedValueOnce({
        data: {
          data: [
            { researchAreaId: 1, name: 'AI' },
            { researchAreaId: 2, name: 'ML' },
            { researchAreaId: 3, name: 'Data Science' }
          ]
        }
      })

      renderWithRouter(<SupervisorDashboard />)

      fireEvent.click(screen.getByText('My Expertise'))

      await waitFor(() => {
        expect(screen.getByText('AI')).toBeInTheDocument()
      })

      // Select first checkbox
      const checkboxes = screen.getAllByRole('checkbox')
      fireEvent.click(checkboxes[0])
      expect(checkboxes[0]).toBeChecked()

      // Select second checkbox
      fireEvent.click(checkboxes[1])
      expect(checkboxes[1]).toBeChecked()

      // Try to select third checkbox (should be blocked)
      fireEvent.click(checkboxes[2])
      expect(checkboxes[2]).not.toBeChecked()
    })

    it('should show alert when trying to select more than 2 areas', async () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      localStorage.setItem('pas_token', 'token')
      api
      api.get.mockResolvedValueOnce({ data: { data: [] } })
      api.get.mockResolvedValueOnce({
        data: {
          data: [
            { researchAreaId: 1, name: 'AI' },
            { researchAreaId: 2, name: 'ML' },
            { researchAreaId: 3, name: 'DS' }
          ]
        }
      })

      renderWithRouter(<SupervisorDashboard />)

      fireEvent.click(screen.getByText('My Expertise'))

      await waitFor(() => {
        expect(screen.getByText('AI')).toBeInTheDocument()
      })

      const checkboxes = screen.getAllByRole('checkbox')
      fireEvent.click(checkboxes[0])
      fireEvent.click(checkboxes[1])
      fireEvent.click(checkboxes[2])

      expect(global.alert).toHaveBeenCalledWith('You can select at most 2 research areas.')
    })

    it('should allow deselecting a research area', async () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('supervisor_expertise_areas', JSON.stringify([1, 2]))
      api
      api.get.mockResolvedValueOnce({ data: { data: [] } })
      api.get.mockResolvedValueOnce({
        data: {
          data: [
            { researchAreaId: 1, name: 'AI' },
            { researchAreaId: 2, name: 'ML' }
          ]
        }
      })

      renderWithRouter(<SupervisorDashboard />)

      fireEvent.click(screen.getByText('My Expertise'))

      await waitFor(() => {
        expect(screen.getByText('AI')).toBeInTheDocument()
      })

      const checkboxes = screen.getAllByRole('checkbox')
      expect(checkboxes[0]).toBeChecked()
      expect(checkboxes[1]).toBeChecked()

      fireEvent.click(checkboxes[0])

      expect(checkboxes[0]).not.toBeChecked()
      expect(checkboxes[1]).toBeChecked()
    })

    it('should update localStorage when selection changes', async () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      localStorage.setItem('pas_token', 'token')
      api
      api.get.mockResolvedValueOnce({ data: { data: [] } })
      api.get.mockResolvedValueOnce({
        data: {
          data: [
            { researchAreaId: 1, name: 'AI' },
            { researchAreaId: 2, name: 'ML' }
          ]
        }
      })

      renderWithRouter(<SupervisorDashboard />)

      fireEvent.click(screen.getByText('My Expertise'))

      await waitFor(() => {
        expect(screen.getByText('AI')).toBeInTheDocument()
      })

      const checkboxes = screen.getAllByRole('checkbox')
      fireEvent.click(checkboxes[0])

      expect(localStorage.getItem('supervisor_expertise_areas')).toBe('[1]')

      fireEvent.click(checkboxes[1])
      expect(localStorage.getItem('supervisor_expertise_areas')).toBe('[1,2]')
    })

    it('should show selected count', async () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('supervisor_expertise_areas', JSON.stringify([1]))
      api
      api.get.mockResolvedValueOnce({ data: { data: [] } })
      api.get.mockResolvedValueOnce({
        data: {
          data: [
            { researchAreaId: 1, name: 'AI' },
            { researchAreaId: 2, name: 'ML' }
          ]
        }
      })

      renderWithRouter(<SupervisorDashboard />)

      fireEvent.click(screen.getByText('My Expertise'))

      await waitFor(() => {
        expect(screen.getByText('Selected: 1/2 areas')).toBeInTheDocument()
      })
    })

    it('should show empty state when no research areas', async () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      localStorage.setItem('pas_token', 'token')
      api
      api.get.mockResolvedValueOnce({ data: { data: [] } })
      api.get.mockResolvedValueOnce({ data: { data: [] } })

      renderWithRouter(<SupervisorDashboard />)

      fireEvent.click(screen.getByText('My Expertise'))

      await waitFor(() => {
        expect(screen.getByText(/no research areas available/i)).toBeInTheDocument()
      })
    })

    it('should handle research areas fetch error', async () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      localStorage.setItem('pas_token', 'token')
      api
      api.get.mockResolvedValueOnce({ data: { data: [] } })
      api.get.mockRejectedValueOnce(new Error('Failed to fetch'))

      renderWithRouter(<SupervisorDashboard />)

      fireEvent.click(screen.getByText('My Expertise'))

      await waitFor(() => {
        // Error is logged but UI should show empty state gracefully
        expect(screen.getByText(/no research areas available/i)).toBeInTheDocument()
      })
    })
  })

  describe('API Error Handling', () => {
    it('should handle fetch submissions error', async () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      localStorage.setItem('pas_token', 'token')
      api
      api.get.mockRejectedValueOnce(new Error('Network error'))
      api.get.mockResolvedValueOnce({ data: { data: [] } })

      renderWithRouter(<SupervisorDashboard />)

      await waitFor(() => {
        expect(console.error).toHaveBeenCalledWith('Failed to fetch submissions', expect.any(Error))
      })
    })

    it('should handle empty submissions response', async () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      localStorage.setItem('pas_token', 'token')
      api
      api.get.mockResolvedValueOnce({ data: { data: [] } })
      api.get.mockResolvedValueOnce({ data: { data: [] } })

      renderWithRouter(<SupervisorDashboard />)

      await waitFor(() => {
        expect(api.get).toHaveBeenCalledWith('/courseworks/active')
      })
    })

    it('should handle submissions as nested data.data', async () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      localStorage.setItem('pas_token', 'token')
      api
      api.get.mockResolvedValueOnce({ data: { data: [{ id: 1, title: 'Test' }] } })
      api.get.mockResolvedValueOnce({ data: { data: [] } })

      renderWithRouter(<SupervisorDashboard />)

      await waitFor(() => {
        expect(api.get).toHaveBeenCalledWith('/courseworks/active')
      })
    })
  })

  describe('Logout', () => {
    it('should clear localStorage and navigate on logout', async () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('supervisor_expertise_areas', JSON.stringify([1]))
      api
      api.get.mockResolvedValueOnce({ data: { data: [] } })
      api.get.mockResolvedValueOnce({ data: { data: [] } })

      renderWithRouter(<SupervisorDashboard />)

      await waitFor(() => {
        expect(screen.getByText('Sign Out')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Sign Out'))

      expect(localStorage.getItem('pas_token')).toBeNull()
      expect(localStorage.getItem('pas_user')).toBeNull()
      expect(localStorage.getItem('supervisor_expertise_areas')).toBeNull()
    })
  })
})
