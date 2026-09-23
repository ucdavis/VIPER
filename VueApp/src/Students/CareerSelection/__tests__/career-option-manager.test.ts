import { careerSelectionService } from "../services/career-selection-service"
import { useCareerOptionManager, validateOptionLabel } from "../composables/use-career-option-manager"
import type { CareerSelectionOption } from "../types"

/**
 * Tests for managing career selection dropdown options: the client-side label check, the
 * service's option endpoints, and the list state the manage page is built on.
 */

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
const mockPost = vi.fn<(...args: unknown[]) => unknown>()
const mockPut = vi.fn<(...args: unknown[]) => unknown>()
const mockDel = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
        post: (...args: unknown[]) => mockPost(...args),
        put: (...args: unknown[]) => mockPut(...args),
        del: (...args: unknown[]) => mockDel(...args),
    }),
    postForBlob: vi.fn<(...args: unknown[]) => unknown>(),
    downloadBlob: vi.fn<(...args: unknown[]) => unknown>(),
}))

function option(id: number, label: string, overrides: Partial<CareerSelectionOption> = {}): CareerSelectionOption {
    return { id, label, isOther: false, usageCount: 0, ...overrides }
}

const EXISTING = [option(1, "Equine"), option(2, "Small Animal"), option(3, "Other", { isOther: true })]
const DUPLICATE = "An option with this name already exists."

function validate(label: string, editingId: number | null = null, maxLength = 100): string | null {
    return validateOptionLabel(label, { existing: EXISTING, editingId, maxLength })
}

describe("option label validation", () => {
    it("rejects a blank name", () => {
        expect.hasAssertions()
        expect(validate("   ")).toBe("Please enter a name.")
    })

    it("rejects a name over the maximum length once trimmed", () => {
        expect.hasAssertions()
        expect(validate("a".repeat(101))).toBe("Name must be 100 characters or fewer.")
        expect(validate(`  ${"a".repeat(100)}  `)).toBeNull()
    })

    it("rejects a duplicate that differs only in case or surrounding spaces", () => {
        expect.hasAssertions()
        expect(validate("  equine ")).toBe(DUPLICATE)
    })

    it("rejects a new option named after the catch-all", () => {
        expect.hasAssertions()
        expect(validate("other")).toBe(DUPLICATE)
    })

    it("allows an option to keep its own name or change only its case", () => {
        expect.hasAssertions()
        expect(validate("Equine", 1)).toBeNull()
        expect(validate("EQUINE", 1)).toBeNull()
    })

    it("rejects renaming an option to another option's name", () => {
        expect.hasAssertions()
        expect(validate("Small Animal", 1)).toBe(DUPLICATE)
    })

    it("accepts a new unique name", () => {
        expect.hasAssertions()
        expect(validate("Exotics")).toBeNull()
    })
})

describe("career selection service option endpoints", () => {
    it("maps each option type to its URL slug", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGet.mockResolvedValue({ success: true, result: [] })

        await careerSelectionService.getOptions("career")
        await careerSelectionService.getOptions("species")
        await careerSelectionService.getOptions("postGrad")

        expect(mockGet).toHaveBeenNthCalledWith(1, expect.stringMatching(/\/career-selection\/options\/career$/u))
        expect(mockGet).toHaveBeenNthCalledWith(2, expect.stringMatching(/\/career-selection\/options\/species$/u))
        expect(mockGet).toHaveBeenNthCalledWith(3, expect.stringMatching(/\/career-selection\/options\/post-grad$/u))
    })

    it("returns null when the options fail to load", async () => {
        expect.hasAssertions()
        mockGet.mockResolvedValue({ success: false, result: null, errors: ["Server error"] })
        await expect(careerSelectionService.getOptions("species")).resolves.toBeNull()
    })

    it("returns an empty list, not null, when there are no options", async () => {
        expect.hasAssertions()
        mockGet.mockResolvedValue({ success: true, result: [] })
        await expect(careerSelectionService.getOptions("species")).resolves.toStrictEqual([])
    })

    it("maps options to the form's dropdown shape", async () => {
        expect.hasAssertions()
        mockGet.mockResolvedValue({
            success: true,
            result: [option(1, "Equine"), option(3, "Other", { isOther: true })],
        })

        await expect(careerSelectionService.getDropdownOptions("species")).resolves.toStrictEqual([
            { label: "Equine", value: 1, isOther: false },
            { label: "Other", value: 3, isOther: true },
        ])
    })

    it("gives the form an empty dropdown when the options fail to load", async () => {
        expect.hasAssertions()
        mockGet.mockResolvedValue({ success: false, result: null, errors: ["Forbidden"] })
        await expect(careerSelectionService.getDropdownOptions("career")).resolves.toStrictEqual([])
    })

    it("posts a new option's label", async () => {
        expect.hasAssertions()
        mockPost.mockResolvedValue({ success: true, result: option(4, "Exotics"), errors: [] })

        const result = await careerSelectionService.createOption("species", "Exotics")

        expect(mockPost).toHaveBeenCalledWith(expect.stringMatching(/\/options\/species$/u), { label: "Exotics" })
        expect(result).toStrictEqual({ success: true, errors: [] })
    })

    it("puts a renamed option to its own URL", async () => {
        expect.hasAssertions()
        mockPut.mockResolvedValue({ success: true, result: option(1, "Horses"), errors: [] })

        await careerSelectionService.updateOption("species", 1, "Horses")

        expect(mockPut).toHaveBeenCalledWith(expect.stringMatching(/\/options\/species\/1$/u), { label: "Horses" })
    })

    it("passes a refused delete's errors through", async () => {
        expect.hasAssertions()
        mockDel.mockResolvedValue({ success: false, result: null, errors: ["Option is in use."] })

        const result = await careerSelectionService.deleteOption("career", 7)

        expect(mockDel).toHaveBeenCalledWith(expect.stringMatching(/\/options\/career\/7$/u))
        expect(result).toStrictEqual({ success: false, errors: ["Option is in use."] })
    })
})

describe("career option list state", () => {
    it("flags a failed load so the page can offer a retry", async () => {
        expect.hasAssertions()
        mockGet.mockResolvedValue({ success: false, result: null, errors: ["Server error"] })
        const manager = useCareerOptionManager("career")

        await manager.load()

        expect(manager.loadFailed.value).toBeTruthy()
        expect(manager.options.value).toStrictEqual([])
    })

    it("trims the label and reloads after a successful save", async () => {
        expect.hasAssertions()
        mockPost.mockResolvedValue({ success: true, result: option(4, "Exotics"), errors: [] })
        mockGet.mockResolvedValue({ success: true, result: [...EXISTING, option(4, "Exotics")] })
        const manager = useCareerOptionManager("species")

        const result = await manager.save(null, "  Exotics  ")

        expect(result.success).toBeTruthy()
        expect(mockPost).toHaveBeenLastCalledWith(expect.any(String), { label: "Exotics" })
        expect(manager.options.value).toHaveLength(4)
    })

    it("does not reload after a failed save", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockPut.mockResolvedValue({ success: false, result: null, errors: ["Duplicate"] })
        const manager = useCareerOptionManager("species")

        await manager.save(1, "Small Animal")

        expect(mockGet).not.toHaveBeenCalled()
    })

    it("reloads after a refused delete so the usage count explains the refusal", async () => {
        expect.hasAssertions()
        mockDel.mockResolvedValue({ success: false, result: null, errors: ["Option is in use."] })
        mockGet.mockResolvedValue({ success: true, result: [option(1, "Equine", { usageCount: 2 })] })
        const manager = useCareerOptionManager("species")

        await manager.remove(1)

        expect(manager.options.value[0]?.usageCount).toBe(2)
        expect(manager.deletingId.value).toBeNull()
    })
})
