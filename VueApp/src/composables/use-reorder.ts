import type { Ref } from "vue"
import { ref } from "vue"

type ReorderKey = string | number

const DEFAULT_FLASH_MS = 600

interface UseReorderOptions<T> {
    /** Stable, unique key for an item. Drives the post-move highlight. */
    getKey: (item: T) => ReorderKey
    /**
     * Called after any committed reorder (button move or drag drop), with the moved
     * item and its new/old indices. Use it to persist order immediately; omit it for
     * lists that batch their order into an explicit save. Fire-and-forget: an async
     * handler runs unawaited, so it owns its own error handling.
     */
    onReorder?: (item: T, newIndex: number, oldIndex: number) => void
    /** Highlight duration in ms; keep in sync with the CSS flash animation. */
    flashMs?: number
}

/**
 * Reorder logic shared by every sortable list in the app: the up/down move math
 * and the transient "just moved" highlight. The list is mutated in place so it
 * stays the same array reference a parent `v-model` and vue-draggable both bind to.
 *
 * Pairs with SortableList.vue, which owns the drag wiring, controls, and a11y;
 * exposed on its own so the move behaviour can be unit-tested without a DOM.
 */
function useReorder<T>(list: Ref<T[]>, options: UseReorderOptions<T>) {
    const { getKey, onReorder, flashMs = DEFAULT_FLASH_MS } = options

    const justMovedKey = ref<ReorderKey | null>(null)
    // A generation counter supersedes earlier flash timers instead of clearing them, so
    // a rapid second move's highlight wins without tracking a mutable timer handle.
    let flashGeneration = 0

    function flash(key: ReorderKey) {
        justMovedKey.value = key
        const generation = (flashGeneration += 1)
        setTimeout(() => {
            if (flashGeneration === generation && justMovedKey.value === key) {
                justMovedKey.value = null
            }
        }, flashMs)
    }

    function move(from: number, to: number): boolean {
        const items = list.value
        if (from === to || from < 0 || to < 0 || from >= items.length || to >= items.length) {
            return false
        }
        const item = items[from]
        if (item === undefined) {
            return false
        }
        items.splice(from, 1)
        items.splice(to, 0, item)
        flash(getKey(item))
        onReorder?.(item, to, from)
        return true
    }

    function moveUp(index: number): boolean {
        return move(index, index - 1)
    }

    function moveDown(index: number): boolean {
        return move(index, index + 1)
    }

    /**
     * Finish a vue-draggable drag: the bound array is already reordered by the time
     * its `end` event fires, so only flash the dropped row and fire `onReorder`.
     */
    function commitDrag(oldIndex: number, newIndex: number) {
        if (oldIndex === newIndex) {
            return
        }
        const item = list.value[newIndex]
        if (item === undefined) {
            return
        }
        flash(getKey(item))
        onReorder?.(item, newIndex, oldIndex)
    }

    return { justMovedKey, move, moveUp, moveDown, commitDrag }
}

export { useReorder, type ReorderKey }
