import { ref } from "vue"
import { useReorder } from "@/composables/use-reorder"

/**
 * Unit tests for useReorder: the array move math and the transient "just moved" highlight
 * shared by every sortable list, exercised without a DOM the way SortableList drives it
 * (button moves) and the way vue-draggable drives it (commitDrag after a drop).
 */

type Item = { id: number; name: string }
type ReorderCallback = (item: Item, newIndex: number, oldIndex: number) => void

function makeList(): Item[] {
    return [
        { id: 1, name: "a" },
        { id: 2, name: "b" },
        { id: 3, name: "c" },
    ]
}

const ids = (list: Item[]) => list.map((i) => i.id)

describe("useReorder - move math", () => {
    it("moveDown swaps an item with its successor", () => {
        const list = ref(makeList())
        const { moveDown } = useReorder(list, { getKey: (i) => i.id })
        expect(moveDown(0)).toBeTruthy()
        expect(ids(list.value)).toEqual([2, 1, 3])
    })

    it("moveUp swaps an item with its predecessor", () => {
        const list = ref(makeList())
        const { moveUp } = useReorder(list, { getKey: (i) => i.id })
        expect(moveUp(2)).toBeTruthy()
        expect(ids(list.value)).toEqual([1, 3, 2])
    })

    it("ignores moveUp on the first item and moveDown on the last", () => {
        const list = ref(makeList())
        const { moveUp, moveDown } = useReorder(list, { getKey: (i) => i.id })
        expect(moveUp(0)).toBeFalsy()
        expect(moveDown(2)).toBeFalsy()
        expect(ids(list.value)).toEqual([1, 2, 3])
    })

    it("ignores out-of-range indices", () => {
        const list = ref(makeList())
        const { moveUp, moveDown } = useReorder(list, { getKey: (i) => i.id })
        expect(moveDown(5)).toBeFalsy()
        expect(moveUp(-1)).toBeFalsy()
        expect(ids(list.value)).toEqual([1, 2, 3])
    })

    it("mutates the same array reference so a parent v-model stays in sync", () => {
        const list = ref(makeList())
        const original = list.value
        const { moveDown } = useReorder(list, { getKey: (i) => i.id })
        moveDown(0)
        expect(list.value).toBe(original)
    })
})

describe("useReorder - onReorder callback", () => {
    it("fires with the moved item and its new/old indices", () => {
        const list = ref(makeList())
        const onReorder = vi.fn<ReorderCallback>()
        const { moveDown } = useReorder(list, { getKey: (i) => i.id, onReorder })
        moveDown(0)
        expect(onReorder).toHaveBeenCalledExactlyOnceWith({ id: 1, name: "a" }, 1, 0)
    })

    it("does not fire on a no-op move", () => {
        const list = ref(makeList())
        const onReorder = vi.fn<ReorderCallback>()
        const { moveUp } = useReorder(list, { getKey: (i) => i.id, onReorder })
        moveUp(0)
        expect(onReorder).not.toHaveBeenCalled()
    })

    it("commitDrag fires for the dropped row and skips a same-slot drop", () => {
        const list = ref(makeList())
        const onReorder = vi.fn<ReorderCallback>()
        const { commitDrag } = useReorder(list, { getKey: (i) => i.id, onReorder })

        // After a drop, vue-draggable has already reordered the array; here item 1
        // was dragged from index 0 to index 2.
        list.value = [
            { id: 2, name: "b" },
            { id: 3, name: "c" },
            { id: 1, name: "a" },
        ]
        commitDrag(0, 2)
        expect(onReorder).toHaveBeenCalledWith({ id: 1, name: "a" }, 2, 0)

        onReorder.mockClear()
        commitDrag(1, 1)
        expect(onReorder).not.toHaveBeenCalled()
    })
})

describe("useReorder - flash highlight", () => {
    beforeEach(() => vi.useFakeTimers())
    afterEach(() => vi.useRealTimers())

    it("marks the moved item, then clears it after flashMs", () => {
        const list = ref(makeList())
        const { moveDown, justMovedKey } = useReorder(list, { getKey: (i) => i.id, flashMs: 600 })
        moveDown(0)
        expect(justMovedKey.value).toBe(1)
        vi.advanceTimersByTime(599)
        expect(justMovedKey.value).toBe(1)
        vi.advanceTimersByTime(1)
        expect(justMovedKey.value).toBeNull()
    })

    it("a second move resets the clear timer", () => {
        const list = ref(makeList())
        const { moveDown, justMovedKey } = useReorder(list, { getKey: (i) => i.id, flashMs: 600 })
        moveDown(0) // [2,1,3] flashes id 1
        vi.advanceTimersByTime(400)
        moveDown(1) // [2,3,1] flashes id 1 again, timer reset
        expect(justMovedKey.value).toBe(1)
        vi.advanceTimersByTime(400) // 800 since first flash but only 400 since second
        expect(justMovedKey.value).toBe(1)
        vi.advanceTimersByTime(200)
        expect(justMovedKey.value).toBeNull()
    })
})
