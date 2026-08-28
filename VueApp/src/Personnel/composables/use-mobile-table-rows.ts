import { computed, toValue } from "vue"
import type { MaybeRefOrGetter, Ref } from "vue"
import type { QTableColumn, QTableProps } from "quasar"

/**
 * Reads a cell the way QTable does, so a list rendering of the same rows shows, matches and
 * orders on exactly the text the table would put in that column.
 */
function columnText(column: QTableColumn, row: unknown): string {
    const raw = typeof column.field === "function" ? column.field(row) : (row as Record<string, unknown>)[column.field]
    const formatted = column.format === undefined ? raw : column.format(raw, row)
    return formatted === null || formatted === undefined ? "" : String(formatted)
}

/**
 * The rows a QTable would keep for this search: a case-insensitive match on any cell. Exported
 * because a page outside the table needs the same answer - to know which sections still have
 * anything in them - and two implementations of "what the filter matches" would drift.
 */
function filterRows<T>(columns: QTableColumn[] | undefined, rows: T[], search: string): T[] {
    const terms = search.toLowerCase()
    if (terms === "") {
        return rows
    }
    return rows.filter((row) => (columns ?? []).some((column) => columnText(column, row).toLowerCase().includes(terms)))
}

type SortOption = { label: string; value: string }

/**
 * What an unlabelled sortable column is called in the sort control, and only there. A section
 * whose units have no collective name renders a deliberately blank unit-column header - the
 * section heading above the table already names it - but that context is gone by the time the
 * column is one entry in a "Sort by" dropdown, where a blank option names nothing at all.
 */
const UNLABELLED_SORT_OPTION = "Unit"

type MobileTableRowsOptions<T> = {
    columns: MaybeRefOrGetter<QTableColumn[] | undefined>
    rows: MaybeRefOrGetter<T[]>
    search: MaybeRefOrGetter<string>
    /** The model already bound to the table with v-model:pagination. */
    pagination: Ref<QTableProps["pagination"]>
}

/**
 * Filtering and sorting for the card list the phone list tables render below the breakpoint at
 * which their columns stop fitting. QTable does both of these internally from its `filter` prop
 * and its column headers, and neither reaches markup outside the table, so this mirrors them.
 *
 * The sort is held in the caller's `pagination` model, the one already bound to the table with
 * `v-model:pagination`. That makes it a single source of truth: a sort chosen in the list is the
 * sort the table shows if the window widens, and a header click on desktop is reflected in the
 * list's control.
 *
 * Sorting compares the same text the cards display. Every sortable column in these tables holds a
 * string, and a missing value reads as "" here just as it sorts first in QTable. A numeric or date
 * column would need the type-aware branches from Quasar's own sort method.
 */
function useMobileTableRows<T>({ columns, rows, search, pagination }: MobileTableRowsOptions<T>) {
    const columnList = computed(() => toValue(columns) ?? [])

    const filteredRows = computed(() => filterRows(columnList.value, toValue(rows), toValue(search)))

    /** Only the columns whose headers the table would let a user click. */
    const sortOptions = computed<SortOption[]>(() =>
        columnList.value
            .filter((column) => column.sortable === true)
            .map((column) => ({
                label: column.label.trim() === "" ? UNLABELLED_SORT_OPTION : column.label,
                value: column.name,
            })),
    )

    const sortBy = computed({
        get: () => pagination.value?.sortBy ?? null,
        set: (name: string | null) => {
            pagination.value = { ...pagination.value, sortBy: name }
        },
    })

    const sortDescending = computed({
        get: () => pagination.value?.descending === true,
        set: (descending: boolean) => {
            pagination.value = { ...pagination.value, descending }
        },
    })

    const visibleRows = computed(() => {
        const column = columnList.value.find((candidate) => candidate.name === sortBy.value)
        if (column === undefined) {
            return filteredRows.value
        }
        const direction = sortDescending.value ? -1 : 1
        // Uses toSorted, not sort: these rows belong to the caller and must not be reordered in place.
        return filteredRows.value.toSorted((a, b) => {
            const first = columnText(column, a).toLowerCase()
            const second = columnText(column, b).toLowerCase()
            if (first === second) {
                return 0
            }
            return first < second ? -direction : direction
        })
    })

    return { sortOptions, sortBy, sortDescending, visibleRows }
}

export { columnText, filterRows, useMobileTableRows }
export type { SortOption }
