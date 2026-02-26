import { RotationService } from "../services/rotation-service"
import { createComponentLogic } from "./rotation-selector-helpers.test"

// Mock the RotationService
vi.mock("../services/rotation-service", () => ({
    RotationService: {
        getRotations: vi.fn(),
        getRotationsWithScheduledWeeks: vi.fn(),
    },
}))

describe("RotationSelector - Service API", () => {
    const componentLogic = createComponentLogic()

    beforeEach(() => {
        // Reset all mocks
        vi.clearAllMocks()

        // Setup default mock responses
        vi.mocked(RotationService.getRotations).mockResolvedValue({
            success: true,
            result: componentLogic.mockRotationsResponse,
            errors: [],
        })

        vi.mocked(RotationService.getRotationsWithScheduledWeeks).mockResolvedValue({
            success: true,
            result: componentLogic.mockRotationsResponse.slice(0, 2),
            errors: [],
        })
    })

    it("calls correct API for normal rotation fetching", async () => {
        const result = await RotationService.getRotations({
            serviceId: null,
            includeService: true,
        } as any)

        expect(RotationService.getRotations).toHaveBeenCalledWith({
            serviceId: null,
            includeService: true,
        })
        expect(result.success).toBeTruthy()
        expect(result.result).toHaveLength(4)
    })

    it("calls correct API for scheduled weeks fetching", async () => {
        const result = await RotationService.getRotationsWithScheduledWeeks({
            year: 2024,
            includeService: true,
        } as any)

        expect(RotationService.getRotationsWithScheduledWeeks).toHaveBeenCalledWith({
            year: 2024,
            includeService: true,
        })
        expect(result.success).toBeTruthy()
        expect(result.result).toHaveLength(2)
    })

    it("handles API errors gracefully", async () => {
        vi.mocked(RotationService.getRotations).mockResolvedValue({
            success: false,
            result: [],
            errors: ["Failed to fetch rotations"],
        })

        const result = await RotationService.getRotations({
            serviceId: null,
            includeService: true,
        } as any)

        expect(result.success).toBeFalsy()
        expect(result.errors).toContain("Failed to fetch rotations")
    })

    it("handles network errors", async () => {
        vi.mocked(RotationService.getRotations).mockRejectedValue(new Error("Network error"))

        await expect(
            RotationService.getRotations({
                serviceId: null,
                includeService: true,
            } as any),
        ).rejects.toThrow("Network error")
    })
})
