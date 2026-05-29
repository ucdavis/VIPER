/**
 * Tests for EffortRecordsTable component logic.
 *
 * These tests validate the effort type legend generation and
 * zero-effort styling logic.
 */

// Type for effort type legend items
interface EffortTypeLegendItem {
    code: string
    description: string
}

// Simplified record type for testing
interface MockRecord {
    id: number
    effortType: string
    effortTypeDescription: string
    effortValue: number | null
}

/**
 * Generates unique effort type legend items from records.
 * Extracted from EffortRecordsTable.vue uniqueEffortTypes computed.
 */
function getUniqueEffortTypes(records: MockRecord[]): EffortTypeLegendItem[] {
    const seen = new Map<string, string>()
    for (const record of records) {
        if (!seen.has(record.effortType)) {
            seen.set(record.effortType, record.effortTypeDescription)
        }
    }
    return [...seen.entries()]
        .map(([code, description]) => ({ code, description }))
        .toSorted((a, b) => a.code.localeCompare(b.code))
}

/**
 * Checks if a record has zero effort for styling.
 * Extracted from EffortRecordsTable.vue isZeroEffort function.
 */
function isZeroEffort(record: MockRecord, zeroEffortRecordIds: number[]): boolean {
    if (zeroEffortRecordIds.length > 0) {
        return zeroEffortRecordIds.includes(record.id)
    }
    return record.effortValue === 0
}

/**
 * Formats the effort display value with label.
 * Based on EffortRecordsTable.vue body-cell-effort template.
 */
function formatEffortDisplay(effortValue: number | null, effortLabel: string): string {
    const value = effortValue ?? 0
    const label = effortLabel === "weeks" ? "Weeks" : "Hours"
    return `${value} ${label}`
}

describe("EffortRecordsTable - Effort Type Legend", () => {
    describe("Unique effort types extraction", () => {
        it("extracts unique effort types from records", () => {
            const records: MockRecord[] = [
                { id: 1, effortType: "LEC", effortTypeDescription: "Lecture", effortValue: 40 },
                { id: 2, effortType: "LAB", effortTypeDescription: "Laboratory", effortValue: 20 },
                { id: 3, effortType: "LEC", effortTypeDescription: "Lecture", effortValue: 10 },
            ]

            const result = getUniqueEffortTypes(records)

            expect(result).toHaveLength(2)
            expect(result.map((r) => r.code)).toEqual(["LAB", "LEC"])
        })

        it("returns empty array for empty records", () => {
            expect(getUniqueEffortTypes([])).toHaveLength(0)
        })

        it("sorts effort types alphabetically by code", () => {
            const records: MockRecord[] = [
                { id: 1, effortType: "CLI", effortTypeDescription: "Clinical", effortValue: 5 },
                { id: 2, effortType: "ADM", effortTypeDescription: "Admin", effortValue: 10 },
                { id: 3, effortType: "LEC", effortTypeDescription: "Lecture", effortValue: 40 },
            ]

            const result = getUniqueEffortTypes(records)
            const codes = result.map((r) => r.code)

            expect(codes).toEqual(["ADM", "CLI", "LEC"])
        })

        it("preserves first description when duplicate codes found", () => {
            const records: MockRecord[] = [
                { id: 1, effortType: "LEC", effortTypeDescription: "Lecture", effortValue: 40 },
                { id: 2, effortType: "LEC", effortTypeDescription: "Different Description", effortValue: 20 },
            ]

            const result = getUniqueEffortTypes(records)

            expect(result).toHaveLength(1)
            expect(result[0]!.description).toBe("Lecture")
        })

        it("handles single record", () => {
            const records: MockRecord[] = [
                { id: 1, effortType: "CLI", effortTypeDescription: "Clinical", effortValue: 3 },
            ]

            const result = getUniqueEffortTypes(records)

            expect(result).toHaveLength(1)
            expect(result[0]).toEqual({ code: "CLI", description: "Clinical" })
        })

        it("handles many unique effort types", () => {
            const records: MockRecord[] = [
                { id: 1, effortType: "ADM", effortTypeDescription: "Admin", effortValue: 10 },
                { id: 2, effortType: "CLI", effortTypeDescription: "Clinical", effortValue: 5 },
                { id: 3, effortType: "LAB", effortTypeDescription: "Laboratory", effortValue: 20 },
                { id: 4, effortType: "LEC", effortTypeDescription: "Lecture", effortValue: 40 },
                { id: 5, effortType: "SEM", effortTypeDescription: "Seminar", effortValue: 8 },
            ]

            const result = getUniqueEffortTypes(records)

            expect(result).toHaveLength(5)
            expect(result.map((r) => r.code)).toEqual(["ADM", "CLI", "LAB", "LEC", "SEM"])
        })
    })
})

describe("EffortRecordsTable - Zero Effort Detection", () => {
    describe("Using zeroEffortRecordIds list", () => {
        it("returns true when record ID is in the zero effort list", () => {
            const record: MockRecord = { id: 5, effortType: "LEC", effortTypeDescription: "Lecture", effortValue: 40 }
            expect(isZeroEffort(record, [3, 5, 7])).toBeTruthy()
        })

        it("returns false when record ID is not in the list", () => {
            const record: MockRecord = { id: 5, effortType: "LEC", effortTypeDescription: "Lecture", effortValue: 0 }
            expect(isZeroEffort(record, [3, 7, 9])).toBeFalsy()
        })

        it("list takes precedence over effortValue", () => {
            const recordWithZeroValue: MockRecord = {
                id: 1,
                effortType: "LEC",
                effortTypeDescription: "Lecture",
                effortValue: 0,
            }
            const recordWithValue: MockRecord = {
                id: 2,
                effortType: "LEC",
                effortTypeDescription: "Lecture",
                effortValue: 40,
            }

            // Record 1 has zero value but is NOT in list - should be false (list takes precedence)
            expect(isZeroEffort(recordWithZeroValue, [2])).toBeFalsy()
            // Record 2 has value but IS in list - should be true
            expect(isZeroEffort(recordWithValue, [2])).toBeTruthy()
        })
    })

    describe("Fallback to effortValue when list is empty", () => {
        it("returns true when effortValue is 0", () => {
            const record: MockRecord = { id: 1, effortType: "LEC", effortTypeDescription: "Lecture", effortValue: 0 }
            expect(isZeroEffort(record, [])).toBeTruthy()
        })

        it("returns false when effortValue is positive", () => {
            const record: MockRecord = { id: 1, effortType: "LEC", effortTypeDescription: "Lecture", effortValue: 40 }
            expect(isZeroEffort(record, [])).toBeFalsy()
        })

        it("returns false when effortValue is null (null !== 0)", () => {
            const record: MockRecord = { id: 1, effortType: "LEC", effortTypeDescription: "Lecture", effortValue: null }
            expect(isZeroEffort(record, [])).toBeFalsy()
        })
    })
})

describe("EffortRecordsTable - Effort Display Formatting", () => {
    describe("Hours display", () => {
        it("formats positive hours correctly", () => {
            expect(formatEffortDisplay(40, "hours")).toBe("40 Hours")
        })

        it("formats zero hours correctly", () => {
            expect(formatEffortDisplay(0, "hours")).toBe("0 Hours")
        })

        it("formats null as 0 hours", () => {
            expect(formatEffortDisplay(null, "hours")).toBe("0 Hours")
        })
    })

    describe("Weeks display", () => {
        it("formats positive weeks correctly", () => {
            expect(formatEffortDisplay(5, "weeks")).toBe("5 Weeks")
        })

        it("formats zero weeks correctly", () => {
            expect(formatEffortDisplay(0, "weeks")).toBe("0 Weeks")
        })

        it("formats null as 0 weeks", () => {
            expect(formatEffortDisplay(null, "weeks")).toBe("0 Weeks")
        })
    })

    describe("Label normalization", () => {
        it("uses Hours for any label that is not 'weeks'", () => {
            expect(formatEffortDisplay(10, "hours")).toBe("10 Hours")
            expect(formatEffortDisplay(10, "")).toBe("10 Hours")
            expect(formatEffortDisplay(10, "other")).toBe("10 Hours")
        })

        it("only uses Weeks for exact 'weeks' label", () => {
            expect(formatEffortDisplay(10, "weeks")).toBe("10 Weeks")
            expect(formatEffortDisplay(10, "Weeks")).toBe("10 Hours") // Case sensitive
        })
    })
})

describe("EffortRecordsTable - Integration Scenarios", () => {
    it("generates correct legend for typical instructor records", () => {
        const records: MockRecord[] = [
            { id: 1, effortType: "LEC", effortTypeDescription: "Lecture", effortValue: 40 },
            { id: 2, effortType: "LAB", effortTypeDescription: "Laboratory", effortValue: 20 },
            { id: 3, effortType: "LEC", effortTypeDescription: "Lecture", effortValue: 15 },
            { id: 4, effortType: "CLI", effortTypeDescription: "Clinical", effortValue: 5 },
        ]

        const legend = getUniqueEffortTypes(records)

        expect(legend).toHaveLength(3)
        expect(legend[0]).toEqual({ code: "CLI", description: "Clinical" })
        expect(legend[1]).toEqual({ code: "LAB", description: "Laboratory" })
        expect(legend[2]).toEqual({ code: "LEC", description: "Lecture" })
    })

    it("correctly identifies zero-effort records in a list", () => {
        const records: MockRecord[] = [
            { id: 1, effortType: "LEC", effortTypeDescription: "Lecture", effortValue: 40 },
            { id: 2, effortType: "LAB", effortTypeDescription: "Laboratory", effortValue: 0 },
            { id: 3, effortType: "CLI", effortTypeDescription: "Clinical", effortValue: 5 },
        ]

        const zeroEffortIds = records.filter((r) => r.effortValue === 0).map((r) => r.id)

        expect(zeroEffortIds).toEqual([2])
        expect(isZeroEffort(records[0]!, zeroEffortIds)).toBeFalsy()
        expect(isZeroEffort(records[1]!, zeroEffortIds)).toBeTruthy()
        expect(isZeroEffort(records[2]!, zeroEffortIds)).toBeFalsy()
    })
})
