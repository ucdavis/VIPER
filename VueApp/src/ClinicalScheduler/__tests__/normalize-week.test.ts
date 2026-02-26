import { normalizeWeek } from "../composables/use-schedule-normalization"

describe(normalizeWeek, () => {
    it("should preserve existing dateEnd when it has a value", () => {
        const week = {
            weekId: 1,
            dateEnd: "2024-01-07",
            someOtherProp: "value",
        }

        const result = normalizeWeek(week)

        expect(result).toEqual({
            weekId: 1,
            dateEnd: "2024-01-07",
            someOtherProp: "value",
        })
    })

    it("should convert null dateEnd to empty string", () => {
        const week = {
            weekId: 1,
            dateEnd: null,
            someOtherProp: "value",
        }

        const result = normalizeWeek(week)

        expect(result).toEqual({
            weekId: 1,
            dateEnd: "",
            someOtherProp: "value",
        })
    })

    it("should convert missing dateEnd to empty string", () => {
        const week = {
            weekId: 1,
            someOtherProp: "value",
        }

        const result = normalizeWeek(week)

        expect(result).toEqual({
            weekId: 1,
            dateEnd: "",
            someOtherProp: "value",
        })
    })

    it("should handle empty string dateEnd", () => {
        const week = {
            weekId: 1,
            dateEnd: "",
            someOtherProp: "value",
        }

        const result = normalizeWeek(week)

        expect(result).toEqual({
            weekId: 1,
            dateEnd: "",
            someOtherProp: "value",
        })
    })

    it("should preserve all other properties", () => {
        const week = {
            weekId: 1,
            dateEnd: null,
            startDate: "2024-01-01",
            rotations: [],
            instructors: ["Dr. Smith"],
            isActive: true,
        }

        const result = normalizeWeek(week)

        expect(result).toEqual({
            weekId: 1,
            dateEnd: "",
            startDate: "2024-01-01",
            rotations: [],
            instructors: ["Dr. Smith"],
            isActive: true,
        })
    })
})
