import { CAREER_FIELDS } from "../utils/career-fields"
import type { StudentCareerListItem, StudentCareerReport } from "../types"

/**
 * Tests for the career field metadata. The overview, report, mobile cards and exports all read
 * their columns from this one list, so a wrong key here breaks several views at once.
 */

function listItem(): StudentCareerListItem {
    return {
        personId: 100,
        rowKey: "100",
        hasDetailRoute: true,
        fullName: "Student, Test",
        classLevel: "V1",
        email: "tstudent@ucdavis.edu",
        directionCompleted: true,
        primaryFocusCompleted: true,
        secondaryFocusCompleted: false,
        postGradCompleted: true,
        shortTermPlansCompleted: true,
        longTermPlansCompleted: false,
        mentorName: "Vet, Ann",
        lastUpdated: "2026-04-17T10:00:00",
    }
}

function report(): StudentCareerReport {
    return {
        personId: 100,
        rowKey: "100",
        hasDetailRoute: true,
        fullName: "Student, Test",
        classLevel: "V1",
        email: "tstudent@ucdavis.edu",
        direction: "Academia",
        primaryFocus: "Equine",
        secondaryFocus: "",
        postGrad: "Residency",
        shortTermPlans: "Internship",
        longTermPlans: "",
        mentorName: "Vet, Ann",
        lastUpdated: "2026-04-17T10:00:00",
    }
}

describe("career fields", () => {
    it("names each field once", () => {
        expect.hasAssertions()
        const names = CAREER_FIELDS.map((f) => f.name)
        expect(new Set(names).size).toBe(names.length)
    })

    it("reads a value from the report for every field", () => {
        expect.hasAssertions()
        const row = report()
        for (const field of CAREER_FIELDS) {
            expect(row).toHaveProperty(field.valueField)
        }
    })

    it("reads a completeness flag from the overview for every flagged field", () => {
        expect.hasAssertions()
        const row = listItem()
        for (const field of CAREER_FIELDS.filter((f) => f.completedField)) {
            expect(row).toHaveProperty(field.completedField!)
        }
    })

    it("labels every completeness icon for screen readers", () => {
        expect.hasAssertions()
        for (const field of CAREER_FIELDS.filter((f) => f.completedField)) {
            expect(field.tooltipLabel).toBeTruthy()
        }
    })

    it("shows the mentor as plain text on both pages", () => {
        expect.hasAssertions()
        // The mentor is admin-managed, so it is reported rather than flagged as complete.
        const mentor = CAREER_FIELDS.find((f) => f.name === "mentor")
        expect(mentor?.completedField).toBeUndefined()
        expect(mentor?.valueField).toBe("mentorName")
    })

    it("treats only the second species focus as optional", () => {
        expect.hasAssertions()
        expect(CAREER_FIELDS.filter((f) => f.optional).map((f) => f.name)).toStrictEqual(["secondarySpecies"])
    })

    it("treats only the two plan statements as free text", () => {
        expect.hasAssertions()
        expect(CAREER_FIELDS.filter((f) => f.statement).map((f) => f.name)).toStrictEqual(["shortTerm", "longTerm"])
    })

    it("keeps the column order the grids and exports share", () => {
        expect.hasAssertions()
        expect(CAREER_FIELDS.map((f) => f.label)).toStrictEqual([
            "Career",
            "Species 1",
            "Species 2",
            "Post Grad",
            "Mentor",
            "Short Term",
            "Long Term",
        ])
    })
})
