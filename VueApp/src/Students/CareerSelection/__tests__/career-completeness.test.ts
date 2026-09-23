import { isSelectionComplete, missingFieldLabels } from "../utils/career-completeness"
import type { CareerDropdownOption, StudentInfo } from "../types"

/**
 * Tests for the form's missing-fields warning. isSelectionComplete mirrors IsSelectionComplete in
 * CareerSelectionService.cs, which drives the roster icons and the exports, so the cases here are
 * the ones the C# tests cover too.
 */

function option(label: string, value: number, isOther = false): CareerDropdownOption {
    return { label, value, isOther }
}

function answered(overrides: Partial<StudentInfo> = {}): StudentInfo {
    return {
        direction: option("Academia", 1),
        directionOther: "",
        primaryFocus: option("Equine", 2),
        primaryFocusOther: "",
        secondaryFocus: option("Bovine", 3),
        secondaryFocusOther: "",
        postGrad: option("Residency", 4),
        shortTermPlans: "Internship",
        longTermPlans: "Practice ownership",
        ...overrides,
    }
}

describe("is selection complete", () => {
    it("counts an ordinary choice as answered", () => {
        expect.hasAssertions()
        expect(isSelectionComplete(option("Academia", 1), null)).toBeTruthy()
    })

    it("counts nothing selected as unanswered", () => {
        expect.hasAssertions()
        expect(isSelectionComplete(null, "Wildlife")).toBeFalsy()
    })

    it("wants the free text before the catch-all counts as answered", () => {
        expect.hasAssertions()
        const other = option("Other", 9, true)
        expect(isSelectionComplete(other, null)).toBeFalsy()
        expect(isSelectionComplete(other, "Wildlife rehabilitation")).toBeTruthy()
    })

    it("does not accept whitespace as the catch-all's free text", () => {
        expect.hasAssertions()
        expect(isSelectionComplete(option("Other", 9, true), "   ")).toBeFalsy()
    })

    it("ignores free text for an ordinary choice", () => {
        expect.hasAssertions()
        expect(isSelectionComplete(option("Academia", 1), "")).toBeTruthy()
    })
})

describe("missing field labels", () => {
    it("wants nothing when every field is answered", () => {
        expect.hasAssertions()
        expect(missingFieldLabels(answered())).toStrictEqual([])
    })

    it("lists every field of an empty form, in page order", () => {
        expect.hasAssertions()
        const empty = answered({
            direction: null,
            primaryFocus: null,
            secondaryFocus: null,
            postGrad: null,
            shortTermPlans: "",
            longTermPlans: "",
        })

        expect(missingFieldLabels(empty)).toStrictEqual([
            "Career Direction",
            "Primary Focus",
            "Secondary Focus",
            "Post-Graduation Plans",
            "Short Term Plans",
            "Long Term Plans",
        ])
    })

    it("prompts for the secondary focus even though the roster treats it as optional", () => {
        expect.hasAssertions()
        expect(missingFieldLabels(answered({ secondaryFocus: null }))).toStrictEqual(["Secondary Focus"])
    })

    it("reads the short term plans as the post-grad catch-all's explanation", () => {
        expect.hasAssertions()
        // Post-graduation plans have no free-text field of their own.
        const other = option("Other", 9, true)

        expect(missingFieldLabels(answered({ postGrad: other, shortTermPlans: "Research fellowship" }))).toStrictEqual(
            [],
        )
        expect(missingFieldLabels(answered({ postGrad: other, shortTermPlans: "" }))).toStrictEqual([
            "Post-Graduation Plans",
            "Short Term Plans",
        ])
    })

    it("treats a whitespace-only statement as unanswered", () => {
        expect.hasAssertions()
        expect(missingFieldLabels(answered({ longTermPlans: "   " }))).toStrictEqual(["Long Term Plans"])
    })

    it("wants the catch-all's free text before the field counts as answered", () => {
        expect.hasAssertions()
        const withOther = answered({ direction: option("Other", 9, true), directionOther: "" })

        expect(missingFieldLabels(withOther)).toStrictEqual(["Career Direction"])
    })
})
