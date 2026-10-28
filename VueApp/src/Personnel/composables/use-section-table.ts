import { computed, ref, toValue } from "vue"
import { Screen } from "quasar"
import { filterRows } from "./use-mobile-table-rows"
import type { MaybeRefOrGetter } from "vue"
import type { QTableColumn, QTableProps } from "quasar"

/**
 * Ensures the anchor ID is applied to the correct heading depending on screen size.
 * Both the desktop and the mobile headings exist in the DOM, but only one is visible at a time,
 * with lt.md as a breakpoint. Since IDs are unique, they must be applied only to the visible heading.
 */
function useHeadingAnchorIds(anchorId: () => string | undefined) {
    return {
        desktopAnchorId: computed(() => (Screen.lt.md ? undefined : anchorId())),
        mobileAnchorId: computed(() => (Screen.lt.md ? anchorId() : undefined)),
    }
}

/**
 * The state every phone list table keeps across its two renderings - the desktop QTable and the
 * mobile card list - kept in one place so the tables cannot drift apart on it.
 */
function useSectionTable(options: {
    columns: MaybeRefOrGetter<QTableColumn[] | undefined>
    rows: MaybeRefOrGetter<unknown[]>
    search: MaybeRefOrGetter<string>
    anchorId: () => string | undefined
}) {
    // Bound to the table, and shared with the card list's sort control.
    const pagination = ref<QTableProps["pagination"]>({ rowsPerPage: 0, sortBy: null, descending: false })

    // The same filterRows the card list and the page's jump links use, so all three agree on it.
    const hasMatches = computed(
        () => filterRows(toValue(options.columns), toValue(options.rows), toValue(options.search)).length > 0,
    )

    return { pagination, hasMatches, ...useHeadingAnchorIds(options.anchorId) }
}

export { useSectionTable }
