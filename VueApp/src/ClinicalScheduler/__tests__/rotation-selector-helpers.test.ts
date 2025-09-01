import { describe, it, expect } from "vitest"
import type { RotationWithService } from "../types/rotation-types"

// Helper function for rotation display name
const getRotationDisplayName = (rotation: RotationWithService): string => {
    const beforeParenthesis = rotation.name.split("(")[0].trim()
    return beforeParenthesis || rotation.name
}

// Helper function to test the core logic methods
const createComponentLogic = () => {
    // Mock rotation data for testing
    const mockRotationsResponse: RotationWithService[] = [
        {
            rotId: 101,
            name: "Anatomic Pathology (Advanced)",
            abbreviation: "AnatPath",
            subjectCode: "VM",
            courseNumber: "456",
            serviceId: 1,
            service: {
                serviceId: 1,
                serviceName: "Anatomic Pathology",
                shortName: "AP",
            },
        },
        {
            rotId: 102,
            name: "Cardiology",
            abbreviation: "Card",
            subjectCode: "VM",
            courseNumber: "789",
            serviceId: 2,
            service: {
                serviceId: 2,
                serviceName: "Cardiology",
                shortName: "CARD",
            },
        },
        {
            rotId: 103,
            name: "Behavior",
            abbreviation: "Beh",
            subjectCode: "VM",
            courseNumber: "123",
            serviceId: 3,
            service: {
                serviceId: 3,
                serviceName: "Behavior",
                shortName: "BEH",
            },
        },
        {
            rotId: 104,
            name: "Anatomic Pathology (Basic)",
            abbreviation: "AnatPath2",
            subjectCode: "VM",
            courseNumber: "455",
            serviceId: 1,
            service: {
                serviceId: 1,
                serviceName: "Anatomic Pathology",
                shortName: "AP",
            },
        },
    ]

    // Extract the core logic functions that we want to test
    const filterRotations = (items: RotationWithService[], searchTerm: string): RotationWithService[] => {
        const search = searchTerm.toLowerCase()
        return items.filter(
            (rotation) =>
                getRotationDisplayName(rotation).toLowerCase().includes(search) ||
                rotation.abbreviation?.toLowerCase().includes(search) ||
                rotation.service?.serviceName?.toLowerCase().includes(search),
        )
    }

    const deduplicateRotations = (
        rotations: RotationWithService[],
        excludeRotationNames?: string[],
    ): RotationWithService[] => {
        const uniqueRotations = new Map<string, RotationWithService>()

        for (const rotation of rotations) {
            const rotationName = getRotationDisplayName(rotation)
            // Skip if this rotation name is in the exclusion list
            if (
                (!excludeRotationNames || !excludeRotationNames.includes(rotationName)) &&
                !uniqueRotations.has(rotationName)
            ) {
                uniqueRotations.set(rotationName, rotation)
            }
        }

        return [...uniqueRotations.values()].sort((a, b) =>
            getRotationDisplayName(a).localeCompare(getRotationDisplayName(b)),
        )
    }

    return {
        mockRotationsResponse,
        getRotationDisplayName,
        filterRotations,
        deduplicateRotations,
    }
}

describe("RotationSelector Helper Functions", () => {
    const componentLogic = createComponentLogic()

    describe("Rotation Display Name Formatting", () => {
        it("formats rotation names correctly by removing text after parentheses", () => {
            const [rotation] = componentLogic.mockRotationsResponse // 'Anatomic Pathology (Advanced)'
            expect(componentLogic.getRotationDisplayName(rotation)).toBe("Anatomic Pathology")
        })

        it("handles rotation names without parentheses", () => {
            const [, rotation] = componentLogic.mockRotationsResponse // 'Cardiology'
            expect(componentLogic.getRotationDisplayName(rotation)).toBe("Cardiology")
        })

        it("handles empty string after parentheses removal", () => {
            const rotation: RotationWithService = {
                rotId: 999,
                name: "(Test)",
                abbreviation: "Test",
                subjectCode: "VM",
                courseNumber: "999",
                serviceId: 1,
                service: null,
            }
            expect(componentLogic.getRotationDisplayName(rotation)).toBe("(Test)")
        })

        it("trims whitespace from rotation names", () => {
            const rotation: RotationWithService = {
                rotId: 999,
                name: "   Spaced Name   (Details)",
                abbreviation: "Space",
                subjectCode: "VM",
                courseNumber: "999",
                serviceId: 1,
                service: null,
            }
            expect(componentLogic.getRotationDisplayName(rotation)).toBe("Spaced Name")
        })
    })

    describe("Search and Filtering", () => {
        it("filters rotations by name", () => {
            const filtered = componentLogic.filterRotations(componentLogic.mockRotationsResponse, "anatomic")

            expect(filtered).toHaveLength(2) // Both anatomic pathology rotations
            expect(
                filtered.every((r) => componentLogic.getRotationDisplayName(r).toLowerCase().includes("anatomic")),
            ).toBeTruthy()
        })

        it("filters rotations by abbreviation", () => {
            const filtered = componentLogic.filterRotations(componentLogic.mockRotationsResponse, "card")

            expect(filtered).toHaveLength(1)
            expect(filtered[0].abbreviation).toBe("Card")
        })

        it("filters rotations by service name", () => {
            const filtered = componentLogic.filterRotations(componentLogic.mockRotationsResponse, "cardiology")

            expect(filtered).toHaveLength(1)
            expect(filtered[0].service?.serviceName).toBe("Cardiology")
        })

        it("is case insensitive when filtering", () => {
            const filtered = componentLogic.filterRotations(componentLogic.mockRotationsResponse, "BEHAVIOR")

            expect(filtered).toHaveLength(1)
            expect(componentLogic.getRotationDisplayName(filtered[0])).toBe("Behavior")
        })

        it("returns empty array when no matches found", () => {
            const filtered = componentLogic.filterRotations(componentLogic.mockRotationsResponse, "nonexistent")

            expect(filtered).toHaveLength(0)
        })
    })
})

export { createComponentLogic, getRotationDisplayName }
