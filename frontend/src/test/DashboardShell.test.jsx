import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { BrowserRouter, MemoryRouter } from 'react-router-dom'
import DashboardShell from '../pages/DashboardShell'

vi.mock('../api', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
  }
}))

const renderWithRouter = (component) => {
  return render(<BrowserRouter>{component}</BrowserRouter>)
}

describe('DashboardShell', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.spyOn(console, 'log').mockImplementation(() => {})
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  const mockUser = { userId: 1, name: 'John Doe', email: 'john@university.ac.uk', role: 'STUDENT', batch: '2024' }

  describe('Component Rendering', () => {
    it('should render dashboard shell with portal name', () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      renderWithRouter(
        <DashboardShell portalName="STUDENT PORTAL" tabs={[{ id: 'tab1', label: 'Tab 1' }]} roleClass="student">
          <div>Content</div>
        </DashboardShell>
      )
      expect(screen.getByText('STUDENT PORTAL')).toBeInTheDocument()
    })

    it('should render tabs', () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      renderWithRouter(
        <DashboardShell 
          portalName="TEST PORTAL" 
          tabs={[
            { id: 'tab1', label: 'Tab One' },
            { id: 'tab2', label: 'Tab Two' }
          ]} 
          roleClass="admin"
        >
          <div>Content</div>
        </DashboardShell>
      )
      expect(screen.getByText('Tab One')).toBeInTheDocument()
      expect(screen.getByText('Tab Two')).toBeInTheDocument()
    })

    it('should render user info when logged in', () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      renderWithRouter(
        <DashboardShell portalName="TEST" tabs={[{ id: 't', label: 'T' }]} roleClass="student">
          <div>Content</div>
        </DashboardShell>
      )
      expect(screen.getByText('John Doe')).toBeInTheDocument()
      expect(screen.getByText('john@university.ac.uk')).toBeInTheDocument()
    })

    it('should render sign out button', () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      renderWithRouter(
        <DashboardShell portalName="TEST" tabs={[{ id: 't', label: 'T' }]} roleClass="student">
          <div>Content</div>
        </DashboardShell>
      )
      expect(screen.getByText('Sign Out')).toBeInTheDocument()
    })

    it('should render logo', () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      renderWithRouter(
        <DashboardShell portalName="TEST" tabs={[{ id: 't', label: 'T' }]} roleClass="student">
          <div>Content</div>
        </DashboardShell>
      )
      expect(screen.getByAltText('PAS logo')).toBeInTheDocument()
    })
  })

  describe('Tab Navigation', () => {
    it('should set first tab as active by default', () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      renderWithRouter(
        <DashboardShell 
          portalName="TEST" 
          tabs={[
            { id: 'first', label: 'First Tab' },
            { id: 'second', label: 'Second Tab' }
          ]} 
          roleClass="student"
        >
          {({ activeTab }) => <div data-testid="active-tab">{activeTab}</div>}
        </DashboardShell>
      )
      expect(screen.getByTestId('active-tab').textContent).toBe('first')
    })

    it('should change active tab on click', () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      renderWithRouter(
        <DashboardShell 
          portalName="TEST" 
          tabs={[
            { id: 'first', label: 'First Tab' },
            { id: 'second', label: 'Second Tab' }
          ]} 
          roleClass="student"
        >
          {({ activeTab }) => <div data-testid="active-tab">{activeTab}</div>}
        </DashboardShell>
      )
      
      const secondTab = screen.getByText('Second Tab')
      fireEvent.click(secondTab)
      
      expect(screen.getByTestId('active-tab').textContent).toBe('second')
    })

    it('should add active class to current tab', () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      renderWithRouter(
        <DashboardShell 
          portalName="TEST" 
          tabs={[
            { id: 'first', label: 'First Tab' },
            { id: 'second', label: 'Second Tab' }
          ]} 
          roleClass="student"
        >
          <div>Content</div>
        </DashboardShell>
      )
      
      const firstTab = screen.getByText('First Tab')
      expect(firstTab.className).toContain('active')
    })
  })

  describe('User Initials', () => {
    it('should show initials from user name', () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      renderWithRouter(
        <DashboardShell portalName="TEST" tabs={[{ id: 't', label: 'T' }]} roleClass="student">
          <div>Content</div>
        </DashboardShell>
      )
      expect(screen.getByText('JD')).toBeInTheDocument()
    })

    it('should handle single word name', () => {
      const singleNameUser = { ...mockUser, name: 'John' }
      localStorage.setItem('pas_user', JSON.stringify(singleNameUser))
      renderWithRouter(
        <DashboardShell portalName="TEST" tabs={[{ id: 't', label: 'T' }]} roleClass="student">
          <div>Content</div>
        </DashboardShell>
      )
      expect(screen.getByText('J')).toBeInTheDocument()
    })

    it('should show ? when no user name', () => {
      const noNameUser = { ...mockUser, name: '' }
      localStorage.setItem('pas_user', JSON.stringify(noNameUser))
      renderWithRouter(
        <DashboardShell portalName="TEST" tabs={[{ id: 't', label: 'T' }]} roleClass="student">
          <div>Content</div>
        </DashboardShell>
      )
      expect(screen.getByText('?')).toBeInTheDocument()
    })
  })

  describe('Logout', () => {
    it('should clear localStorage on logout', () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      localStorage.setItem('pas_token', 'some-token')
      
      renderWithRouter(
        <DashboardShell portalName="TEST" tabs={[{ id: 't', label: 'T' }]} roleClass="student">
          <div>Content</div>
        </DashboardShell>
      )
      
      const logoutButton = screen.getByText('Sign Out')
      fireEvent.click(logoutButton)
      
      expect(localStorage.getItem('pas_token')).toBeNull()
      expect(localStorage.getItem('pas_user')).toBeNull()
    })
  })

  describe('Render Function Children', () => {
    it('should pass activeTab to function children', () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      renderWithRouter(
        <DashboardShell portalName="TEST" tabs={[{ id: 'mytab', label: 'My Tab' }]} roleClass="student">
          {({ activeTab }) => <span>Current: {activeTab}</span>}
        </DashboardShell>
      )
      expect(screen.getByText('Current: mytab')).toBeInTheDocument()
    })
  })

  describe('Role Classes', () => {
    it('should apply student role class', () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      renderWithRouter(
        <DashboardShell portalName="TEST" tabs={[{ id: 't', label: 'T' }]} roleClass="student">
          <div>Content</div>
        </DashboardShell>
      )
      const avatar = screen.getByText('JD')
      expect(avatar.className).toContain('student')
    })

    it('should apply admin role class', () => {
      const adminUser = { ...mockUser, role: 'ADMIN' }
      localStorage.setItem('pas_user', JSON.stringify(adminUser))
      renderWithRouter(
        <DashboardShell portalName="TEST" tabs={[{ id: 't', label: 'T' }]} roleClass="admin">
          <div>Content</div>
        </DashboardShell>
      )
      const avatar = screen.getByText('JD')
      expect(avatar.className).toContain('admin')
    })
  })

  describe('Edge Cases', () => {
    it('should handle empty tabs array', () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      renderWithRouter(
        <DashboardShell portalName="TEST" tabs={[]} roleClass="student">
          <div>Content</div>
        </DashboardShell>
      )
      expect(screen.getByText('Content')).toBeInTheDocument()
    })

    it('should reset activeTab if not found in tabs', () => {
      localStorage.setItem('pas_user', JSON.stringify(mockUser))
      const { rerender } = renderWithRouter(
        <DashboardShell portalName="TEST" tabs={[{ id: 'first', label: 'First' }]} roleClass="student">
          {({ activeTab }) => <div data-testid="tab">{activeTab}</div>}
        </DashboardShell>
      )
      
      rerender(
        <BrowserRouter>
          <DashboardShell portalName="TEST" tabs={[{ id: 'new', label: 'New' }]} roleClass="student">
            {({ activeTab }) => <div data-testid="tab">{activeTab}</div>}
          </DashboardShell>
        </BrowserRouter>
      )
    })
  })
})

describe('StudentDashboard', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.spyOn(console, 'log').mockImplementation(() => {})
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('should render student dashboard with tabs', async () => {
    const mockUser = { userId: 1, name: 'John Doe', email: 'john@test.com', role: 'STUDENT', batch: '2024' }
    localStorage.setItem('pas_user', JSON.stringify(mockUser))
    localStorage.setItem('pas_token', 'token')
    
    const StudentDashboard = (await import('../pages/StudentDashboard')).default
    renderWithRouter(<StudentDashboard />)
    
    expect(screen.getByText('STUDENT PORTAL')).toBeInTheDocument()
  })
})

describe('SupervisorDashboard', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.spyOn(console, 'log').mockImplementation(() => {})
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('should render supervisor dashboard with tabs', async () => {
    const mockUser = { userId: 1, name: 'Dr. Smith', email: 'smith@test.com', role: 'SUPERVISOR' }
    localStorage.setItem('pas_user', JSON.stringify(mockUser))
    localStorage.setItem('pas_token', 'token')
    
    const SupervisorDashboard = (await import('../pages/SupervisorDashboard')).default
    renderWithRouter(<SupervisorDashboard />)
    
    expect(screen.getByText('SUPERVISOR PORTAL')).toBeInTheDocument()
  })
})

describe('ModuleLeaderDashboard', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.spyOn(console, 'log').mockImplementation(() => {})
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('should render module leader dashboard', async () => {
    const mockUser = { userId: 1, name: 'Prof. Johnson', email: 'johnson@test.com', role: 'MODULE_LEADER' }
    localStorage.setItem('pas_user', JSON.stringify(mockUser))
    localStorage.setItem('pas_token', 'token')
    
    const ModuleLeaderDashboard = (await import('../pages/ModuleLeaderDashboard')).default
    renderWithRouter(<ModuleLeaderDashboard />)
    
    expect(screen.getByText('MODULE LEADER PORTAL')).toBeInTheDocument()
  })
})

describe('AdminDashboard', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.spyOn(console, 'log').mockImplementation(() => {})
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('should render admin dashboard', async () => {
    const mockUser = { userId: 1, name: 'Admin User', email: 'admin@test.com', role: 'ADMIN' }
    localStorage.setItem('pas_user', JSON.stringify(mockUser))
    localStorage.setItem('pas_token', 'token')
    
    const AdminDashboard = (await import('../pages/AdminDashboard')).default
    renderWithRouter(<AdminDashboard />)
    
    expect(screen.getByText('ADMIN PORTAL')).toBeInTheDocument()
  })
})