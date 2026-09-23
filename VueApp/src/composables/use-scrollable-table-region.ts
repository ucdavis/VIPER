import { onMounted, onUpdated } from "vue"
import type { Ref } from "vue"
import type { QTable } from "quasar"

/**
 * Makes a q-table's horizontal scroll container reachable by keyboard, and gives it a name.
 *
 * A region that scrolls has to be focusable, or a keyboard user cannot reach the columns sitting
 * off the right-hand edge.
 *
 * Applied on update as well as on mount, because the container is rebuilt whenever the table
 * re-renders.
 *
 * Matched on `.scroll` rather than on the container alone so a grid of cards
 * still handles properly.
 */
export function useScrollableTableRegion(tableRef: Ref<QTable | null | undefined>, label: string): void {
    function applyScrollRegion(): void {
        const root = tableRef.value?.$el as HTMLElement | undefined | null
        const scroller = root?.querySelector<HTMLElement>(".q-table__middle.scroll")
        if (!scroller || scroller.getAttribute("aria-label") === label) {
            return
        }

        scroller.tabIndex = 0
        scroller.setAttribute("role", "region")
        scroller.setAttribute("aria-label", label)
    }

    onMounted(applyScrollRegion)
    onUpdated(applyScrollRegion)
}
