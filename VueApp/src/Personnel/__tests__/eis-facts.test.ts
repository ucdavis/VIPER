import { headerFacts, listingFacts, summaryFacts } from "../services/eis-facts"
import type { EisPersonHeader } from "../types/eis-types"

/**
 * The EIS fact lists keep missing values as null (shown blank) and leave out the rows a person
 * doesn't have: staff and faculty programs, and a PTO balance of zero.
 */

const person: EisPersonHeader = {
    employeeId: "10123456",
    name: "Lovelace, Ada",
    email: null,
    dateOfBirth: null,
    age: null,
    gender: null,
    ethnicity: null,
    hireDate: null,
    employmentStatus: null,
    citizenship: null,
    visa: null,
    primaryAffiliation: null,
    primaryTitle: null,
    jobGroup: null,
    homeDepartment: null,
    alternateDepartment: null,
    bargainingUnit: null,
    laborRelationsUnit: null,
    isStaff: false,
    staffProgram: null,
    staffStatus: null,
    isFaculty: false,
    facultyProgram: null,
    ladderRank: null,
    serviceCreditMonths: 0,
    vacationHours: null,
    sickHours: null,
    ptoHours: 0,
}

function labels(facts: { label: string }[]): string[] {
    return facts.map((fact) => fact.label)
}

describe("eis-facts", () => {
    it("links the email and shows PTO only when there is a balance", () => {
        expect.hasAssertions()
        const facts = summaryFacts({ ...person, email: "ada@ucdavis.edu", ptoHours: 8 })

        expect(facts[0]).toStrictEqual({ label: "Email", value: "ada@ucdavis.edu", href: "mailto:ada@ucdavis.edu" })
        expect(labels(facts)).toContain("PTO balance")
        expect(summaryFacts(person)[0]).toStrictEqual({ label: "Email", value: null })
        expect(labels(summaryFacts(person))).not.toContain("PTO balance")
    })

    it("adds the staff and faculty programs only for people who hold them", () => {
        expect.hasAssertions()

        expect(labels(headerFacts(person))).not.toContain("Staff program")
        expect(labels(headerFacts({ ...person, isStaff: true, isFaculty: true }))).toStrictEqual(
            expect.arrayContaining(["Staff program", "Staff type", "Faculty program", "Ladder rank"]),
        )
    })

    it("lists a campus listing's details", () => {
        expect.hasAssertions()
        const facts = listingFacts({
            isPrimary: true,
            isPublic: true,
            title: "Professor",
            department: null,
            address: null,
            phone: null,
        })

        expect(facts.map((fact) => fact.value)).toStrictEqual(["Professor", null, null, null])
    })
})
