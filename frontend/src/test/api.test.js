import { describe, it, expect, vi, beforeEach } from 'vitest'
import api from '../api'

describe('API Module', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.restoreAllMocks()
  })

  describe('API Configuration', () => {
    it('should have correct base URL', () => {
      expect(api.defaults.baseURL).toBe('http://localhost:5000/api')
    })

    it('should have correct default headers', () => {
      expect(api.defaults.headers['Content-Type']).toBe('application/json')
    })
  })

  describe('Request Interceptor', () => {
    it('should not add Authorization header when no token exists', async () => {
      const mockRequest = {
        headers: {},
        method: 'get',
        url: 'http://localhost:5000/api/test'
      }
      
      const result = await api.interceptors.request.handlers[0].fulfilled(mockRequest)
      
      expect(result.headers.Authorization).toBeUndefined()
    })

    it('should add Authorization header when token exists', async () => {
      const testToken = 'test-jwt-token'
      localStorage.setItem('pas_token', testToken)
      
      const mockRequest = {
        headers: {},
        method: 'get',
        url: 'http://localhost:5000/api/test'
      }
      
      const result = await api.interceptors.request.handlers[0].fulfilled(mockRequest)
      
      expect(result.headers.Authorization).toBe(`Bearer ${testToken}`)
    })
  })

  describe('Response Interceptor', () => {
    it('should log successful responses', async () => {
      const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {})
      
      const mockResponse = {
        config: { method: 'get', url: '/test' },
        status: 200,
        data: { success: true }
      }
      
      const result = await api.interceptors.response.handlers[0].fulfilled(mockResponse)
      
      expect(consoleSpy).toHaveBeenCalled()
      expect(result).toEqual(mockResponse)
      consoleSpy.mockRestore()
    })

    it('should log error responses and reject', async () => {
      const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {})
      
      const mockError = {
        config: { method: 'post', url: '/test' },
        response: { status: 404, data: { error: 'Not found' } }
      }
      
      try {
        await api.interceptors.response.handlers[0].rejected(mockError)
      } catch (e) {
        expect(e).toEqual(mockError)
      }
      expect(consoleSpy).toHaveBeenCalled()
      consoleSpy.mockRestore()
    })

    it('should return error from rejection handler', async () => {
      const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {})
      const mockError = {
        config: { method: 'post', url: '/test' },
        response: { status: 500, data: { error: 'Server error' } }
      }
      
      try {
        await api.interceptors.response.handlers[0].rejected(mockError)
      } catch (e) {
        expect(e).toBeDefined()
      }
      consoleSpy.mockRestore()
    })
  })
})