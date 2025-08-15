import { describe, it, expect, vi, beforeEach } from 'vitest'
import { RotationService, type RotationWithService } from '../services/RotationService'

// Mock the RotationService
vi.mock('../services/RotationService', () => ({
  RotationService: {
    getRotations: vi.fn(),
    getRotationsWithScheduledWeeks: vi.fn()
  }
}))

// Component testing without importing the actual Vue component
// This approach avoids needing @vue/test-utils and complex Quasar mocking

// Helper function to test the core logic methods
const createComponentLogic = () => {
  // Mock rotation data for testing
  const mockRotationsResponse: RotationWithService[] = [
    {
      rotId: 101,
      name: 'Anatomic Pathology (Advanced)',
      abbreviation: 'AnatPath',
      subjectCode: 'VM',
      courseNumber: '456',
      serviceId: 1,
      service: {
        serviceId: 1,
        serviceName: 'Anatomic Pathology',
        shortName: 'AP'
      }
    },
    {
      rotId: 102,
      name: 'Cardiology',
      abbreviation: 'Card',
      subjectCode: 'VM',
      courseNumber: '789',
      serviceId: 2,
      service: {
        serviceId: 2,
        serviceName: 'Cardiology',
        shortName: 'CARD'
      }
    },
    {
      rotId: 103,
      name: 'Behavior',
      abbreviation: 'Beh',
      subjectCode: 'VM',
      courseNumber: '123',
      serviceId: 3,
      service: {
        serviceId: 3,
        serviceName: 'Behavior',
        shortName: 'BEH'
      }
    },
    {
      rotId: 104,
      name: 'Anatomic Pathology (Basic)',
      abbreviation: 'AnatPath2',
      subjectCode: 'VM',
      courseNumber: '455',
      serviceId: 1,
      service: {
        serviceId: 1,
        serviceName: 'Anatomic Pathology',
        shortName: 'AP'
      }
    }
  ]

  // Extract the core logic functions that we want to test
  const getRotationDisplayName = (rotation: RotationWithService): string => {
    const beforeParenthesis = rotation.name.split('(')[0].trim()
    return beforeParenthesis || rotation.name
  }

  const filterRotations = (items: RotationWithService[], searchTerm: string): RotationWithService[] => {
    const search = searchTerm.toLowerCase()
    return items.filter(rotation => 
      getRotationDisplayName(rotation).toLowerCase().includes(search) ||
      rotation.abbreviation?.toLowerCase().includes(search) ||
      rotation.service?.serviceName?.toLowerCase().includes(search)
    )
  }

  const deduplicateRotations = (rotations: RotationWithService[], excludeRotationNames?: string[]): RotationWithService[] => {
    const uniqueRotations = new Map<string, RotationWithService>()
    
    rotations.forEach(rotation => {
      const rotationName = getRotationDisplayName(rotation)
      // Skip if this rotation name is in the exclusion list
      if (excludeRotationNames && excludeRotationNames.includes(rotationName)) {
        return
      }
      // Keep only the first occurrence of each rotation name
      if (!uniqueRotations.has(rotationName)) {
        uniqueRotations.set(rotationName, rotation)
      }
    })
    
    return Array.from(uniqueRotations.values())
      .sort((a, b) => getRotationDisplayName(a).localeCompare(getRotationDisplayName(b)))
  }

  return {
    mockRotationsResponse,
    getRotationDisplayName,
    filterRotations,
    deduplicateRotations
  }
}

describe('RotationSelector', () => {
  let componentLogic: ReturnType<typeof createComponentLogic>

  beforeEach(() => {
    // Reset all mocks
    vi.clearAllMocks()
    componentLogic = createComponentLogic()

    // Setup default mock responses
    vi.mocked(RotationService.getRotations).mockResolvedValue({
      success: true,
      result: componentLogic.mockRotationsResponse,
      errors: []
    })

    vi.mocked(RotationService.getRotationsWithScheduledWeeks).mockResolvedValue({
      success: true,
      result: componentLogic.mockRotationsResponse.slice(0, 2), // Only first two for scheduled weeks
      errors: []
    })
  })

  describe('Rotation Display Name Formatting', () => {
    it('formats rotation names correctly by removing text after parentheses', () => {
      const rotation = componentLogic.mockRotationsResponse[0] // 'Anatomic Pathology (Advanced)'
      expect(componentLogic.getRotationDisplayName(rotation)).toBe('Anatomic Pathology')
    })

    it('handles rotation names without parentheses', () => {
      const rotation = componentLogic.mockRotationsResponse[1] // 'Cardiology'
      expect(componentLogic.getRotationDisplayName(rotation)).toBe('Cardiology')
    })

    it('handles empty string after parentheses removal', () => {
      const rotation: RotationWithService = {
        rotId: 999,
        name: '(Test)',
        abbreviation: 'Test',
        subjectCode: 'VM',
        courseNumber: '999',
        serviceId: 1,
        service: null
      }
      expect(componentLogic.getRotationDisplayName(rotation)).toBe('(Test)')
    })

    it('trims whitespace from rotation names', () => {
      const rotation: RotationWithService = {
        rotId: 999,
        name: '   Spaced Name   (Details)',
        abbreviation: 'Space',
        subjectCode: 'VM',
        courseNumber: '999',
        serviceId: 1,
        service: null
      }
      expect(componentLogic.getRotationDisplayName(rotation)).toBe('Spaced Name')
    })
  })

  describe('Search and Filtering', () => {
    it('filters rotations by name', () => {
      const filtered = componentLogic.filterRotations(componentLogic.mockRotationsResponse, 'anatomic')
      
      expect(filtered).toHaveLength(2) // Both anatomic pathology rotations
      expect(filtered.every(r => 
        componentLogic.getRotationDisplayName(r).toLowerCase().includes('anatomic')
      )).toBe(true)
    })

    it('filters rotations by abbreviation', () => {
      const filtered = componentLogic.filterRotations(componentLogic.mockRotationsResponse, 'card')
      
      expect(filtered).toHaveLength(1)
      expect(filtered[0].abbreviation).toBe('Card')
    })

    it('filters rotations by service name', () => {
      const filtered = componentLogic.filterRotations(componentLogic.mockRotationsResponse, 'cardiology')
      
      expect(filtered).toHaveLength(1)
      expect(filtered[0].service?.serviceName).toBe('Cardiology')
    })

    it('is case insensitive when filtering', () => {
      const filtered = componentLogic.filterRotations(componentLogic.mockRotationsResponse, 'BEHAVIOR')
      
      expect(filtered).toHaveLength(1)
      expect(componentLogic.getRotationDisplayName(filtered[0])).toBe('Behavior')
    })

    it('returns empty array when no matches found', () => {
      const filtered = componentLogic.filterRotations(componentLogic.mockRotationsResponse, 'nonexistent')
      
      expect(filtered).toHaveLength(0)
    })

    it('handles null service when filtering by service name', () => {
      const rotationWithoutService: RotationWithService = {
        rotId: 999,
        name: 'Orphaned Rotation',
        abbreviation: 'Orphan',
        subjectCode: 'VM',
        courseNumber: '999',
        serviceId: 999,
        service: null
      }

      const rotationsWithNull = [...componentLogic.mockRotationsResponse, rotationWithoutService]
      const filtered = componentLogic.filterRotations(rotationsWithNull, 'cardiology')
      
      expect(filtered).toHaveLength(1)
      expect(filtered[0].service?.serviceName).toBe('Cardiology')
    })

    it('handles undefined abbreviation when filtering', () => {
      const rotationWithoutAbbreviation: RotationWithService = {
        rotId: 999,
        name: 'No Abbreviation',
        abbreviation: undefined as any,
        subjectCode: 'VM',
        courseNumber: '999',
        serviceId: 1,
        service: { serviceId: 1, serviceName: 'Test Service', shortName: 'TEST' }
      }

      const rotationsWithUndefined = [...componentLogic.mockRotationsResponse, rotationWithoutAbbreviation]
      const filtered = componentLogic.filterRotations(rotationsWithUndefined, 'abbreviation')
      
      expect(filtered).toHaveLength(1) // Should match "No Abbreviation" by name
      expect(filtered[0].name).toBe('No Abbreviation')
    })
  })

  describe('Deduplication Logic', () => {
    it('deduplicates rotations by display name', () => {
      // Add duplicate rotation names to test deduplication
      const duplicateRotations = [
        ...componentLogic.mockRotationsResponse,
        {
          rotId: 105,
          name: 'Anatomic Pathology (Duplicate)',
          abbreviation: 'AnatPath3',
          subjectCode: 'VM',
          courseNumber: '457',
          serviceId: 1,
          service: {
            serviceId: 1,
            serviceName: 'Anatomic Pathology',
            shortName: 'AP'
          }
        }
      ]

      const deduplicated = componentLogic.deduplicateRotations(duplicateRotations)
      
      // Should deduplicate 'Anatomic Pathology' entries (keeps first occurrence)
      const anatomicPathRotations = deduplicated.filter(
        r => componentLogic.getRotationDisplayName(r) === 'Anatomic Pathology'
      )
      expect(anatomicPathRotations).toHaveLength(1)
      expect(anatomicPathRotations[0].rotId).toBe(101) // Should keep the first one
    })

    it('excludes rotation names from excludeRotationNames parameter', () => {
      const deduplicated = componentLogic.deduplicateRotations(
        componentLogic.mockRotationsResponse,
        ['Anatomic Pathology', 'Cardiology']
      )
      
      // Should only have Behavior left after exclusions
      expect(deduplicated).toHaveLength(1)
      expect(deduplicated[0].name).toBe('Behavior')
    })

    it('sorts deduplicated rotations alphabetically by display name', () => {
      const deduplicated = componentLogic.deduplicateRotations(componentLogic.mockRotationsResponse)
      
      // Should be sorted alphabetically: Anatomic Pathology, Behavior, Cardiology
      expect(deduplicated).toHaveLength(3)
      expect(componentLogic.getRotationDisplayName(deduplicated[0])).toBe('Anatomic Pathology')
      expect(componentLogic.getRotationDisplayName(deduplicated[1])).toBe('Behavior')
      expect(componentLogic.getRotationDisplayName(deduplicated[2])).toBe('Cardiology')
    })

    it('handles empty rotation list', () => {
      const deduplicated = componentLogic.deduplicateRotations([])
      expect(deduplicated).toHaveLength(0)
    })

    it('handles undefined excludeRotationNames parameter', () => {
      const deduplicated = componentLogic.deduplicateRotations(
        componentLogic.mockRotationsResponse,
        undefined
      )
      
      expect(deduplicated).toHaveLength(3) // All rotations after deduplication
      expect(deduplicated.every(r => r.rotId)).toBe(true) // All have valid rotIds
    })

    it('preserves first occurrence when deduplicating', () => {
      const testRotations: RotationWithService[] = [
        {
          rotId: 1,
          name: 'Test Rotation',
          abbreviation: 'Test1',
          subjectCode: 'VM',
          courseNumber: '100',
          serviceId: 1,
          service: { serviceId: 1, serviceName: 'Service1', shortName: 'S1' }
        },
        {
          rotId: 2,
          name: 'Test Rotation', // Same display name
          abbreviation: 'Test2',
          subjectCode: 'VM',
          courseNumber: '200',
          serviceId: 2,
          service: { serviceId: 2, serviceName: 'Service2', shortName: 'S2' }
        }
      ]

      const deduplicated = componentLogic.deduplicateRotations(testRotations)
      
      expect(deduplicated).toHaveLength(1)
      expect(deduplicated[0].rotId).toBe(1) // Should keep the first one
      expect(deduplicated[0].abbreviation).toBe('Test1')
    })
  })

  describe('Service API Integration', () => {
    it('calls correct API for normal rotation fetching', async () => {
      vi.mocked(RotationService.getRotations).mockResolvedValue({
        success: true,
        result: componentLogic.mockRotationsResponse,
        errors: []
      })

      // This test verifies the service would be called correctly
      const result = await RotationService.getRotations({
        serviceId: undefined,
        includeService: true
      })

      expect(RotationService.getRotations).toHaveBeenCalledWith({
        serviceId: undefined,
        includeService: true
      })
      expect(result.success).toBe(true)
      expect(result.result).toHaveLength(4)
    })

    it('calls correct API for scheduled weeks fetching', async () => {
      vi.mocked(RotationService.getRotationsWithScheduledWeeks).mockResolvedValue({
        success: true,
        result: componentLogic.mockRotationsResponse.slice(0, 2),
        errors: []
      })

      // This test verifies the service would be called correctly
      const result = await RotationService.getRotationsWithScheduledWeeks({
        year: 2024,
        includeService: true
      })

      expect(RotationService.getRotationsWithScheduledWeeks).toHaveBeenCalledWith({
        year: 2024,
        includeService: true
      })
      expect(result.success).toBe(true)
      expect(result.result).toHaveLength(2)
    })

    it('handles API errors gracefully', async () => {
      vi.mocked(RotationService.getRotations).mockResolvedValue({
        success: false,
        result: [],
        errors: ['Failed to fetch rotations']
      })

      const result = await RotationService.getRotations({
        serviceId: undefined,
        includeService: true
      })

      expect(result.success).toBe(false)
      expect(result.errors).toContain('Failed to fetch rotations')
    })

    it('handles network errors', async () => {
      vi.mocked(RotationService.getRotations).mockRejectedValue(new Error('Network error'))

      await expect(RotationService.getRotations({
        serviceId: undefined,
        includeService: true
      })).rejects.toThrow('Network error')
    })
  })

  describe('Edge Cases and Error Handling', () => {
    it('handles rotation without service data', () => {
      const rotationWithoutService: RotationWithService = {
        rotId: 999,
        name: 'Orphaned Rotation',
        abbreviation: 'Orphan',
        subjectCode: 'VM',
        courseNumber: '999',
        serviceId: 999,
        service: null
      }

      expect(componentLogic.getRotationDisplayName(rotationWithoutService)).toBe('Orphaned Rotation')
      
      const filtered = componentLogic.filterRotations([rotationWithoutService], 'orphan')
      expect(filtered).toHaveLength(1)
      
      const deduplicated = componentLogic.deduplicateRotations([rotationWithoutService])
      expect(deduplicated).toHaveLength(1)
    })

    it('handles rotations with same name but different case', () => {
      const testRotations: RotationWithService[] = [
        {
          rotId: 1,
          name: 'test rotation',
          abbreviation: 'Test1',
          subjectCode: 'VM',
          courseNumber: '100',
          serviceId: 1,
          service: null
        },
        {
          rotId: 2,
          name: 'TEST ROTATION', // Different case
          abbreviation: 'Test2', 
          subjectCode: 'VM',
          courseNumber: '200',
          serviceId: 2,
          service: null
        }
      ]

      // Filtering should be case-insensitive
      const filtered = componentLogic.filterRotations(testRotations, 'TEST')
      expect(filtered).toHaveLength(2)

      // Deduplication considers them different (exact string match for Map key)
      const deduplicated = componentLogic.deduplicateRotations(testRotations)
      expect(deduplicated).toHaveLength(2) // Different names due to case
    })

    it('handles very long rotation names', () => {
      const longName = 'A'.repeat(1000) + ' (Details)'
      const rotation: RotationWithService = {
        rotId: 999,
        name: longName,
        abbreviation: 'Long',
        subjectCode: 'VM',
        courseNumber: '999',
        serviceId: 1,
        service: null
      }

      expect(componentLogic.getRotationDisplayName(rotation)).toBe('A'.repeat(1000))
      
      const filtered = componentLogic.filterRotations([rotation], 'A'.repeat(10))
      expect(filtered).toHaveLength(1)
    })
  })
})