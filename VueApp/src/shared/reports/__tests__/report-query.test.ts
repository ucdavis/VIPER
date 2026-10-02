import { hasQueryParameters, initialValues, toQuery, toRequestBody } from "../report-query"
import type { ReportParameterMetadata, ReportParameterType, ReportParameterValue } from "../report-types"

function parameter(
    name: string,
    type: ReportParameterType,
    defaultValue: ReportParameterValue = null,
): ReportParameterMetadata {
    return { name, label: name, type, required: false, defaultValue, options: [] }
}

const parameters = [
    parameter("facultyType", "Choice", "S"),
    parameter("departments", "MultiChoice"),
    parameter("startDate", "Date", "2026-07-01"),
    parameter("search", "Text"),
    parameter("minimumAge", "Number"),
    parameter("includeEmeriti", "Boolean"),
]

describe("initialValues()", () => {
    it("uses the report's defaults, or empty values, when the query is empty", () => {
        expect(initialValues(parameters, {})).toStrictEqual({
            facultyType: "S",
            departments: [],
            startDate: "2026-07-01",
            search: null,
            minimumAge: null,
            includeEmeriti: null,
        })
    })

    it("reads each parameter type from the query", () => {
        const values = initialValues(parameters, {
            facultyType: "F",
            departments: ["VME", "APC"],
            startDate: "2025-07-01",
            search: "smith",
            minimumAge: "60",
            includeEmeriti: "true",
        })

        expect(values).toStrictEqual({
            facultyType: "F",
            departments: ["VME", "APC"],
            startDate: "2025-07-01",
            search: "smith",
            minimumAge: 60,
            includeEmeriti: true,
        })
    })

    it("accepts a single value for a list and a list for a single value", () => {
        const values = initialValues(parameters, { departments: "VME", search: ["first", "second"] })

        expect(values.departments).toStrictEqual(["VME"])
        expect(values.search).toBe("first")
    })

    it.each([
        ["minimumAge", "old", null],
        ["minimumAge", "", null],
        ["minimumAge", null, null],
        ["includeEmeriti", "false", false],
        ["includeEmeriti", null, null],
        ["facultyType", null, null],
        ["startDate", null, null],
        ["search", null, null],
        ["departments", null, []],
    ])("reads %s=%o as %o", (name, raw, expected) => {
        expect(initialValues(parameters, { [name]: raw })[name]).toStrictEqual(expected)
    })
})

describe("toQuery()", () => {
    it("writes the non-empty values, repeating the key for lists", () => {
        const query = toQuery(parameters, {
            facultyType: "F",
            departments: ["VME", "APC"],
            startDate: "2025-07-01",
            search: "",
            minimumAge: 60,
            includeEmeriti: false,
        })

        expect(query).toStrictEqual({
            facultyType: "F",
            departments: ["VME", "APC"],
            startDate: "2025-07-01",
            minimumAge: "60",
            includeEmeriti: "false",
        })
    })

    it("round-trips through initialValues", () => {
        const values = { ...initialValues(parameters, {}), minimumAge: 45, includeEmeriti: true }

        expect(initialValues(parameters, toQuery(parameters, values) as never)).toStrictEqual(values)
    })
})

describe("toRequestBody()", () => {
    it("sends only the report's non-empty parameters", () => {
        const body = toRequestBody(parameters, {
            facultyType: "F",
            departments: [],
            search: "smith",
            includeEmeriti: false,
            unknown: "ignored",
        })

        expect(body).toStrictEqual({ facultyType: "F", search: "smith", includeEmeriti: false })
    })
})

describe("hasQueryParameters()", () => {
    it("is true when the query names one of the report's parameters", () => {
        expect(hasQueryParameters(parameters, { search: "smith" })).toBeTruthy()
        expect(hasQueryParameters(parameters, { tab: "2" })).toBeFalsy()
    })
})
