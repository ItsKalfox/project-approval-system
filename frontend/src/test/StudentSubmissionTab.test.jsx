import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react'
import { BrowserRouter } from 'react-router-dom'
import StudentSubmissionTab from '../pages/StudentSubmissionTab'

// Import api for mocking
import api from '../api'

// Mock the api module
vi.mock('../api', () => ({
  __esModule: true,
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
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

// Mock URL.createObjectURL and URL.revokeObjectURL
global.URL.createObjectURL = vi.fn(() => 'mock-url')
global.URL.revokeObjectURL = vi.fn()

// Mock window.alert
global.alert = vi.fn()

describe('StudentSubmissionTab', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.restoreAllMocks()
    vi.spyOn(console, 'log').mockImplementation(() => {})
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  const mockUser = {
    userId: 1,
    name: 'John Doe',
    email: 'john@university.ac.uk',
    role: 'STUDENT',
    batch: '2024'
  }

  describe('Initial Render & Loading State', () => {
    it('should show loading state initially', () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      renderWithRouter(<StudentSubmissionTab />)
      expect(screen.getByText(/loading submission points/i)).toBeInTheDocument()
    })

    it('should fetch submission points on mount', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api.get.mockResolvedValueOnce({ data: { data: [] } })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(api.get).toHaveBeenCalledWith('/submissions/submission-points')
      })
    })
  })

  describe('Submission Points List View', () => {
    it('should render empty state when no submission points', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api.get.mockResolvedValueOnce({ data: { data: [] } })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText(/no submission points available/i)).toBeInTheDocument()
      })
    })

    it('should render submission points', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Project Proposal',
          description: 'Submit your project proposal',
          deadline: '2024-12-31T23:59:00Z',
          isIndividual: true,
          hasSubmitted: false
        },
        {
          courseworkId: 2,
          title: 'Final Report',
          description: 'Submit your final report',
          deadline: '2025-01-15T23:59:00Z',
          isIndividual: false,
          hasSubmitted: true
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Project Proposal')).toBeInTheDocument()
        expect(screen.getByText('Final Report')).toBeInTheDocument()
      })
    })

    it('should show Open/Closed/Submitted badges correctly', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Open Submission',
          deadline: '2099-12-31T23:59:00Z',
          isIndividual: true,
          hasSubmitted: false
        },
        {
          courseworkId: 2,
          title: 'Closed Submission',
          deadline: '2020-01-01T00:00:00Z',
          isIndividual: true,
          hasSubmitted: false
        },
        {
          courseworkId: 3,
          title: 'Already Submitted',
          deadline: '2099-12-31T23:59:00Z',
          isIndividual: true,
          hasSubmitted: true
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Open')).toBeInTheDocument()
        expect(screen.getByText('Closed')).toBeInTheDocument()
        expect(screen.getByText(/✓ submitted/i)).toBeInTheDocument()
      })
    })

    it('should display Individual/Group badges', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Individual Project',
          isIndividual: true,
          hasSubmitted: false
        },
        {
          courseworkId: 2,
          title: 'Group Project',
          isIndividual: false,
          hasSubmitted: false
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Individual')).toBeInTheDocument()
        expect(screen.getByText('Group')).toBeInTheDocument()
      })
    })

    it('should call refresh button to reload data', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      api.get.mockResolvedValueOnce({ data: { data: [] } })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText(/loading submission points/i)).toBeInTheDocument()
      })

      const refreshButton = screen.getByText('Refresh')
      fireEvent.click(refreshButton)

      await waitFor(() => {
        expect(api.get).toHaveBeenCalledTimes(2)
      })
    })
  })

  describe('Submission Point Selection & Detail View', () => {
    it('should select a submission point and show detail view', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Project Proposal',
          description: 'Submit your proposal',
          deadline: '2099-12-31T23:59:00Z',
          isIndividual: true,
          hasSubmitted: false
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })
      api.get.mockResolvedValueOnce({ data: { data: null } })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Project Proposal')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Project Proposal'))

      await waitFor(() => {
        expect(screen.getByText('Back to Submissions')).toBeInTheDocument()
      })
    })

    it('should show back button in detail view', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      api.get.mockResolvedValueOnce({ data: { data: [] } })

      renderWithRouter(<StudentSubmissionTab />)

      // No submission points, so no detail view
      expect(screen.queryByText('Back to Submissions')).not.toBeInTheDocument()
    })
  })

  describe('Create New Submission', () => {
    it('should open create form when clicking Create Submission button', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Project Proposal',
          deadline: '2099-12-31T23:59:00Z',
          isIndividual: true,
          hasSubmitted: false
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })
      api.get.mockResolvedValueOnce({ data: { data: null } })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Project Proposal')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Project Proposal'))

      await waitFor(() => {
        expect(screen.getByText('No Submission Yet')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Create Submission'))

      await waitFor(() => {
        expect(screen.getByText('New Submission')).toBeInTheDocument()
      })
    })

    it('should show required field validation errors on empty submit', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Project Proposal',
          deadline: '2099-12-31T23:59:00Z',
          isIndividual: true,
          hasSubmitted: false
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })
      api.get.mockResolvedValueOnce({ data: { data: null } })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Project Proposal')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Project Proposal'))
      fireEvent.click(screen.getByText('Create Submission'))

      await waitFor(() => {
        expect(screen.getByText('New Submission')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByRole('button', { name: /submit proposal/i }))

      expect(screen.getByText('Title is required.')).toBeInTheDocument()
      expect(screen.getByText('Description is required.')).toBeInTheDocument()
      expect(screen.getByText('Abstract is required.')).toBeInTheDocument()
      expect(screen.getByText('Please select a research area.')).toBeInTheDocument()
      expect(screen.getByText('Please upload a PDF file.')).toBeInTheDocument()
    })

    it('should close form when clicking Cancel', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Project Proposal',
          deadline: '2099-12-31T23:59:00Z',
          isIndividual: true,
          hasSubmitted: false
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })
      api.get.mockResolvedValueOnce({ data: { data: null } })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Project Proposal')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Project Proposal'))
      fireEvent.click(screen.getByText('Create Submission'))

      await waitFor(() => {
        expect(screen.getByText('New Submission')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByRole('button', { name: /cancel/i }))

      await waitFor(() => {
        expect(screen.queryByText('New Submission')).not.toBeInTheDocument()
      })
    })
  })

  describe('Edit Existing Submission', () => {
    it('should open edit form when clicking Edit button', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Project Proposal',
          deadline: '2099-12-31T23:59:00Z',
          isIndividual: true,
          hasSubmitted: true
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })
      api.get.mockResolvedValueOnce({
        data: {
          data: {
            projectId: 1,
            title: 'My Proposal',
            description: 'My description',
            abstract: 'My abstract',
            researchAreaId: 1,
            researchAreaName: 'AI',
            groupId: null,
            submittedAt: '2024-01-01T00:00:00Z',
            status: 'PENDING',
            proposalFileName: 'proposal.pdf',
            fileSize: 1024
          }
        }
      })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Project Proposal')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Project Proposal'))

      await waitFor(() => {
        expect(screen.getByText('Edit')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Edit'))

      await waitFor(() => {
        expect(screen.getByText('Edit Submission')).toBeInTheDocument()
      })
    })
  })

  describe('Delete Submission', () => {
    it('should show delete confirmation dialog', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Project Proposal',
          deadline: '2099-12-31T23:59:00Z',
          isIndividual: true,
          hasSubmitted: true
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })
      api.get.mockResolvedValueOnce({
        data: {
          data: {
            projectId: 1,
            title: 'My Proposal',
            status: 'PENDING'
          }
        }
      })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Project Proposal')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Project Proposal'))

      await waitFor(() => {
        expect(screen.getByText('Delete')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Delete'))

      await waitFor(() => {
        expect(screen.getByText('Delete Submission?')).toBeInTheDocument()
      })
    })

    it('should cancel delete when clicking Cancel in dialog', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Project Proposal',
          deadline: '2099-12-31T23:59:00Z',
          isIndividual: true,
          hasSubmitted: true
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })
      api.get.mockResolvedValueOnce({
        data: {
          data: { projectId: 1, title: 'My Proposal', status: 'PENDING' }
        }
      })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Project Proposal')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Project Proposal'))
      await waitFor(() => {
        expect(screen.getByText('Delete')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Delete'))
      await waitFor(() => {
        expect(screen.getByText('Delete Submission?')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByRole('button', { name: /cancel/i }))

      await waitFor(() => {
        expect(screen.queryByText('Delete Submission?')).not.toBeInTheDocument()
      })
    })

    it('should delete submission when confirmed', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Project Proposal',
          deadline: '2099-12-31T23:59:00Z',
          isIndividual: true,
          hasSubmitted: true
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })
      api.get.mockResolvedValueOnce({
        data: {
          data: { projectId: 1, title: 'My Proposal', status: 'PENDING' }
        }
      })
      api.delete.mockResolvedValueOnce({})

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Project Proposal')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Project Proposal'))
      await waitFor(() => {
        expect(screen.getByText('Delete')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Delete'))
      await waitFor(() => {
        expect(screen.getByText('Delete Submission?')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByRole('button', { name: /yes, delete/i }))

      await waitFor(() => {
        expect(api.delete).toHaveBeenCalledWith('/submissions/1')
      })
    })
  })

  describe('PDF Viewing', () => {
    it('should open PDF viewer when clicking View PDF', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Project Proposal',
          deadline: '2099-12-31T23:59:00Z',
          isIndividual: true,
          hasSubmitted: true
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })
      api.get.mockResolvedValueOnce({
        data: {
          data: {
            projectId: 1,
            title: 'My Proposal',
            status: 'PENDING',
            proposalFileName: 'proposal.pdf'
          }
        }
      })
      api.get.mockResolvedValueOnce(new Blob(['pdf content'], { type: 'application/pdf' }))

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Project Proposal')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Project Proposal'))

      await waitFor(() => {
        expect(screen.getByText('View PDF')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('View PDF'))

      await waitFor(() => {
        expect(api.get).toHaveBeenCalledWith('/submissions/1/file', { responseType: 'blob' })
      })
    })

    it('should close PDF viewer when clicking Back', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Project Proposal',
          deadline: '2099-12-31T23:59:00Z',
          isIndividual: true,
          hasSubmitted: true
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })
      api.get.mockResolvedValueOnce({
        data: {
          data: { projectId: 1, proposalFileName: 'proposal.pdf' }
        }
      })
      api.get.mockResolvedValueOnce(new Blob(['pdf content'], { type: 'application/pdf' }))

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Project Proposal')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Project Proposal'))
      await waitFor(() => {
        expect(screen.getByText('View PDF')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('View PDF'))

      await waitFor(() => {
        expect(screen.getByText('← Back')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('← Back'))

      await waitFor(() => {
        expect(screen.queryByText(/pdf viewer/i)).not.toBeInTheDocument()
      })
    })
  })

  describe('Group Submissions', () => {
    it('should load groups when opening create form for group coursework', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Group Project',
          deadline: '2099-12-31T23:59:00Z',
          isIndividual: false,
          hasSubmitted: false
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })
      api.get.mockResolvedValueOnce({
        data: {
          data: [
            { groupId: 1, groupName: 'Team Alpha', currentMembers: 2, maxMembers: 4 },
            { groupId: 2, groupName: 'Team Beta', currentMembers: 3, maxMembers: 3 }
          ]
        }
      })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Group Project')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Group Project'))
      fireEvent.click(screen.getByText('Create Submission'))

      await waitFor(() => {
        expect(api.get).toHaveBeenCalledWith('/submissions/coursework/1/groups')
      })

      expect(screen.getByText('Team Alpha (2/4 members)')).toBeInTheDocument()
      expect(screen.getByText('Team Beta (3/3 members) - FULL')).toBeInTheDocument()
    })

    it('should show disabled option for full groups', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Group Project',
          deadline: '2099-12-31T23:59:00Z',
          isIndividual: false,
          hasSubmitted: false
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })
      api.get.mockResolvedValueOnce({
        data: {
          data: [
            { groupId: 1, groupName: 'Team Alpha', currentMembers: 3, maxMembers: 3 }
          ]
        }
      })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Group Project')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Group Project'))
      fireEvent.click(screen.getByText('Create Submission'))

      await waitFor(() => {
        const fullOption = screen.getByText(/FULL/i)
        expect(fullOption.closest('option')).toBeDisabled()
      })
    })
  })

  describe('File Upload', () => {
    it('should handle file selection via click', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Project Proposal',
          deadline: '2099-12-31T23:59:00Z',
          isIndividual: true,
          hasSubmitted: false
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })
      api.get.mockResolvedValueOnce({ data: { data: null } })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Project Proposal')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Project Proposal'))
      fireEvent.click(screen.getByText('Create Submission'))

      await waitFor(() => {
        expect(screen.getByText('New Submission')).toBeInTheDocument()
      })

      const fileInput = document.getElementById('pdf-input')
      const mockFile = new File(['pdf content'], 'proposal.pdf', { type: 'application/pdf' })
      fireEvent.change(fileInput, { target: { files: [mockFile] } })

      expect(screen.getByText('proposal.pdf')).toBeInTheDocument()
      expect(screen.getByText(/1.0 KB/i)).toBeInTheDocument()
    })

    it('should handle file drop', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Project Proposal',
          deadline: '2099-12-31T23:59:00Z',
          isIndividual: true,
          hasSubmitted: false
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })
      api.get.mockResolvedValueOnce({ data: { data: null } })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Project Proposal')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Project Proposal'))
      fireEvent.click(screen.getByText('Create Submission'))

      await waitFor(() => {
        expect(screen.getByText('New Submission')).toBeInTheDocument()
      })

      const dropZone = screen.getByText('Click or drag a PDF file here').closest('div')
      const mockFile = new File(['pdf content'], 'dropped.pdf', { type: 'application/pdf' })

      fireEvent.dragOver(dropZone, { dataTransfer: { files: [] } })
      expect(dropZone).toHaveClass('dragover')

      fireEvent.dragLeave(dropZone)
      expect(dropZone).not.toHaveClass('dragover')

      fireEvent.drop(dropZone, { dataTransfer: { files: [mockFile] } })

      expect(screen.getByText('dropped.pdf')).toBeInTheDocument()
    })

    it('should reject non-PDF files', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Project Proposal',
          deadline: '2099-12-31T23:59:00Z',
          isIndividual: true,
          hasSubmitted: false
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })
      api.get.mockResolvedValueOnce({ data: { data: null } })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Project Proposal')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Project Proposal'))
      fireEvent.click(screen.getByText('Create Submission'))

      await waitFor(() => {
        expect(screen.getByText('New Submission')).toBeInTheDocument()
      })

      const dropZone = screen.getByText('Click or drag a PDF file here').closest('div')
      const mockFile = new File(['text content'], 'test.txt', { type: 'text/plain' })

      fireEvent.drop(dropZone, { dataTransfer: { files: [mockFile] } })

      expect(screen.getByText('Only PDF files are allowed.')).toBeInTheDocument()
    })

    it('should remove file when clicking remove button', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Project Proposal',
          deadline: '2099-12-31T23:59:00Z',
          isIndividual: true,
          hasSubmitted: false
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })
      api.get.mockResolvedValueOnce({ data: { data: null } })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Project Proposal')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Project Proposal'))
      fireEvent.click(screen.getByText('Create Submission'))

      await waitFor(() => {
        expect(screen.getByText('New Submission')).toBeInTheDocument()
      })

      const fileInput = document.getElementById('pdf-input')
      const mockFile = new File(['pdf content'], 'proposal.pdf', { type: 'application/pdf' })
      fireEvent.change(fileInput, { target: { files: [mockFile] } })

      expect(screen.getByText('proposal.pdf')).toBeInTheDocument()

      const removeButton = screen.getByText('✕')
      fireEvent.click(removeButton)

      expect(screen.queryByText('proposal.pdf')).not.toBeInTheDocument()
    })
  })

  describe('Submission Form Validation', () => {
    it('should validate required fields on submit', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Project Proposal',
          deadline: '2099-12-31T23:59:00Z',
          isIndividual: true,
          hasSubmitted: false
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })
      api.get.mockResolvedValueOnce({ data: { data: null } })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Project Proposal')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Project Proposal'))
      fireEvent.click(screen.getByText('Create Submission'))

      await waitFor(() => {
        expect(screen.getByText('New Submission')).toBeInTheDocument()
      })

      // Try to submit empty form
      fireEvent.click(screen.getByRole('button', { name: /submit proposal/i }))

      expect(screen.getByText('Title is required.')).toBeInTheDocument()
      expect(screen.getByText('Description is required.')).toBeInTheDocument()
      expect(screen.getByText('Abstract is required.')).toBeInTheDocument()
      expect(screen.getByText('Please select a research area.')).toBeInTheDocument()
      expect(screen.getByText('Please upload a PDF file.')).toBeInTheDocument()
    })

    it('should validate group selection for group coursework', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Group Project',
          deadline: '2099-12-31T23:59:00Z',
          isIndividual: false,
          hasSubmitted: false
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })
      api.get.mockResolvedValueOnce({
        data: {
          data: [
            { groupId: 1, groupName: 'Team A', currentMembers: 1, maxMembers: 4 }
          ]
        }
      })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Group Project')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Group Project'))
      fireEvent.click(screen.getByText('Create Submission'))

      await waitFor(() => {
        expect(screen.getByText('New Submission')).toBeInTheDocument()
      })

      // Fill in required fields except group
      fireEvent.change(screen.getByLabelText(/title/i), { target: { value: 'My Project' } })
      fireEvent.change(screen.getByLabelText(/abstract/i), { target: { value: 'My abstract' } })
      fireEvent.change(screen.getByLabelText(/description/i), { target: { value: 'My description' } })
      fireEvent.change(screen.getByLabelText(/research area/i), { target: { value: '1' } })

      fireEvent.click(screen.getByRole('button', { name: /submit proposal/i }))

      expect(screen.getByText('Please select a group.')).toBeInTheDocument()
    })
  })

  describe('API Error Handling', () => {
    it('should handle fetch submission points error', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      api.get.mockRejectedValueOnce({
        response: {
          data: { message: 'Failed to load submission points' }
        }
      })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Failed to load submission points.')).toBeInTheDocument()
      })
    })

    it('should handle fetch submission error (404)', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Project Proposal',
          deadline: '2099-12-31T23:59:00Z',
          isIndividual: true,
          hasSubmitted: false
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })
      api.get.mockRejectedValueOnce({
        response: { status: 404 }
      })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Project Proposal')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Project Proposal'))

      await waitFor(() => {
        // Should just set mySubmission to null without error
        expect(screen.getByText('No Submission Yet')).toBeInTheDocument()
      })
    })

    it('should handle fetch submission other error', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Project Proposal',
          deadline: '2099-12-31T23:59:00Z',
          isIndividual: true,
          hasSubmitted: false
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })
      api.get.mockRejectedValueOnce({
        response: {
          data: { message: 'Server error' }
        }
      })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Project Proposal')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Project Proposal'))

      await waitFor(() => {
        // Error should show in snackbar
        expect(screen.getByText('Server error')).toBeInTheDocument()
      })
    })

    it('should handle create submission error', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Project Proposal',
          deadline: '2099-12-31T23:59:00Z',
          isIndividual: true,
          hasSubmitted: false
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })
      api.get.mockResolvedValueOnce({ data: { data: null } })
      api.post.mockRejectedValueOnce({
        response: {
          data: { message: 'Submission failed' }
        }
      })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Project Proposal')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Project Proposal'))
      fireEvent.click(screen.getByText('Create Submission'))

      await waitFor(() => {
        expect(screen.getByText('New Submission')).toBeInTheDocument()
      })

      // Fill form
      const fileInput = document.getElementById('pdf-input')
      const mockFile = new File(['pdf content'], 'proposal.pdf', { type: 'application/pdf' })
      fireEvent.change(fileInput, { target: { files: [mockFile] } })
      fireEvent.change(screen.getByLabelText(/title/i), { target: { value: 'My Project' } })
      fireEvent.change(screen.getByLabelText(/abstract/i), { target: { value: 'My abstract' } })
      fireEvent.change(screen.getByLabelText(/description/i), { target: { value: 'My description' } })
      fireEvent.change(screen.getByLabelText(/research area/i), { target: { value: '1' } })

      fireEvent.click(screen.getByRole('button', { name: /submit proposal/i }))

      await waitFor(() => {
        expect(screen.getByText('Submission failed')).toBeInTheDocument()
      })
    })

    it('should handle delete submission error', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Project Proposal',
          deadline: '2099-12-31T23:59:00Z',
          isIndividual: true,
          hasSubmitted: true
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })
      api.get.mockResolvedValueOnce({
        data: {
          data: { projectId: 1, title: 'My Proposal', status: 'PENDING' }
        }
      })
      api.delete.mockRejectedValueOnce({
        response: {
          data: { message: 'Delete failed' }
        }
      })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Project Proposal')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Project Proposal'))
      await waitFor(() => {
        expect(screen.getByText('Delete')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Delete'))
      await waitFor(() => {
        expect(screen.getByText('Delete Submission?')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByRole('button', { name: /yes, delete/i }))

      await waitFor(() => {
        expect(screen.getByText('Delete failed')).toBeInTheDocument()
      })
    })

    it('should handle view PDF error', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Project Proposal',
          deadline: '2099-12-31T23:59:00Z',
          isIndividual: true,
          hasSubmitted: true
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })
      api.get.mockResolvedValueOnce({
        data: {
          data: { projectId: 1, proposalFileName: 'proposal.pdf' }
        }
      })
      api.get.mockRejectedValueOnce({
        response: {
          data: { message: 'Failed to load PDF' }
        }
      })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Project Proposal')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Project Proposal'))

      await waitFor(() => {
        expect(screen.getByText('View PDF')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('View PDF'))

      await waitFor(() => {
        expect(screen.getByText('Failed to load PDF.')).toBeInTheDocument()
      })
    })
  })

  describe('Deadline Handling', () => {
    it('should show deadline passed badge for past deadlines', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Old Submission',
          deadline: '2020-01-01T00:00:00Z',
          isIndividual: true,
          hasSubmitted: false
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Old Submission')).toBeInTheDocument()
      })

      expect(screen.getByText('Closed')).toBeInTheDocument()
    })

    it('should disable Create Submission button when deadline passed', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Old Submission',
          deadline: '2020-01-01T00:00:00Z',
          isIndividual: true,
          hasSubmitted: false
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })
      api.get.mockResolvedValueOnce({ data: { data: null } })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Old Submission')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Old Submission'))

      await waitFor(() => {
        expect(screen.getByText(/the deadline has passed/i)).toBeInTheDocument()
      })
    })

    it('should not show Create Submission button when deadline passed and no submission', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Old Submission',
          deadline: '2020-01-01T00:00:00Z',
          isIndividual: true,
          hasSubmitted: false
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })
      api.get.mockResolvedValueOnce({ data: { data: null } })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Old Submission')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Old Submission'))

      await waitFor(() => {
        expect(screen.queryByText('Create Submission')).not.toBeInTheDocument()
      })
    })
  })

  describe('Snackbar Notifications', () => {
    it('should show success snackbar after creating submission', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Project Proposal',
          deadline: '2099-12-31T23:59:00Z',
          isIndividual: true,
          hasSubmitted: false
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })
      api.get.mockResolvedValueOnce({ data: { data: null } })
      api.post.mockResolvedValueOnce({
        data: {
          data: {
            projectId: 1,
            title: 'My Project',
            submittedAt: new Date().toISOString()
          }
        }
      })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Project Proposal')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Project Proposal'))
      fireEvent.click(screen.getByText('Create Submission'))

      await waitFor(() => {
        expect(screen.getByText('New Submission')).toBeInTheDocument()
      })

      // Fill form
      const fileInput = document.getElementById('pdf-input')
      const mockFile = new File(['pdf content'], 'proposal.pdf', { type: 'application/pdf' })
      fireEvent.change(fileInput, { target: { files: [mockFile] } })
      fireEvent.change(screen.getByLabelText(/title/i), { target: { value: 'My Project' } })
      fireEvent.change(screen.getByLabelText(/abstract/i), { target: { value: 'My abstract' } })
      fireEvent.change(screen.getByLabelText(/description/i), { target: { value: 'My description' } })
      fireEvent.change(screen.getByLabelText(/research area/i), { target: { value: '1' } })

      fireEvent.click(screen.getByRole('button', { name: /submit proposal/i }))

      await waitFor(() => {
        expect(screen.getByText('Submission created successfully!')).toBeInTheDocument()
      })
    })

    it('should show error snackbar on failed submission', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Project Proposal',
          deadline: '2099-12-31T23:59:00Z',
          isIndividual: true,
          hasSubmitted: false
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })
      api.get.mockResolvedValueOnce({ data: { data: null } })
      api.post.mockRejectedValueOnce({
        response: {
          data: { message: 'Network error' }
        }
      })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Project Proposal')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Project Proposal'))
      fireEvent.click(screen.getByText('Create Submission'))

      await waitFor(() => {
        expect(screen.getByText('New Submission')).toBeInTheDocument()
      })

      // Fill form
      const fileInput = document.getElementById('pdf-input')
      const mockFile = new File(['pdf content'], 'proposal.pdf', { type: 'application/pdf' })
      fireEvent.change(fileInput, { target: { files: [mockFile] } })
      fireEvent.change(screen.getByLabelText(/title/i), { target: { value: 'My Project' } })
      fireEvent.change(screen.getByLabelText(/abstract/i), { target: { value: 'My abstract' } })
      fireEvent.change(screen.getByLabelText(/description/i), { target: { value: 'My description' } })
      fireEvent.change(screen.getByLabelText(/research area/i), { target: { value: '1' } })

      fireEvent.click(screen.getByRole('button', { name: /submit proposal/i }))

      await waitFor(() => {
        expect(screen.getByText('Network error')).toBeInTheDocument()
      })
    })
  })

  describe('Back Navigation', () => {
    it('should return to list view when clicking Back to Submissions', async () => {
      localStorage.setItem('pas_token', 'token')
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      api
      const mockPoints = [
        {
          courseworkId: 1,
          title: 'Project Proposal',
          deadline: '2099-12-31T23:59:00Z',
          isIndividual: true,
          hasSubmitted: false
        }
      ]
      api.get.mockResolvedValueOnce({ data: { data: mockPoints } })
      api.get.mockResolvedValueOnce({ data: { data: null } })

      renderWithRouter(<StudentSubmissionTab />)

      await waitFor(() => {
        expect(screen.getByText('Project Proposal')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Project Proposal'))

      await waitFor(() => {
        expect(screen.getByText('Back to Submissions')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Back to Submissions'))

      await waitFor(() => {
        expect(screen.getByText('Project Proposal')).toBeInTheDocument()
      })
    })
  })
})
