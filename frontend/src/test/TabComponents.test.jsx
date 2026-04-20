import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react'
import { BrowserRouter } from 'react-router-dom'

vi.mock('../api', () => ({
  default: {
    get: vi.fn(() => Promise.resolve({ data: { data: [] } })),
    post: vi.fn(() => Promise.resolve({ data: {} })),
    put: vi.fn(() => Promise.resolve({ data: {} })),
    delete: vi.fn(() => Promise.resolve({ data: {} })),
  }
}))

const renderWithRouter = (component) => {
  return render(<BrowserRouter>{component}</BrowserRouter>)
}

const mockFetch = vi.fn()
global.fetch = mockFetch

describe('SupervisorBrowseTab', () => {
  beforeEach(() => {
    localStorage.clear()
    localStorage.setItem('pas_token', 'mock-token')
    localStorage.setItem('pas_user', JSON.stringify({ userId: 1, role: 'SUPERVISOR', name: 'Test Supervisor' }))
    mockFetch.mockReset()
    vi.spyOn(console, 'log').mockImplementation(() => {})
    vi.spyOn(console, 'error').mockImplementation(() => {})
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('should render supervisor browse tab with empty data', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      json: () => Promise.resolve({ data: [] })
    })
    
    const SupervisorBrowseTab = (await import('../pages/SupervisorBrowseTab')).default
    renderWithRouter(<SupervisorBrowseTab />)
    
    await waitFor(() => {
      expect(screen.getByText(/browse/i)).toBeInTheDocument()
    }, { timeout: 2000 })
  })

  it('should show empty state when no courseworks', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      json: () => Promise.resolve({ data: [] })
    })
    
    const SupervisorBrowseTab = (await import('../pages/SupervisorBrowseTab')).default
    renderWithRouter(<SupervisorBrowseTab />)
    
    await waitFor(() => {
      expect(screen.getByText(/no courseworks/i)).toBeInTheDocument()
    }, { timeout: 2000 })
  })
})

describe('SupervisorSubmissionsTab', () => {
  beforeEach(() => {
    localStorage.clear()
    localStorage.setItem('pas_token', 'mock-token')
    localStorage.setItem('pas_user', JSON.stringify({ userId: 1, role: 'SUPERVISOR', name: 'Test Supervisor' }))
    mockFetch.mockReset()
    vi.spyOn(console, 'log').mockImplementation(() => {})
    vi.spyOn(console, 'error').mockImplementation(() => {})
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('should render submissions tab', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      json: () => Promise.resolve({ data: { matchedProjects: [], pendingReviews: [] } })
    })
    
    const SupervisorSubmissionsTab = (await import('../pages/SupervisorSubmissionsTab')).default
    renderWithRouter(<SupervisorSubmissionsTab />)
    
    await waitFor(() => {
      expect(screen.getByText(/submissions/i)).toBeInTheDocument()
    }, { timeout: 2000 })
  })

  it('should show matched projects count', async () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({ data: { matchedProjects: [{ projectId: 1, title: 'Project 1' }], pendingReviews: [] } })
      })
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({ data: [] })
      })
    
    const SupervisorSubmissionsTab = (await import('../pages/SupervisorSubmissionsTab')).default
    renderWithRouter(<SupervisorSubmissionsTab />)
    
    await waitFor(() => {
      expect(screen.getByText('Matched Projects')).toBeInTheDocument()
    }, { timeout: 2000 })
  })

  it('should show empty state for matched projects', async () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({ data: { matchedProjects: [], pendingReviews: [] } })
      })
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({ data: [] })
      })
    
    const SupervisorSubmissionsTab = (await import('../pages/SupervisorSubmissionsTab')).default
    renderWithRouter(<SupervisorSubmissionsTab />)
    
    await waitFor(() => {
      expect(screen.getByText(/no matched projects/i)).toBeInTheDocument()
    }, { timeout: 2000 })
  })

  it('should show loading state initially', async () => {
    mockFetch.mockImplementation(() => new Promise(() => {}))
    
    const SupervisorSubmissionsTab = (await import('../pages/SupervisorSubmissionsTab')).default
    renderWithRouter(<SupervisorSubmissionsTab />)
    
    expect(screen.getByText(/loading submissions/i)).toBeInTheDocument()
  })
})

describe('StudentSubmissionTab', () => {
  beforeEach(() => {
    localStorage.clear()
    localStorage.setItem('pas_token', 'mock-token')
    localStorage.setItem('pas_user', JSON.stringify({ userId: 1, role: 'STUDENT', name: 'Test Student' }))
    mockFetch.mockReset()
    vi.spyOn(console, 'log').mockImplementation(() => {})
    vi.spyOn(console, 'error').mockImplementation(() => {})
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('should render submission tab', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      json: () => Promise.resolve({ data: null })
    })
    
    const StudentSubmissionTab = (await import('../pages/StudentSubmissionTab')).default
    renderWithRouter(<StudentSubmissionTab />)
    
    await waitFor(() => {
      expect(screen.getByText('Submission Portal')).toBeInTheDocument()
    }, { timeout: 2000 })
  })
})

describe('StudentProjectTab', () => {
  beforeEach(() => {
    localStorage.clear()
    localStorage.setItem('pas_token', 'mock-token')
    localStorage.setItem('pas_user', JSON.stringify({ userId: 1, role: 'STUDENT', name: 'Test Student' }))
    mockFetch.mockReset()
    vi.spyOn(console, 'log').mockImplementation(() => {})
    vi.spyOn(console, 'error').mockImplementation(() => {})
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('should show empty state when no submissions', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      json: () => Promise.resolve({ data: { data: [] } })
    })
    
    const StudentProjectTab = (await import('../pages/StudentProjectTab')).default
    renderWithRouter(<StudentProjectTab />)
    
    await waitFor(() => {
      expect(screen.getByText(/no submissions yet/i)).toBeInTheDocument()
    }, { timeout: 2000 })
  })

  it('should show loading state initially', async () => {
    mockFetch.mockImplementation(() => new Promise(() => {}))
    
    const StudentProjectTab = (await import('../pages/StudentProjectTab')).default
    renderWithRouter(<StudentProjectTab />)
    
    expect(screen.getByText(/loading/i)).toBeInTheDocument()
  })
})

describe('AllocationOversightTab', () => {
  beforeEach(() => {
    localStorage.clear()
    localStorage.setItem('pas_token', 'mock-token')
    localStorage.setItem('pas_user', JSON.stringify({ userId: 1, role: 'ADMIN', name: 'Test Admin' }))
    mockFetch.mockReset()
    vi.spyOn(console, 'log').mockImplementation(() => {})
    vi.spyOn(console, 'error').mockImplementation(() => {})
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('should render allocation oversight tab', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      text: () => Promise.resolve(JSON.stringify({ data: [] }))
    })
    
    const AllocationOversightTab = (await import('../pages/AllocationOversightTab')).default
    renderWithRouter(<AllocationOversightTab />)
    
    await waitFor(() => {
      expect(screen.getByText('Allocation Oversight')).toBeInTheDocument()
    }, { timeout: 2000 })
  })

  it('should display allocations table', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      text: () => Promise.resolve(JSON.stringify({ data: [{ id: 1, studentName: 'Student 1', supervisorName: 'Supervisor 1', projectName: 'Project 1', status: 'matched' }] }))
    })
    
    const AllocationOversightTab = (await import('../pages/AllocationOversightTab')).default
    renderWithRouter(<AllocationOversightTab />)
    
    await waitFor(() => {
      expect(screen.getByText('Student 1')).toBeInTheDocument()
      expect(screen.getByText('Supervisor 1')).toBeInTheDocument()
    }, { timeout: 2000 })
  })

  it('should display filter dropdown', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      text: () => Promise.resolve(JSON.stringify({ data: [] }))
    })
    
    const AllocationOversightTab = (await import('../pages/AllocationOversightTab')).default
    renderWithRouter(<AllocationOversightTab />)
    
    await waitFor(() => {
      expect(screen.getByRole('combobox')).toBeInTheDocument()
    }, { timeout: 2000 })
  })

  it('should show empty state when no allocations', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      text: () => Promise.resolve(JSON.stringify({ data: [] }))
    })
    
    const AllocationOversightTab = (await import('../pages/AllocationOversightTab')).default
    renderWithRouter(<AllocationOversightTab />)
    
    await waitFor(() => {
      expect(screen.getByText(/no allocations found/i)).toBeInTheDocument()
    }, { timeout: 2000 })
  })

  it('should show error when not authenticated', async () => {
    localStorage.removeItem('pas_token')
    
    const AllocationOversightTab = (await import('../pages/AllocationOversightTab')).default
    renderWithRouter(<AllocationOversightTab />)
    
    await waitFor(() => {
      expect(screen.getByText(/not authenticated/i)).toBeInTheDocument()
    }, { timeout: 2000 })
  })

  it('should have refresh button', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      text: () => Promise.resolve(JSON.stringify({ data: [] }))
    })
    
    const AllocationOversightTab = (await import('../pages/AllocationOversightTab')).default
    renderWithRouter(<AllocationOversightTab />)
    
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /refresh/i })).toBeInTheDocument()
    }, { timeout: 2000 })
  })

  it('should have intervention tools button', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      text: () => Promise.resolve(JSON.stringify({ data: [] }))
    })
    
    const AllocationOversightTab = (await import('../pages/AllocationOversightTab')).default
    renderWithRouter(<AllocationOversightTab />)
    
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /intervention tools/i })).toBeInTheDocument()
    }, { timeout: 2000 })
  })

  it('should filter allocations by status', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      text: () => Promise.resolve(JSON.stringify({ data: [{ id: 1, status: 'matched' }] }))
    })
    
    const AllocationOversightTab = (await import('../pages/AllocationOversightTab')).default
    renderWithRouter(<AllocationOversightTab />)
    
    await waitFor(() => {
      const select = screen.getByRole('combobox')
      fireEvent.change(select, { target: { value: 'matched' } })
    })
  })

  it('should show loading state', async () => {
    mockFetch.mockImplementation(() => new Promise(() => {}))
    
    const AllocationOversightTab = (await import('../pages/AllocationOversightTab')).default
    renderWithRouter(<AllocationOversightTab />)
    
    expect(screen.getByText(/loading allocations/i)).toBeInTheDocument()
  })
})

describe('ModuleLeaderManagementTab', () => {
  beforeEach(() => {
    localStorage.clear()
    localStorage.setItem('pas_token', 'mock-token')
    localStorage.setItem('pas_user', JSON.stringify({ userId: 1, role: 'MODULE_LEADER', name: 'Test ML' }))
    mockFetch.mockReset()
    vi.spyOn(console, 'log').mockImplementation(() => {})
    vi.spyOn(console, 'error').mockImplementation(() => {})
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('should render module leader management tab', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      json: () => Promise.resolve({ data: [] })
    })
    
    const ModuleLeaderManagementTab = (await import('../pages/ModuleLeaderManagementTab')).default
    renderWithRouter(<ModuleLeaderManagementTab />)
    
    await waitFor(() => {
      expect(screen.getByText('Module Leader Management')).toBeInTheDocument()
    }, { timeout: 2000 })
  })

  it('should show empty state', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      json: () => Promise.resolve({ data: [] })
    })
    
    const ModuleLeaderManagementTab = (await import('../pages/ModuleLeaderManagementTab')).default
    renderWithRouter(<ModuleLeaderManagementTab />)
    
    await waitFor(() => {
      expect(screen.getByText(/no module leaders found/i)).toBeInTheDocument()
    }, { timeout: 2000 })
  })
})

describe('SupervisorManagementTab', () => {
  beforeEach(() => {
    localStorage.clear()
    localStorage.setItem('pas_token', 'mock-token')
    localStorage.setItem('pas_user', JSON.stringify({ userId: 1, role: 'ADMIN', name: 'Test Admin' }))
    mockFetch.mockReset()
    vi.spyOn(console, 'log').mockImplementation(() => {})
    vi.spyOn(console, 'error').mockImplementation(() => {})
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('should render supervisor management tab', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      json: () => Promise.resolve({ data: [] })
    })
    
    const SupervisorManagementTab = (await import('../pages/SupervisorManagementTab')).default
    renderWithRouter(<SupervisorManagementTab />)
    
    await waitFor(() => {
      expect(screen.getByText('Supervisor Management')).toBeInTheDocument()
    }, { timeout: 2000 })
  })
})

describe('StudentManagementTab', () => {
  beforeEach(() => {
    localStorage.clear()
    localStorage.setItem('pas_token', 'mock-token')
    localStorage.setItem('pas_user', JSON.stringify({ userId: 1, role: 'ADMIN', name: 'Test Admin' }))
    mockFetch.mockReset()
    vi.spyOn(console, 'log').mockImplementation(() => {})
    vi.spyOn(console, 'error').mockImplementation(() => {})
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('should render student management tab', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      json: () => Promise.resolve({ data: [] })
    })
    
    const StudentManagementTab = (await import('../pages/StudentManagementTab')).default
    renderWithRouter(<StudentManagementTab />)
    
    await waitFor(() => {
      expect(screen.getByText('Student Management')).toBeInTheDocument()
    }, { timeout: 2000 })
  })

  it('should display create student button', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      json: () => Promise.resolve({ data: [] })
    })
    
    const StudentManagementTab = (await import('../pages/StudentManagementTab')).default
    renderWithRouter(<StudentManagementTab />)
    
    await waitFor(() => {
      expect(screen.getByText('+ Create Student')).toBeInTheDocument()
    }, { timeout: 2000 })
  })
})

describe('SubmissionManagementTab', () => {
  beforeEach(() => {
    localStorage.clear()
    localStorage.setItem('pas_token', 'mock-token')
    localStorage.setItem('pas_user', JSON.stringify({ userId: 1, role: 'ADMIN', name: 'Test Admin' }))
    mockFetch.mockReset()
    vi.spyOn(console, 'log').mockImplementation(() => {})
    vi.spyOn(console, 'error').mockImplementation(() => {})
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('should render submission management tab', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      json: () => Promise.resolve({ data: [] })
    })
    
    const SubmissionManagementTab = (await import('../pages/SubmissionManagementTab')).default
    renderWithRouter(<SubmissionManagementTab />)
    
    await waitFor(() => {
      expect(screen.getByText('Submissions')).toBeInTheDocument()
    }, { timeout: 2000 })
  })
})

describe('ResearchAreaManagementTab', () => {
  beforeEach(() => {
    localStorage.clear()
    localStorage.setItem('pas_token', 'mock-token')
    localStorage.setItem('pas_user', JSON.stringify({ userId: 1, role: 'ADMIN', name: 'Test Admin' }))
    mockFetch.mockReset()
    vi.spyOn(console, 'log').mockImplementation(() => {})
    vi.spyOn(console, 'error').mockImplementation(() => {})
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('should render research area management tab', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      json: () => Promise.resolve({ data: [] })
    })
    
    const ResearchAreaManagementTab = (await import('../pages/ResearchAreaManagementTab')).default
    renderWithRouter(<ResearchAreaManagementTab />)
    
    await waitFor(() => {
      expect(screen.getByText('Research Areas')).toBeInTheDocument()
    }, { timeout: 2000 })
  })
})