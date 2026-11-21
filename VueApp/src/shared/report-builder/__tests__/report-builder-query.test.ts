import { hasRequiredValues, initialValues, isEmptyValue, toQuery, toRequestParameters } from "../report-builder-query"
import { deptParameter, hiredParameter, roster, textParameter } from "./report-builder-fixtures"

describe("starting values", () => {
    it("takes values from the query first", () => {
        expect.hasAssertions()

        const values = initialValues(roster.parameters, { Name: "Ada", Dept: ["VME", "PMI"], Hired: "2026-03-01" })

        expect(values).toStrictEqual({ Name: "Ada", Dept: ["VME", "PMI"], Hired: "2026-03-01" })
    })

    it("then from the embedding page, then from the defaults", () => {
        expect.hasAssertions()

        const values = initialValues(roster.parameters, {}, { Name: "Bo", Dept: "VME" })

        expect(values).toStrictEqual({ Name: "Bo", Dept: ["VME"], Hired: "2026-01-01" })
    })

    it("leaves a parameter without a value empty", () => {
        expect.hasAssertions()

        expect(initialValues([textParameter, deptParameter], { Name: "" })).toStrictEqual({ Name: null, Dept: [] })
    })

    it("ignores query values that aren't strings", () => {
        expect.hasAssertions()

        expect(initialValues([textParameter], { Name: [null, "Ada"] })).toStrictEqual({ Name: "Ada" })
    })
})

describe("required values", () => {
    it("is true only when every required parameter has a value", () => {
        expect.hasAssertions()

        expect(hasRequiredValues([textParameter, hiredParameter], { Name: null, Hired: "2026-01-01" })).toBeTruthy()
        expect(hasRequiredValues([textParameter, hiredParameter], { Name: "Ada", Hired: null })).toBeFalsy()
    })
})

describe("request and query", () => {
    const values = { Name: "Ada", Dept: ["VME", "PMI"], Hired: null, Unknown: "x" }

    it("sends every filled-in value as a list", () => {
        expect.hasAssertions()

        expect(toRequestParameters(roster.parameters, values)).toStrictEqual({ Name: ["Ada"], Dept: ["VME", "PMI"] })
    })

    it("writes single values plainly and repeats the key for lists", () => {
        expect.hasAssertions()

        expect(toQuery(roster.parameters, values)).toStrictEqual({ Name: "Ada", Dept: ["VME", "PMI"] })
    })
})

describe("empty values", () => {
    it.each([null, undefined, "", []])("treats %j as empty", (value) => {
        expect.hasAssertions()

        expect(isEmptyValue(value)).toBeTruthy()
    })

    it("treats text and lists with values as filled in", () => {
        expect.hasAssertions()

        expect(isEmptyValue("x")).toBeFalsy()
        expect(isEmptyValue(["x"])).toBeFalsy()
    })
})
