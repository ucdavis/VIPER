import { mount } from "@vue/test-utils"
import { defineComponent, useTemplateRef } from "vue"
import { Quasar, QTable } from "quasar"
import { useScrollableTableRegion } from "../use-scrollable-table-region"

/**
 * Tests for the q-table scroll-region helper. A region that scrolls has to be focusable and
 * named, and Quasar renders that container itself, so the attributes are set on the element by
 * hand. These cover the two ways that goes wrong: the container not existing yet on mount, and
 * it not existing at all.
 */

type Row = { id: number; name: string }

const columns = [{ name: "name", label: "Name", field: "name", align: "left" as const }]

const Harness = defineComponent({
    // Registered by hand: Quasar's auto-import is a build-time transform, so it does not reach a
    // string template compiled in a test.
    components: { QTable },
    props: {
        rows: { type: Array<Row>, required: true },
        grid: { type: Boolean, default: false },
    },
    setup() {
        const tableRef = useTemplateRef<QTable>("tableRef")
        useScrollableTableRegion(tableRef, "Test table")
        return { columns }
    },
    template: `<q-table ref="tableRef" :rows="rows" :columns="columns" :grid="grid" row-key="id" />`,
})

function mountTable(rows: Row[] = [], grid = false) {
    return mount(Harness, {
        props: { rows, grid },
        global: { plugins: [[Quasar, {}]] },
    })
}

describe("scrollable table region", () => {
    it("names the scroll container and puts it in the tab order", () => {
        expect.hasAssertions()
        const scroller = mountTable([{ id: 1, name: "Anne" }]).get(".q-table__middle")

        expect(scroller.attributes("role")).toBe("region")
        expect(scroller.attributes("aria-label")).toBe("Test table")
        expect(scroller.attributes("tabindex")).toBe("0")
    })

    it("still applies once rows arrive, not only on mount", async () => {
        expect.hasAssertions()
        // The rows are fetched, so the table a page mounts is empty and re-renders later.
        const wrapper = mountTable([])
        await wrapper.setProps({ rows: [{ id: 1, name: "Anne" }] })

        expect(wrapper.get(".q-table__middle").attributes("aria-label")).toBe("Test table")
    })

    it("leaves the grid-mode container alone, since a grid of cards does not scroll", () => {
        expect.hasAssertions()
        // Quasar still renders q-table__middle for a grid, just without the scroll class. Naming
        // it there would be a tab stop that goes nowhere.
        const container = mountTable([{ id: 1, name: "Anne" }], true).get(".q-table__middle")

        expect(container.classes()).not.toContain("scroll")
        expect(container.attributes("role")).toBeUndefined()
        expect(container.attributes("tabindex")).toBeUndefined()
    })
})
