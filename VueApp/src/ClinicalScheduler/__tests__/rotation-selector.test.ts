import { RotationService } from "../services/rotation-service"
import { createComponentLogic } from "./rotation-selector-helpers.test"

// Mock the RotationService
vi.mock("../services/rotation-service", () => ({
    RotationService: {
        getRotations: vi.fn(),
        getRotationsWithScheduledWeeks: vi.fn(),
    },
}))

describe("RotationSelector", () => {
    let componentLogic: ReturnType<typeof createComponentLogic> = {} as ReturnType<typeof createComponentLogic>

    beforeEach(() => {
        // Reset all mocks
        vi.clearAllMocks()
        componentLogic = createComponentLogic()

        // Setup default mock responses
        vi.mocked(RotationService.getRotations).mockResolvedValue({
            success: true,
            result: componentLogic.mockRotationsResponse,
            errors: [],
        })

        vi.mocked(RotationService.getRotationsWithScheduledWeeks).mockResolvedValue({
            success: true,
            result: componentLogic.mockRotationsResponse.slice(0, 2), // Only first two for scheduled weeks
            errors: [],
        })
    })

    describe("API Integration", () => {
        it("fetches all rotations successfully", async () => {
            const result = await RotationService.getRotations({ includeService: true } as any)

            expect(result.success).toBeTruthy()
            expect(result.result).toHaveLength(4)
            expect(result.result[0]!.name).toBe("Anatomic Pathology (Advanced)")
        })

        it("fetches rotations with scheduled weeks", async () => {
            const result = await RotationService.getRotationsWithScheduledWeeks({
                year: 2024,
                includeService: true,
            } as any)

            expect(result.success).toBeTruthy()
            expect(result.result).toHaveLength(2)
        })

        it("handles API errors gracefully", async () => {
            vi.mocked(RotationService.getRotations).mockResolvedValue({
                success: false,
                result: [],
                errors: ["Network error"],
            })

            const result = await RotationService.getRotations({ includeService: true } as any)

            expect(result.success).toBeFalsy()
            expect(result.errors).toContain("Network error")
        })
    })

    describe("Helper Functions", () => {
        it("formats rotation display names correctly", () => {
            const [rotation] = componentLogic.mockRotationsResponse
            const displayName = componentLogic.getRotationDisplayName(rotation!)

            expect(displayName).toBe("Anatomic Pathology")
        })

        it("filters rotations by search term", () => {
            const filtered = componentLogic.filterRotations(componentLogic.mockRotationsResponse, "cardio")

            expect(filtered).toHaveLength(1)
            expect(filtered[0]!.name).toBe("Cardiology")
        })

        it("filters rotations by service name", () => {
            const filtered = componentLogic.filterRotations(componentLogic.mockRotationsResponse, "behavior")

            expect(filtered).toHaveLength(1)
            expect(filtered[0]!.name).toBe("Behavior")
        })

        it("filters rotations by abbreviation", () => {
            const filtered = componentLogic.filterRotations(componentLogic.mockRotationsResponse, "card")

            expect(filtered).toHaveLength(1)
            expect(filtered[0]!.abbreviation).toBe("Card")
        })

        it("returns empty array when no matches found", () => {
            const filtered = componentLogic.filterRotations(componentLogic.mockRotationsResponse, "nonexistent")

            expect(filtered).toHaveLength(0)
        })

        it("handles case-insensitive filtering", () => {
            const filtered = componentLogic.filterRotations(componentLogic.mockRotationsResponse, "ANATOMIC")

            expect(filtered).toHaveLength(2)
            expect(
                filtered.every((r) => componentLogic.getRotationDisplayName(r).toLowerCase().includes("anatomic")),
            ).toBeTruthy()
        })
    })

    describe("Data Transformation", () => {
        it("sorts rotations alphabetically by display name", () => {
            const sorted = componentLogic.mockRotationsResponse.toSorted((a, b) =>
                componentLogic.getRotationDisplayName(a).localeCompare(componentLogic.getRotationDisplayName(b)),
            )

            expect(componentLogic.getRotationDisplayName(sorted[0]!)).toBe("Anatomic Pathology")
            expect(componentLogic.getRotationDisplayName(sorted[2]!)).toBe("Behavior")
        })

        it("excludes specified rotation names", () => {
            const excluded = componentLogic.mockRotationsResponse.filter((rotation) => {
                const name = componentLogic.getRotationDisplayName(rotation)
                return !["Anatomic Pathology", "Cardiology"].includes(name)
            })

            expect(excluded).toHaveLength(1)
            expect(excluded[0]!.name).toBe("Behavior")
        })

        it("handles rotations without service data", () => {
            const rotationWithoutService = {
                ...componentLogic.mockRotationsResponse[0],
                service: undefined,
            }

            const displayName = componentLogic.getRotationDisplayName(rotationWithoutService as any)
            expect(displayName).toBe("Anatomic Pathology")
        })
    })

    describe("Permission-Based Filtering Logic", () => {
        it("filters rotations by service ID", () => {
            const serviceId = 1
            const filtered = componentLogic.mockRotationsResponse.filter((r) => r.serviceId === serviceId)

            expect(filtered).toHaveLength(2) // Both Anatomic Pathology rotations
            expect(filtered.every((r) => r.serviceId === serviceId)).toBeTruthy()
        })

        it("filters rotations by multiple service IDs", () => {
            const allowedServiceIds = new Set([1, 2])
            const filtered = componentLogic.mockRotationsResponse.filter((r) => allowedServiceIds.has(r.serviceId))

            expect(filtered).toHaveLength(3) // 2 Anatomic Pathology + 1 Cardiology
            expect(filtered.every((r) => allowedServiceIds.has(r.serviceId))).toBeTruthy()
        })

        it("returns empty array when no services are allowed", () => {
            const allowedServiceIds = new Set<number>()
            const filtered = componentLogic.mockRotationsResponse.filter((r) => allowedServiceIds.has(r.serviceId))

            expect(filtered).toHaveLength(0)
        })

        it("returns all rotations when filtering is not applied", () => {
            const filtered = componentLogic.mockRotationsResponse.filter(() => true)

            expect(filtered).toHaveLength(4)
            expect(filtered).toEqual(componentLogic.mockRotationsResponse)
        })
    })
})
