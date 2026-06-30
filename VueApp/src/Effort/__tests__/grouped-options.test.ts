import { filterGroupedOptions } from "../utils/grouped-options"

/**
 * Tests for filterGroupedOptions: type-ahead filtering of grouped select options.
 * Covers the empty-search contract (return everything) and header pruning.
 */

type Option = { label: string; isHeader?: boolean }

// The trailing "Empty Section" header has no items beneath it.
const options: Option[] = [
    { label: "Fruits", isHeader: true },
    { label: "Apple" },
    { label: "Apricot" },
    { label: "Vegetables", isHeader: true },
    { label: "Carrot" },
    { label: "Empty Section", isHeader: true },
]

describe("filterGroupedOptions util", () => {
    it("returns every option (including an empty-section header) for an empty search", () => {
        expect.hasAssertions()
        expect(filterGroupedOptions(options, "")).toStrictEqual(options)
    })

    it("treats a whitespace-only search as empty and returns every option", () => {
        expect.hasAssertions()
        expect(filterGroupedOptions(options, "   ")).toStrictEqual(options)
    })

    it("keeps matching items with their headers and drops headers left empty", () => {
        expect.hasAssertions()
        const result = filterGroupedOptions(options, "ap")
        expect(result.map((o) => o.label)).toStrictEqual(["Fruits", "Apple", "Apricot"])
    })

    it("is case-insensitive", () => {
        expect.hasAssertions()
        const result = filterGroupedOptions(options, "CARROT")
        expect(result.map((o) => o.label)).toStrictEqual(["Vegetables", "Carrot"])
    })

    it("drops all headers when nothing matches", () => {
        expect.hasAssertions()
        expect(filterGroupedOptions(options, "zzz")).toStrictEqual([])
    })
})
