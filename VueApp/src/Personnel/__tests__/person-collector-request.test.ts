import {
    emptyRequest,
    hasChoice,
    studentKeys,
    toApiRequest,
    withStudentKeys,
} from "../services/person-collector-request"

/**
 * The request helpers decide when there is anything to load, and keep MPVM in the student picker
 * while the API takes it as its own flag.
 */

describe("person collector request", () => {
    it("needs a list, not just a department filter", () => {
        expect.hasAssertions()
        const departmentsOnly = { ...emptyRequest(), departments: ["VME"] }

        expect(hasChoice(emptyRequest())).toBeFalsy()
        expect(hasChoice(departmentsOnly)).toBeFalsy()
        expect(hasChoice({ ...departmentsOnly, fullDepartmentList: true })).toBeTruthy()
        expect(
            [
                { senateEmeriti: true },
                { senateGroups: ["PROFESSOR"] },
                { federationGroups: ["ADJUNCT"] },
                { staffMsp: true },
                { staffPss: true },
                { staffVeterinarians: true },
                { studentClasses: ["V1"] },
                { studentMpvm: true },
            ].filter((choice) => !hasChoice({ ...emptyRequest(), ...choice })),
        ).toStrictEqual([])
    })

    it("drops the full-list flag when no department is chosen", () => {
        expect.hasAssertions()

        expect(toApiRequest({ ...emptyRequest(), fullDepartmentList: true }).fullDepartmentList).toBeFalsy()
        expect(
            toApiRequest({ ...emptyRequest(), fullDepartmentList: true, departments: ["VME"] }).fullDepartmentList,
        ).toBeTruthy()
    })

    it("keeps MPVM in the student picker and splits it out for the API", () => {
        expect.hasAssertions()
        const chosen = withStudentKeys(emptyRequest(), ["V2", "MPVM"])

        expect(chosen).toMatchObject({ studentClasses: ["V2"], studentMpvm: true })
        expect(studentKeys(chosen)).toStrictEqual(["V2", "MPVM"])
        expect(studentKeys(emptyRequest())).toStrictEqual([])
    })
})
