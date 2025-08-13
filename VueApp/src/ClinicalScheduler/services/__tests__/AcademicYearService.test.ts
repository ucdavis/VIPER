import { describe, it, expect, vi, beforeEach } from 'vitest'
import { AcademicYearService } from '../AcademicYearService'

const mockGet = vi.fn()

vi.mock('../../../composables/ViperFetch', () => ({
  useFetch: () => ({
    get: mockGet
  })
}))

describe('AcademicYearService', () => {
  beforeEach(() => {
    mockGet.mockClear()
  })

  describe('getAcademicYearData', () => {
    it('should correctly parse response from /rotations/years endpoint', async () => {
      // Arrange: Mock the expected backend response
      const mockBackendResponse = {
        CurrentGradYear: 2026,
        AvailableGradYears: [2026, 2025, 2024, 2023, 2022, 2021],
        DefaultYear: 2026
      }
      mockGet.mockResolvedValueOnce(mockBackendResponse)

      // Act: Call the service method
      const result = await AcademicYearService.getAcademicYearData()

      // Assert: Verify the API was called correctly
      expect(mockGet).toHaveBeenCalledWith('/api/clinicalscheduler/rotations/years')
      expect(mockGet).toHaveBeenCalledTimes(1)

      // Assert: Verify response is parsed correctly
      expect(result).toEqual({
        currentGradYear: 2026,
        availableGradYears: [2026, 2025, 2024, 2023, 2022, 2021]
      })
    })

    it('should handle API errors gracefully and return fallback data', async () => {
      // Arrange: Mock API error
      const mockError = new Error('Network error')
      mockGet.mockRejectedValueOnce(mockError)

      // Spy on console.error to verify error logging
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {})

      // Act: Call the service method
      const result = await AcademicYearService.getAcademicYearData()

      // Assert: Verify error handling
      expect(consoleSpy).toHaveBeenCalledWith('Error fetching academic year data:', mockError)

      // Assert: Verify fallback data structure
      expect(result).toEqual({
        currentGradYear: expect.any(Number), // Should be current year
        availableGradYears: []
      })

      // Verify the current year is actually the current calendar year
      expect(result.currentGradYear).toBe(new Date().getFullYear())

      // Cleanup
      consoleSpy.mockRestore()
    })

    it('should handle missing properties in backend response', async () => {
      // Arrange: Mock incomplete backend response
      const incompleteResponse = {
        CurrentGradYear: undefined,
        AvailableGradYears: null
      }
      mockGet.mockResolvedValueOnce(incompleteResponse)

      // Act: Call the service method
      const result = await AcademicYearService.getAcademicYearData()

      // Assert: Verify handling of undefined/null values
      expect(result).toEqual({
        currentGradYear: undefined,
        availableGradYears: null
      })
    })
  })

  describe('getCurrentAcademicYear', () => {
    it('should return current academic year from successful API response', async () => {
      // Arrange
      const mockResponse = {
        CurrentGradYear: 2026,
        AvailableGradYears: [2026, 2025, 2024]
      }
      mockGet.mockResolvedValueOnce(mockResponse)

      // Act
      const result = await AcademicYearService.getCurrentAcademicYear()

      // Assert
      expect(result).toBe(2026)
    })

    it('should fallback to current calendar year when API fails', async () => {
      // Arrange: Mock API error
      mockGet.mockRejectedValueOnce(new Error('API Error'))

      // Spy on console.error to suppress error output in tests
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {})

      // Act
      const result = await AcademicYearService.getCurrentAcademicYear()

      // Assert
      expect(result).toBe(new Date().getFullYear())

      // Cleanup
      consoleSpy.mockRestore()
    })

    it('should fallback to current calendar year when response has undefined currentGradYear', async () => {
      // Arrange
      const mockResponse = {
        CurrentGradYear: undefined,
        AvailableGradYears: []
      }
      mockGet.mockResolvedValueOnce(mockResponse)

      // Act
      const result = await AcademicYearService.getCurrentAcademicYear()

      // Assert
      expect(result).toBe(new Date().getFullYear())
    })
  })

  describe('getAvailableYears', () => {
    it('should return available years from successful API response', async () => {
      // Arrange
      const mockResponse = {
        CurrentGradYear: 2026,
        AvailableGradYears: [2026, 2025, 2024, 2023, 2022, 2021]
      }
      mockGet.mockResolvedValueOnce(mockResponse)

      // Act
      const result = await AcademicYearService.getAvailableYears()

      // Assert
      expect(result).toEqual([2026, 2025, 2024, 2023, 2022, 2021])
    })

    it('should return fallback years when API fails', async () => {
      // Arrange: Mock API error
      mockGet.mockRejectedValueOnce(new Error('API Error'))

      // Spy on console.error to suppress error output in tests
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {})

      // Act
      const result = await AcademicYearService.getAvailableYears()

      // Assert: Verify fallback generates 6 years starting from current year
      const currentYear = new Date().getFullYear()
      const expectedYears = Array.from({ length: 6 }, (_, i) => currentYear - i)
      expect(result).toEqual(expectedYears)

      // Cleanup
      consoleSpy.mockRestore()
    })

    it('should return fallback years when availableGradYears is empty', async () => {
      // Arrange
      const mockResponse = {
        CurrentGradYear: 2026,
        AvailableGradYears: []
      }
      mockGet.mockResolvedValueOnce(mockResponse)

      // Act
      const result = await AcademicYearService.getAvailableYears()

      // Assert
      const currentYear = new Date().getFullYear()
      const expectedYears = Array.from({ length: 6 }, (_, i) => currentYear - i)
      expect(result).toEqual(expectedYears)
    })

    it('should return fallback years when availableGradYears is not an array', async () => {
      // Arrange
      const mockResponse = {
        CurrentGradYear: 2026,
        AvailableGradYears: null
      }
      mockGet.mockResolvedValueOnce(mockResponse)

      // Act
      const result = await AcademicYearService.getAvailableYears()

      // Assert
      const currentYear = new Date().getFullYear()
      const expectedYears = Array.from({ length: 6 }, (_, i) => currentYear - i)
      expect(result).toEqual(expectedYears)
    })
  })

  describe('API endpoint regression test', () => {
    it('should call the correct stable endpoint and not debug endpoints', async () => {
      // Arrange
      const mockResponse = {
        CurrentGradYear: 2026,
        AvailableGradYears: [2026, 2025]
      }
      mockGet.mockResolvedValueOnce(mockResponse)

      // Act
      await AcademicYearService.getAcademicYearData()

      // Assert: Verify it's calling the stable rotations/years endpoint
      expect(mockGet).toHaveBeenCalledWith('/api/clinicalscheduler/rotations/years')

      // Assert: Verify it's NOT calling any debug endpoints
      expect(mockGet).not.toHaveBeenCalledWith(expect.stringContaining('debug'))
      expect(mockGet).not.toHaveBeenCalledWith('/api/clinicalscheduler/clinicians/debug-status')
    })
  })
})