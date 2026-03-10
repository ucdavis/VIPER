import { describe, it, expect } from "vitest"
import { ref } from "vue"
import { useEffortTypeColumns, ALWAYS_SHOW } from "../composables/use-effort-type-columns"

describe("useEffortTypeColumns", () => {
    describe("effortColumns", () => {
        it("generates columns from effort type list", () => {
            const effortTypes = ref(["CLI", "LEC", "LAB"])

            const { effortColumns } = useEffortTypeColumns(effortTypes)

            expect(effortColumns.value).toHaveLength(3)
            expect(effortColumns.value[0].label).toBe("CLI")
            expect(effortColumns.value[1].label).toBe("LEC")
            expect(effortColumns.value[2].label).toBe("LAB")
        })

        it("generates unique column names with effort_ prefix", () => {
            const effortTypes = ref(["CLI", "LEC"])

            const { effortColumns } = useEffortTypeColumns(effortTypes)

            expect(effortColumns.value[0].name).toBe("effort_CLI")
            expect(effortColumns.value[1].name).toBe("effort_LEC")
        })

        it("columns are right-aligned and not sortable", () => {
            const effortTypes = ref(["CLI"])

            const { effortColumns } = useEffortTypeColumns(effortTypes)

            expect(effortColumns.value[0].align).toBe("right")
            expect(effortColumns.value[0].sortable).toBe(false)
        })

        it("returns empty array when no effort types", () => {
            const effortTypes = ref<string[]>([])

            const { effortColumns } = useEffortTypeColumns(effortTypes)

            expect(effortColumns.value).toHaveLength(0)
        })

        it("updates reactively when effort types change", () => {
            const effortTypes = ref(["CLI"])

            const { effortColumns } = useEffortTypeColumns(effortTypes)
            expect(effortColumns.value).toHaveLength(1)

            effortTypes.value = ["CLI", "LEC", "LAB"]
            expect(effortColumns.value).toHaveLength(3)
        })

        it("field accessor extracts correct effort type value from row", () => {
            const effortTypes = ref(["LEC", "CLI"])

            const { effortColumns } = useEffortTypeColumns(effortTypes)

            const row = { effortByType: { LEC: 30, CLI: 10 } }
            const lecField = effortColumns.value[0].field
            const cliField = effortColumns.value[1].field

            // field is a function that extracts the value from the row
            expect(typeof lecField).toBe("function")
            expect((lecField as (row: typeof row) => number)(row)).toBe(30)
            expect((cliField as (row: typeof row) => number)(row)).toBe(10)
        })

        it("field accessor returns 0 for missing effort type", () => {
            const effortTypes = ref(["LEC"])

            const { effortColumns } = useEffortTypeColumns(effortTypes)

            const row = { effortByType: { CLI: 10 } }
            const lecField = effortColumns.value[0].field
            expect((lecField as (row: typeof row) => number)(row)).toBe(0)
        })

        it("field accessor returns 0 when effortByType is undefined", () => {
            const effortTypes = ref(["LEC"])

            const { effortColumns } = useEffortTypeColumns(effortTypes)

            const row = {}
            const lecField = effortColumns.value[0].field
            expect((lecField as (row: typeof row) => number)(row)).toBe(0)
        })

        it("format function shows value when > 0", () => {
            const effortTypes = ref(["LEC"])

            const { effortColumns } = useEffortTypeColumns(effortTypes)

            const formatFn = effortColumns.value[0].format!
            expect(formatFn(30, null as never)).toBe("30")
            expect(formatFn(0.5, null as never)).toBe("0.5")
        })

        it("format function returns empty string for zero", () => {
            const effortTypes = ref(["LEC"])

            const { effortColumns } = useEffortTypeColumns(effortTypes)

            const formatFn = effortColumns.value[0].format!
            expect(formatFn(0, null as never)).toBe("")
        })
    })

    describe("getTotalValue", () => {
        it("returns string value for non-zero total", () => {
            const effortTypes = ref(["LEC"])

            const { getTotalValue } = useEffortTypeColumns(effortTypes)

            const totals = { LEC: 30, CLI: 10 }
            expect(getTotalValue(totals, "LEC")).toBe("30")
            expect(getTotalValue(totals, "CLI")).toBe("10")
        })

        it("returns empty string for zero total", () => {
            const effortTypes = ref(["LEC"])

            const { getTotalValue } = useEffortTypeColumns(effortTypes)

            const totals = { LEC: 0 }
            expect(getTotalValue(totals, "LEC")).toBe("")
        })

        it("returns empty string for missing effort type", () => {
            const effortTypes = ref(["LEC"])

            const { getTotalValue } = useEffortTypeColumns(effortTypes)

            const totals = { CLI: 10 }
            expect(getTotalValue(totals, "LEC")).toBe("")
        })

        it("returns empty string for empty totals object", () => {
            const effortTypes = ref(["LEC"])

            const { getTotalValue } = useEffortTypeColumns(effortTypes)

            expect(getTotalValue({}, "LEC")).toBe("")
        })
    })

    describe("with showZero option", () => {
        it("format returns '0' for zero values", () => {
            const effortTypes = ref(["LEC"])

            const { effortColumns } = useEffortTypeColumns(effortTypes, { showZero: true })

            const formatFn = effortColumns.value[0].format!
            expect(formatFn(0, null as never)).toBe("0")
        })

        it("format still returns string value for non-zero values", () => {
            const effortTypes = ref(["LEC"])

            const { effortColumns } = useEffortTypeColumns(effortTypes, { showZero: true })

            const formatFn = effortColumns.value[0].format!
            expect(formatFn(30, null as never)).toBe("30")
            expect(formatFn(0.5, null as never)).toBe("0.5")
        })

        it("getTotalValue returns '0' for zero totals", () => {
            const effortTypes = ref(["LEC"])

            const { getTotalValue } = useEffortTypeColumns(effortTypes, { showZero: true })

            const totals = { LEC: 0 }
            expect(getTotalValue(totals, "LEC")).toBe("0")
        })

        it("getTotalValue returns '0' for missing effort type", () => {
            const effortTypes = ref(["LEC"])

            const { getTotalValue } = useEffortTypeColumns(effortTypes, { showZero: true })

            expect(getTotalValue({}, "LEC")).toBe("0")
        })

        it("getTotalValue returns string value for non-zero totals", () => {
            const effortTypes = ref(["LEC"])

            const { getTotalValue } = useEffortTypeColumns(effortTypes, { showZero: true })

            const totals = { LEC: 30 }
            expect(getTotalValue(totals, "LEC")).toBe("30")
        })
    })

    describe("with legacyColumnOrder option", () => {
        it("ALWAYS_SHOW types appear first in defined order", () => {
            const effortTypes = ref(["LAB", "CLI", "LEC"])

            const { effortColumns } = useEffortTypeColumns(effortTypes, { legacyColumnOrder: true })

            const labels = effortColumns.value.map((c) => c.label)
            // ALWAYS_SHOW order: CLI, VAR, LEC, LAB, DIS, PBL, CBL, TBL, PRS, JLC, EXM
            // Input has CLI, LAB, LEC — so among ALWAYS_SHOW those appear in ALWAYS_SHOW order
            // Plus all ALWAYS_SHOW types are included even if not in input
            const cliIdx = labels.indexOf("CLI")
            const lecIdx = labels.indexOf("LEC")
            const labIdx = labels.indexOf("LAB")
            expect(cliIdx).toBeLessThan(lecIdx)
            expect(lecIdx).toBeLessThan(labIdx)
        })

        it("ALWAYS_SHOW types appear even if not in input effortTypes", () => {
            const effortTypes = ref(["CLI", "LEC"])

            const { effortColumns } = useEffortTypeColumns(effortTypes, { legacyColumnOrder: true })

            const labels = effortColumns.value.map((c) => c.label)
            // All 11 ALWAYS_SHOW types should be present
            for (const type of ALWAYS_SHOW) {
                expect(labels).toContain(type)
            }
            expect(effortColumns.value).toHaveLength(ALWAYS_SHOW.length)
        })

        it("remaining types with data come after ALWAYS_SHOW types, alphabetically", () => {
            const effortTypes = ref(["CLI", "LEC", "SEM", "FLD", "LAB"])

            const { effortColumns } = useEffortTypeColumns(effortTypes, { legacyColumnOrder: true })

            const labels = effortColumns.value.map((c) => c.label)
            // ALWAYS_SHOW types come first (11), then FLD, SEM (alphabetical)
            const exmIdx = labels.indexOf("EXM") // last ALWAYS_SHOW
            const fldIdx = labels.indexOf("FLD")
            const semIdx = labels.indexOf("SEM")
            expect(fldIdx).toBeGreaterThan(exmIdx)
            expect(semIdx).toBeGreaterThan(fldIdx)
        })

        it("VAR and EXM columns have paddingRight style", () => {
            const effortTypes = ref(["CLI", "VAR", "EXM", "LEC"])

            const { effortColumns } = useEffortTypeColumns(effortTypes, { legacyColumnOrder: true })

            const varCol = effortColumns.value.find((c) => c.label === "VAR")
            const exmCol = effortColumns.value.find((c) => c.label === "EXM")
            const cliCol = effortColumns.value.find((c) => c.label === "CLI")

            expect(varCol?.style).toContain("padding-right: 20px")
            expect(exmCol?.style).toContain("padding-right: 20px")
            expect(cliCol?.style).not.toContain("padding-right: 20px")
        })

        it("non-ALWAYS_SHOW columns do not have extra padding", () => {
            const effortTypes = ref(["CLI", "SEM"])

            const { effortColumns } = useEffortTypeColumns(effortTypes, { legacyColumnOrder: true })

            const semCol = effortColumns.value.find((c) => c.label === "SEM")
            expect(semCol?.style).not.toContain("padding-right: 20px")
        })

        it("works with both legacyColumnOrder and showZero", () => {
            const effortTypes = ref(["CLI", "LEC"])

            const { effortColumns, getTotalValue } = useEffortTypeColumns(effortTypes, {
                legacyColumnOrder: true,
                showZero: true,
            })

            // Should have all ALWAYS_SHOW columns
            expect(effortColumns.value.length).toBeGreaterThanOrEqual(ALWAYS_SHOW.length)

            // format should show "0" for zero
            const formatFn = effortColumns.value[0].format!
            expect(formatFn(0, null as never)).toBe("0")

            // getTotalValue should show "0" for zero
            expect(getTotalValue({}, "CLI")).toBe("0")
        })
    })
})
