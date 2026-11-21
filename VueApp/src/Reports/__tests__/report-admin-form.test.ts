import {
    emptyForm,
    formatChoices,
    missingColumns,
    newColumn,
    newParameter,
    operatorsFor,
    parameterTargets,
    parseChoices,
    toForm,
    toRequest,
    withType,
} from "../admin/report-admin-form"
import { rosterReport, rosterView } from "./report-admin-fixtures"

describe("choice text", () => {
    it("reads one choice per line, with an optional label", () => {
        expect.hasAssertions()

        expect(parseChoices("VME | Medicine\n\n  PMI  \r\nA|B|C")).toStrictEqual([
            { value: "VME", label: "Medicine" },
            { value: "PMI", label: "PMI" },
            { value: "A", label: "B|C" },
        ])
    })

    it("writes a choice's label only when it differs from its value", () => {
        expect.hasAssertions()

        expect(
            formatChoices([
                { value: "VME", label: "Medicine" },
                { value: "PMI", label: "PMI" },
            ]),
        ).toBe("VME | Medicine\nPMI")
        expect(formatChoices(null)).toBe("")
    })
})

describe("form and request", () => {
    it("round-trips a saved report", () => {
        expect.hasAssertions()

        expect(toRequest(toForm(rosterReport))).toStrictEqual(rosterReport)
    })

    it("edits a fixed choice list as text", () => {
        expect.hasAssertions()

        const [dept] = toForm(rosterReport).parameters

        expect([dept?.choicesFrom, dept?.choicesText]).toStrictEqual(["list", "VME | Medicine"])
    })

    it("sends choices only for choice parameters, from the list or the view picked", () => {
        expect.hasAssertions()
        const form = toForm(rosterReport)
        const [dept] = form.parameters

        const fromView = toRequest({
            ...form,
            parameters: [{ ...dept!, choicesFrom: "view", choicesSource: "DeptChoices" }],
        })
        const asText = toRequest({ ...form, parameters: [{ ...dept!, type: "Text", choicesSource: "DeptChoices" }] })

        expect([fromView.parameters[0]?.choicesList, fromView.parameters[0]?.choicesSource]).toStrictEqual([
            null,
            "DeptChoices",
        ])
        expect([asText.parameters[0]?.choicesList, asText.parameters[0]?.choicesSource]).toStrictEqual([null, null])
    })

    it("picks a choice view for a parameter saved with one", () => {
        expect.hasAssertions()
        const [dept] = rosterReport.parameters

        const form = toForm({
            ...rosterReport,
            parameters: [{ ...dept!, choicesList: null, choicesSource: "DeptChoices" }],
        })

        expect(form.parameters[0]?.choicesFrom).toBe("view")
    })

    it("starts a new report paged, with Excel and no PDF", () => {
        expect.hasAssertions()

        expect(emptyForm()).toMatchObject({
            title: "",
            source: "",
            paginate: true,
            allowExcel: true,
            allowPdf: false,
            columns: [],
        })
    })
})

describe("columns", () => {
    it("adds the source's showable columns the report lacks, with formats from their types", () => {
        expect.hasAssertions()

        const added = missingColumns(rosterView, [newColumn("name")])

        expect(added.map((column) => [column.sourceColumn, column.format])).toStrictEqual([["Hired", "Date"]])
    })

    it("gives each new column its own key", () => {
        expect.hasAssertions()

        expect(newColumn().key).not.toBe(newColumn().key)
    })
})

describe("parameters", () => {
    it("starts a view's parameter as an equals filter and a procedure's without one", () => {
        expect.hasAssertions()

        expect([newParameter(true).operator, newParameter(false).operator]).toStrictEqual(["Equals", null])
    })

    it("keeps an operator that suits the new type, or picks the first that does", () => {
        expect.hasAssertions()
        const from = { ...newParameter(true), type: "Number" as const, operator: "From" as const }

        expect(withType(from, "Date", true).operator).toBe("From")
        expect(withType(from, "MultiChoice", true).operator).toBe("In")
        expect(withType(from, "Text", false).operator).toBeNull()
    })

    it("offers the operators that suit each type", () => {
        expect.hasAssertions()

        expect([operatorsFor("Text"), operatorsFor("MultiChoice")]).toStrictEqual([["Equals", "Contains"], ["In"]])
    })

    it("binds to a view's showable columns or a procedure's parameters", () => {
        expect.hasAssertions()
        const procedure = {
            ...rosterView,
            kind: "Procedure" as const,
            parameters: [
                { name: "LoginId", sqlType: "varchar", isSupported: true },
                { name: "Out", sqlType: "int", isSupported: false },
            ],
        }

        expect([parameterTargets(rosterView), parameterTargets(procedure), parameterTargets(null)]).toStrictEqual([
            ["Name", "Hired"],
            ["LoginId"],
            [],
        ])
    })
})
