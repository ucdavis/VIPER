import { describe, it, expect } from "vitest"
import type { RotationWithService } from "../types/rotation-types"
import { createComponentLogic } from "./rotation-selector-helpers.test"

describe("RotationSelector - Basic Edge Cases", () => {
    const componentLogic = createComponentLogic()

    it("handles rotation without service data", () => {
        const rotationWithoutService: RotationWithService = {
            rotId: 999,
            name: "Orphaned Rotation",
            abbreviation: "Orphan",
            subjectCode: "VM",
            courseNumber: "999",
            serviceId: 999,
            service: undefined,
        }

        expect(componentLogic.getRotationDisplayName(rotationWithoutService)).toBe("Orphaned Rotation")

        const filtered = componentLogic.filterRotations([rotationWithoutService], "orphan")
        expect(filtered).toHaveLength(1)
    })

    it("handles rotations with same name but different case", () => {
        const testRotations: RotationWithService[] = [
            {
                rotId: 1,
                name: "test rotation",
                abbreviation: "Test1",
                subjectCode: "VM",
                courseNumber: "100",
                serviceId: 1,
                service: undefined,
            },
            {
                rotId: 2,
                name: "TEST ROTATION", // Different case
                abbreviation: "Test2",
                subjectCode: "VM",
                courseNumber: "200",
                serviceId: 2,
                service: undefined,
            },
        ]

        // Filtering should be case-insensitive
        const filtered = componentLogic.filterRotations(testRotations, "TEST")
        expect(filtered).toHaveLength(2)
    })

    it("handles very long rotation names", () => {
        const longName = `${"A".repeat(1000)} (Details)`
        const rotation: RotationWithService = {
            rotId: 999,
            name: longName,
            abbreviation: "Long",
            subjectCode: "VM",
            courseNumber: "999",
            serviceId: 1,
            service: undefined,
        }

        expect(componentLogic.getRotationDisplayName(rotation)).toBe("A".repeat(1000))

        const filtered = componentLogic.filterRotations([rotation], "A".repeat(10))
        expect(filtered).toHaveLength(1)
    })
})

describe("RotationSelector - Null Handling", () => {
    const componentLogic = createComponentLogic()

    it("handles null service properties when filtering", () => {
        const rotationWithNullService: RotationWithService = {
            rotId: 999,
            name: "Null Service Rotation",
            abbreviation: "NSR",
            subjectCode: "VM",
            courseNumber: "999",
            serviceId: 999,
            service: undefined,
        }

        const rotationsWithNull = [rotationWithNullService]
        const filtered = componentLogic.filterRotations(rotationsWithNull, "service")

        // Should match by name containing "service"
        expect(filtered).toHaveLength(1)
        expect(filtered[0].name).toBe("Null Service Rotation")
    })

    it("handles undefined abbreviation when filtering", () => {
        const rotationWithoutAbbreviation: RotationWithService = {
            rotId: 999,
            name: "No Abbreviation",
            abbreviation: "",
            subjectCode: "VM",
            courseNumber: "999",
            serviceId: 1,
            service: { serviceId: 1, serviceName: "Test Service", shortName: "TEST" },
        }

        const rotationsWithUndefined = [rotationWithoutAbbreviation]
        const filtered = componentLogic.filterRotations(rotationsWithUndefined, "abbreviation")

        // Should match by name containing "abbreviation"
        expect(filtered).toHaveLength(1)
        expect(filtered[0].name).toBe("No Abbreviation")
    })
})
