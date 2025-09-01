import { describe, it, expect, vi, beforeEach } from "vitest"
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
    let componentLogic: ReturnType<typeof createComponentLogic> = createComponentLogic()

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

    describe("Deduplication Logic", () => {
        it("deduplicates rotations by display name", () => {
            // Add duplicate rotation names to test deduplication
            const duplicateRotations = [
                ...componentLogic.mockRotationsResponse,
                {
                    rotId: 105,
                    name: "Anatomic Pathology (Duplicate)",
                    abbreviation: "AnatPath3",
                    subjectCode: "VM",
                    courseNumber: "457",
                    serviceId: 1,
                    service: {
                        serviceId: 1,
                        serviceName: "Anatomic Pathology",
                        shortName: "AP",
                    },
                },
            ]

            const deduplicated = componentLogic.deduplicateRotations(duplicateRotations)

            // Should deduplicate 'Anatomic Pathology' entries (keeps first occurrence)
            const anatomicPathRotations = deduplicated.filter(
                (r) => componentLogic.getRotationDisplayName(r) === "Anatomic Pathology",
            )
            expect(anatomicPathRotations).toHaveLength(1)
            expect(anatomicPathRotations[0].rotId).toBe(101) // Should keep the first one
        })

        it("excludes rotation names from excludeRotationNames parameter", () => {
            const deduplicated = componentLogic.deduplicateRotations(componentLogic.mockRotationsResponse, [
                "Anatomic Pathology",
                "Cardiology",
            ])

            // Should only have Behavior left after exclusions
            expect(deduplicated).toHaveLength(1)
            expect(deduplicated[0].name).toBe("Behavior")
        })

        it("sorts deduplicated rotations alphabetically by display name", () => {
            const deduplicated = componentLogic.deduplicateRotations(componentLogic.mockRotationsResponse)

            // Should be sorted alphabetically: Anatomic Pathology, Behavior, Cardiology
            expect(deduplicated).toHaveLength(3)
            expect(componentLogic.getRotationDisplayName(deduplicated[0])).toBe("Anatomic Pathology")
            expect(componentLogic.getRotationDisplayName(deduplicated[1])).toBe("Behavior")
            expect(componentLogic.getRotationDisplayName(deduplicated[2])).toBe("Cardiology")
        })

        it("handles empty rotation list", () => {
            const deduplicated = componentLogic.deduplicateRotations([])
            expect(deduplicated).toHaveLength(0)
        })

        it("handles null excludeRotationNames parameter", () => {
            const deduplicated = componentLogic.deduplicateRotations(componentLogic.mockRotationsResponse)

            expect(deduplicated).toHaveLength(3) // All rotations after deduplication
            expect(deduplicated.every((r) => r.rotId)).toBeTruthy() // All have valid rotIds
        })
    })
})
