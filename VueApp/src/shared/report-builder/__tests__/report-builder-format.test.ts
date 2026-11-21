import { formatCell, isNumericFormat } from "../report-builder-format"

describe("cell text", () => {
    it.each([
        [1234.5, "Number", "1,234.5"],
        [1234.5, "Currency", "$1,234.50"],
        [0.255, "Percent", "25.5%"],
        [7, "Text", "7"],
    ] as const)("shows %j in a %s column as %s", (value, format, expected) => {
        expect.hasAssertions()

        expect(formatCell(value, format)).toBe(expected)
    })

    it.each([
        ["2026-03-01T00:00:00", "Date", "03/01/2026"],
        ["2026-03-01T14:05:00", "Date", "03/01/2026"],
        ["2026-03-01T00:00:00", "DateTime", "03/01/2026 12:00 AM"],
        ["2026-03-01T14:05:00", "DateTime", "03/01/2026 2:05 PM"],
        ["2026-03-01T12:30:00", "Text", "03/01/2026 12:30 PM"],
        ["2026-03-01T00:00:00", "Text", "03/01/2026"],
        ["2026-03-01", "Date", "03/01/2026"],
    ] as const)("shows the date %s in a %s column as %s", (value, format, expected) => {
        expect.hasAssertions()

        expect(formatCell(value, format)).toBe(expected)
    })

    it("leaves text that only looks like a date alone in a text column", () => {
        expect.hasAssertions()

        expect(formatCell("2026-03-01", "Text")).toBe("2026-03-01")
    })

    it("leaves text that isn't a date alone in a text column", () => {
        expect.hasAssertions()

        expect(formatCell("N/A", "Text")).toBe("N/A")
    })

    it.each([
        [true, "Yes"],
        [false, "No"],
        [1, "Yes"],
        [0, "No"],
        [" y ", "Yes"],
        ["FALSE", "No"],
        ["Maybe", "Maybe"],
    ] as const)("shows %j in a yes/no column as %s", (value, expected) => {
        expect.hasAssertions()

        expect(formatCell(value, "YesNo")).toBe(expected)
    })

    it("shows true and false as Yes and No in any column, and nothing as blank", () => {
        expect.hasAssertions()

        expect([formatCell(true, "Text"), formatCell(false, "Number"), formatCell(null, "Text")]).toStrictEqual([
            "Yes",
            "No",
            "",
        ])
    })
})

describe("numeric formats", () => {
    it("is true for number, currency and percent columns only", () => {
        expect.hasAssertions()

        expect(
            ["Number", "Currency", "Percent", "Text", "Date"].map((format) => isNumericFormat(format as never)),
        ).toStrictEqual([true, true, true, false, false])
    })
})
