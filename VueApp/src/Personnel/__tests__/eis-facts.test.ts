import { headerFacts, listingFacts, summaryFacts } from "../utils/eis-facts"
import type { EisFactRow, EisFactSection } from "../utils/eis-facts"
import type { EisPersonHeader } from "../types/eis-types"

/**
 * The EIS fact lists pair related facts on one line as the legacy header did, keep missing
 * values as null (shown blank) and leave out what a person doesn't have: staff and faculty
 * programs, and a PTO balance of zero.
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

function labels(rows: EisFactRow[]): string[] {
    return rows.flat().map((fact) => fact.label)
}

/** Every row of every Summary section, in order. */
function summaryRows(sections: EisFactSection[]): EisFactRow[] {
    return sections.flatMap((section) => section.rows)
}

/** Each row's labels joined, so the pairing is easy to read in the expectations. */
function lines(rows: EisFactRow[]): string[] {
    return rows.map((row) => row.map((fact) => fact.label).join(" | "))
}

describe("eis-facts", () => {
    it("links the email and shows PTO only when there is a balance", () => {
        expect.hasAssertions()
        const facts = summaryRows(summaryFacts({ ...person, email: "ada@ucdavis.edu", ptoHours: 8 }))

        expect(facts[0]).toStrictEqual([{ label: "Email", value: "ada@ucdavis.edu", href: "mailto:ada@ucdavis.edu" }])
        expect(labels(facts)).toContain("PTO balance")
        expect(summaryRows(summaryFacts(person))[0]).toStrictEqual([{ label: "Email", value: null }])
        expect(labels(summaryRows(summaryFacts(person)))).not.toContain("PTO balance")
    })

    it("pairs related facts the way the legacy header did", () => {
        expect.hasAssertions()

        expect(lines(headerFacts({ ...person, isStaff: true, isFaculty: true }))).toStrictEqual([
            "Employee ID",
            "Primary affiliation",
            "Primary title | Job group ID",
            "Home department | Alternate department",
            "Staff program | Staff type",
            "Faculty program | Ladder rank",
        ])
    })

    it("keeps the legacy order in the Summary, split into personal, position, and service and leave", () => {
        expect.hasAssertions()
        const sections = summaryFacts({ ...person, ptoHours: 8 })

        expect(sections.map((section) => [section.title, lines(section.rows)])).toStrictEqual([
            [
                "Personal",
                [
                    "Email",
                    "Date of birth | Age",
                    "Gender | Ethnicity",
                    "Hire date | Employment status",
                    "US citizen | Visa type",
                ],
            ],
            ["Position", ["Bargaining unit | Labor relations unit"]],
            ["Service and leave", ["Service credit | Vacation balance", "Sick leave balance | PTO balance"]],
        ])
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

        expect(facts.map((row) => row.map((fact) => fact.value))).toStrictEqual([
            ["Professor", null],
            [null, null],
        ])
    })
})
