import { ref } from "vue"
import { columnText, useMobileTableRows } from "../composables/use-mobile-table-rows"
import type { Ref } from "vue"
import type { QTableColumn, QTableProps } from "quasar"

type Row = { name: string; office: string | null; phone: string }

const columns: QTableColumn[] = [
    { name: "name", label: "Name", field: "name", align: "left", sortable: true },
    { name: "office", label: "Office", field: "office", align: "left", sortable: true },
    { name: "phone", label: "Phone", field: "phone", align: "left" },
]

const rows: Row[] = [
    { name: "Pharmacy", office: "Room 300", phone: "530-555-3000" },
    { name: "Front Desk", office: null, phone: "530-555-1000" },
    { name: "Imaging", office: "Room 100", phone: "530-555-2000" },
]

function setup(search = "") {
    const pagination: Ref<QTableProps["pagination"]> = ref({ rowsPerPage: 0, sortBy: null, descending: false })
    const view = useMobileTableRows<Row>({ columns, rows, search, pagination })
    return { ...view, pagination }
}

describe("columnText()", () => {
    it("reads a string field off the row", () => {
        expect.hasAssertions()

        expect(columnText(columns[0], rows[0])).toBe("Pharmacy")
    })

    it("reads a value through a function field", () => {
        expect.hasAssertions()
        const computedColumn: QTableColumn = {
            name: "both",
            label: "Both",
            field: (row: Row) => `${row.name} ${row.phone}`,
            align: "left",
        }

        expect(columnText(computedColumn, rows[0])).toBe("Pharmacy 530-555-3000")
    })

    it("applies a column format, the way the table would", () => {
        expect.hasAssertions()
        const formatted: QTableColumn = { ...columns[2], format: (value: string) => `tel: ${value}` }

        expect(columnText(formatted, rows[0])).toBe("tel: 530-555-3000")
    })

    it("reads a missing value as empty rather than as null", () => {
        expect.hasAssertions()

        expect(columnText(columns[1], rows[1])).toBe("")
    })
})

describe("useMobileTableRows - filtering", () => {
    it("returns every row when nothing is searched for", () => {
        expect.hasAssertions()

        expect(setup().visibleRows.value).toHaveLength(3)
    })

    it("matches case-insensitively on any column, as the table's filter prop does", () => {
        expect.hasAssertions()

        expect(setup("ROOM 100").visibleRows.value.map((row) => row.name)).toStrictEqual(["Imaging"])
    })

    it("matches a column the caller may not be displaying", () => {
        expect.hasAssertions()

        expect(setup("555-1000").visibleRows.value.map((row) => row.name)).toStrictEqual(["Front Desk"])
    })
})

describe("useMobileTableRows - sorting", () => {
    it("offers only sortable columns as options", () => {
        expect.hasAssertions()

        expect(setup().sortOptions.value).toStrictEqual([
            { label: "Name", value: "name" },
            { label: "Office", value: "office" },
        ])
    })

    it("names a column whose header is deliberately blank so the option is not empty", () => {
        expect.hasAssertions()
        const unlabelled: QTableColumn[] = [
            { name: "unitName", label: "", field: "name", align: "left", sortable: true },
            { name: "office", label: "Office", field: "office", align: "left", sortable: true },
        ]
        const pagination: Ref<QTableProps["pagination"]> = ref({ rowsPerPage: 0, sortBy: null, descending: false })

        const view = useMobileTableRows<Row>({ columns: unlabelled, rows, search: "", pagination })

        expect(view.sortOptions.value).toStrictEqual([
            { label: "Unit", value: "unitName" },
            { label: "Office", value: "office" },
        ])
    })

    it("leaves rows in source order until a column is chosen", () => {
        expect.hasAssertions()

        expect(setup().visibleRows.value.map((row) => row.name)).toStrictEqual(["Pharmacy", "Front Desk", "Imaging"])
    })

    it("orders ascending on the chosen column", () => {
        expect.hasAssertions()
        const view = setup()

        view.sortBy.value = "name"

        expect(view.visibleRows.value.map((row) => row.name)).toStrictEqual(["Front Desk", "Imaging", "Pharmacy"])
    })

    it("reverses on descending", () => {
        expect.hasAssertions()
        const view = setup()

        view.sortBy.value = "name"
        view.sortDescending.value = true

        expect(view.visibleRows.value.map((row) => row.name)).toStrictEqual(["Pharmacy", "Imaging", "Front Desk"])
    })

    it("sorts a missing value first, matching how QTable orders a null cell", () => {
        expect.hasAssertions()
        const view = setup()

        view.sortBy.value = "office"

        expect(view.visibleRows.value.map((row) => row.name)).toStrictEqual(["Front Desk", "Imaging", "Pharmacy"])
    })

    it("writes the sort into the pagination model the table is bound to", () => {
        expect.hasAssertions()
        const view = setup()

        view.sortBy.value = "office"
        view.sortDescending.value = true

        expect(view.pagination.value).toMatchObject({ sortBy: "office", descending: true })
    })

    it("reads a sort the table put into the pagination model, such as a header click", () => {
        expect.hasAssertions()
        const view = setup()

        view.pagination.value = { rowsPerPage: 0, sortBy: "name", descending: false }

        expect(view.sortBy.value).toBe("name")
        expect(view.visibleRows.value.map((row) => row.name)).toStrictEqual(["Front Desk", "Imaging", "Pharmacy"])
    })

    it("sorts the filtered rows rather than all of them", () => {
        expect.hasAssertions()
        const view = setup("room")

        view.sortBy.value = "name"

        expect(view.visibleRows.value.map((row) => row.name)).toStrictEqual(["Imaging", "Pharmacy"])
    })

    it("leaves the caller's own array in its original order", () => {
        expect.hasAssertions()
        const view = setup()

        view.sortBy.value = "name"
        view.visibleRows.value.map((row) => row.name)

        expect(rows.map((row) => row.name)).toStrictEqual(["Pharmacy", "Front Desk", "Imaging"])
    })
})
