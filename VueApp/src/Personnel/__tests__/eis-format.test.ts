import {
    describeAppointment,
    filterPeople,
    formatDecimal,
    formatEisDate,
    formatHours,
    formatMoney,
    formatPercent,
    formatServiceCredit,
} from "../services/eis-format"
import type { EisAppointment } from "../types/eis-types"

/**
 * The EIS pages format values the way the legacy pages did, so people comparing the two see the
 * same dates, amounts and service credit.
 */

const appointment: EisAppointment = {
    number: "1",
    title: "PROF-AY",
    titleCode: "001100",
    grade: null,
    exemptStatus: "EXEMPT",
    department: "VM: VME",
    beginDate: "07/01/2020",
    endDate: null,
    distributions: [],
    total: null,
}

describe("eis-format", () => {
    it("formats ISO dates as US dates and leaves other text alone", () => {
        expect.hasAssertions()
        expect(formatEisDate("2026-07-01")).toBe("07/01/2026")
        expect(formatEisDate("2026-07-01T00:00:00")).toBe("07/01/2026")
        expect(formatEisDate("07/2026")).toBe("07/2026")
        expect(formatEisDate(null)).toBe("")
    })

    it("formats money and percents with cents", () => {
        expect.hasAssertions()
        expect(formatMoney(1234.5)).toBe("1,234.50")
        expect(formatMoney(null)).toBe("")
        expect(formatPercent(75)).toBe("75.00")
    })

    it("formats hours and plain decimals", () => {
        expect.hasAssertions()
        expect(formatHours(120.5)).toBe("120.50 hrs")
        expect(formatHours(null)).toBe("")
        expect(formatDecimal(0.5)).toBe("0.5")
        expect(formatDecimal(null)).toBe("")
    })

    it("shows service credit in months and years", () => {
        expect.hasAssertions()
        expect(formatServiceCredit(360)).toBe("360 months (30.00 yrs)")
        expect(formatServiceCredit(1)).toBe("1 month (0.08 yrs)")
    })

    it("describes an appointment from the parts it has", () => {
        expect.hasAssertions()
        expect(describeAppointment(appointment)).toBe("Appt. 1 — PROF-AY (001100) — EXEMPT — VM: VME — 07/01/2020")
        expect(
            describeAppointment({ ...appointment, number: null, grade: "5", endDate: "06/30/2026", department: "" }),
        ).toBe("Appt. ? — PROF-AY (001100) — Grade: 5 — EXEMPT — 07/01/2020 to 06/30/2026")
    })

    it("filters people by name or employee ID, ignoring case", () => {
        expect.hasAssertions()
        const people = [
            { employeeId: "10123456", name: "Lovelace, Ada" },
            { employeeId: "10999999", name: "Byron, Bo" },
        ]

        expect(filterPeople(people, " ada ")).toStrictEqual([people[0]])
        expect(filterPeople(people, "1099")).toStrictEqual([people[1]])
        expect(filterPeople(people, "")).toBe(people)
    })
})
