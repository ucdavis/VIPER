import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import { h, nextTick, ref } from "vue"
import SortableList from "@/components/SortableList.vue"

/**
 * SortableList owns the shared reorder shell: drag handle, move buttons, the screen-reader
 * announcement, and the row/header/actions/empty slots. The drag itself comes from
 * vue-draggable (SortableJS), which needs real pointer events, so it's stubbed to a
 * passthrough here; the button-move path (which is what these tests drive) runs through
 * useReorder directly and is fully exercised.
 */
vi.mock("vue-draggable-plus", () => ({
    VueDraggable: { name: "VueDraggable", template: "<div><slot /></div>" },
}))

type Item = { id: number; label: string }

function mountList(initial: Item[], extraProps: Record<string, unknown> = {}) {
    const model = ref(initial)
    const wrapper = mount(SortableList, {
        props: {
            modelValue: model.value,
            itemKey: "id",
            "onUpdate:modelValue": (v) => {
                model.value = v as Item[]
            },
            ...extraProps,
        },
        global: { plugins: [[Quasar, {}]] },
        slots: {
            row: (params) => h("span", { class: "row-label" }, params.item.label),
            actions: (params) => h("button", { class: "del" }, `del ${params.item.id}`),
            empty: () => h("div", { class: "empty" }, "Nothing here yet"),
            header: () => h("div", { class: "hdr" }, "Label"),
        },
    })
    return { wrapper, model }
}

const rowText = (wrapper: ReturnType<typeof mountList>["wrapper"]) => wrapper.findAll(".row-label").map((n) => n.text())

const threeItems = (): Item[] => [
    { id: 1, label: "Alpha" },
    { id: 2, label: "Bravo" },
    { id: 3, label: "Charlie" },
]

describe("SortableList - rendering", () => {
    it("renders one row per item through the row slot", () => {
        const { wrapper } = mountList(threeItems())
        expect(rowText(wrapper)).toEqual(["Alpha", "Bravo", "Charlie"])
    })

    it("renders the actions slot per row", () => {
        const { wrapper } = mountList(threeItems())
        expect(wrapper.findAll(".del")).toHaveLength(3)
    })

    it("renders the header slot", () => {
        const { wrapper } = mountList(threeItems())
        expect(wrapper.find(".hdr").exists()).toBeTruthy()
    })

    it("shows the empty slot only when the list is empty", () => {
        const { wrapper: full } = mountList(threeItems())
        expect(full.find(".empty").exists()).toBeFalsy()

        const { wrapper: empty } = mountList([])
        expect(empty.find(".empty").exists()).toBeTruthy()
    })
})

describe("SortableList - move buttons", () => {
    it("disables Move up on the first row and Move down on the last", () => {
        const { wrapper } = mountList(threeItems())
        const ups = wrapper.findAll('[aria-label="Move up"]')
        const downs = wrapper.findAll('[aria-label="Move down"]')
        expect(ups[0]!.attributes("disabled")).toBeDefined()
        expect(ups[1]!.attributes("disabled")).toBeUndefined()
        expect(downs[2]!.attributes("disabled")).toBeDefined()
        expect(downs[1]!.attributes("disabled")).toBeUndefined()
    })

    it("moving a row down reorders the list and emits reorder", async () => {
        const { wrapper } = mountList(threeItems())
        await wrapper.findAll('[aria-label="Move down"]')[0]!.trigger("click")

        expect(rowText(wrapper)).toEqual(["Bravo", "Alpha", "Charlie"])
        const events = wrapper.emitted("reorder")
        expect(events).toHaveLength(1)
        expect(events![0]).toEqual([{ id: 1, label: "Alpha" }, 1, 0])
    })

    it("moving a row up reorders the list", async () => {
        const { wrapper } = mountList(threeItems())
        await wrapper.findAll('[aria-label="Move up"]')[2]!.trigger("click")
        expect(rowText(wrapper)).toEqual(["Alpha", "Charlie", "Bravo"])
    })

    it("uses custom move-button labels when provided", () => {
        const { wrapper } = mountList(threeItems(), {
            moveUpLabel: "Move link up",
            moveDownLabel: "Move link down",
        })
        expect(wrapper.find('[aria-label="Move link up"]').exists()).toBeTruthy()
        expect(wrapper.find('[aria-label="Move link down"]').exists()).toBeTruthy()
    })
})

describe("SortableList - drag commit", () => {
    it("announces and emits reorder when a drag ends (the array is already reordered by SortableJS)", async () => {
        const { wrapper } = mountList(threeItems())

        wrapper.findComponent({ name: "VueDraggable" }).vm.$emit("end", { oldIndex: 0, newIndex: 2 })
        await nextTick()
        await nextTick()

        // CommitDrag doesn't move anything itself; it reports the item now sitting at newIndex.
        const events = wrapper.emitted("reorder")
        expect(events).toHaveLength(1)
        expect(events![0]).toEqual([{ id: 3, label: "Charlie" }, 2, 0])
        expect(wrapper.find('[role="status"]').text()).toBe("Moved to position 3 of 3")
    })

    it("ignores a drag end without indices (cancelled drag)", async () => {
        const { wrapper } = mountList(threeItems())

        wrapper.findComponent({ name: "VueDraggable" }).vm.$emit("end", {})
        await nextTick()

        expect(wrapper.emitted("reorder")).toBeFalsy()
    })

    it("ignores a drag that ends where it started", async () => {
        const { wrapper } = mountList(threeItems())

        wrapper.findComponent({ name: "VueDraggable" }).vm.$emit("end", { oldIndex: 1, newIndex: 1 })
        await nextTick()

        expect(wrapper.emitted("reorder")).toBeFalsy()
    })
})

describe("SortableList - accessibility", () => {
    it("announces the move in a polite live region", async () => {
        const { wrapper } = mountList(threeItems())
        await wrapper.findAll('[aria-label="Move down"]')[0]!.trigger("click")
        await nextTick()
        await nextTick()

        const live = wrapper.find('[role="status"]')
        expect(live.attributes("aria-live")).toBe("polite")
        expect(live.text()).toBe("Moved to position 2 of 3")
    })

    it("uses a custom announce builder when provided", async () => {
        const { wrapper } = mountList(threeItems(), {
            announce: (item: Item, newIndex: number, total: number) => `${item.label} is now ${newIndex + 1}/${total}`,
        })
        await wrapper.findAll('[aria-label="Move down"]')[0]!.trigger("click")
        await nextTick()
        await nextTick()
        expect(wrapper.find('[role="status"]').text()).toBe("Alpha is now 2/3")
    })

    it("hides the drag handle from assistive tech (move buttons are the a11y path)", () => {
        const { wrapper } = mountList(threeItems())
        // The header's handle is a layout spacer; the per-row handles are the real affordance.
        const handle = wrapper.find(".sortable-list__items .sortable-row__handle")
        expect(handle.attributes("aria-hidden")).toBe("true")
    })
})
