import { mount, flushPromises } from "@vue/test-utils"
import { Quasar, Screen } from "quasar"
import ExportToolbar from "../ExportToolbar.vue"

/**
 * Tests for the export toolbar's phone layout. A row of export buttons plus the search overflows a
 * phone screen, so there the exports fold into one Export menu and the search takes its own line.
 */

function mountToolbar(xs: boolean) {
    Screen.xs = xs
    const excelExport = vi.fn<() => Promise<void>>().mockResolvedValue()
    const pdfExport = vi.fn<() => void>()
    const wrapper = mount(ExportToolbar, {
        props: { showSearch: true, excelExport, pdfExport },
        global: { plugins: [[Quasar, {}]] },
        attachTo: document.body,
    })
    return { wrapper, excelExport, pdfExport }
}

describe("export toolbar", () => {
    let unmount: (() => void) | undefined

    afterEach(() => {
        unmount?.()
        Screen.xs = false
    })

    it("shows each export as its own button on a wider screen", () => {
        expect.hasAssertions()
        const { wrapper } = mountToolbar(false)
        unmount = () => wrapper.unmount()

        expect(wrapper.find(".export-excel").exists()).toBeTruthy()
        expect(wrapper.find(".export-pdf").exists()).toBeTruthy()
        expect(wrapper.find(".export-menu").exists()).toBeFalsy()
    })

    it("folds the exports into one menu on a phone", () => {
        expect.hasAssertions()
        const { wrapper } = mountToolbar(true)
        unmount = () => wrapper.unmount()

        expect(wrapper.find(".export-menu").text()).toContain("Export")
        expect(wrapper.find(".export-excel").exists()).toBeFalsy()
    })

    it("runs an export chosen from the phone menu", async () => {
        expect.hasAssertions()
        const { wrapper, excelExport, pdfExport } = mountToolbar(true)
        unmount = () => wrapper.unmount()

        await wrapper.find(".export-menu").trigger("click")
        await flushPromises()
        // The menu is teleported to the body, outside the wrapper.
        document.body.querySelector<HTMLElement>(".q-item.export-excel")?.click()
        await flushPromises()

        expect(excelExport).toHaveBeenCalledWith()
        expect(pdfExport).not.toHaveBeenCalled()
    })

    it("puts the search on a line of its own on a phone", () => {
        expect.hasAssertions()
        const { wrapper } = mountToolbar(true)
        unmount = () => wrapper.unmount()

        expect(wrapper.find(".q-field").classes()).toContain("col-12")
        expect(wrapper.classes()).not.toContain("no-wrap")
    })

    it("keeps the search on the button row on a wider screen", () => {
        expect.hasAssertions()
        const { wrapper } = mountToolbar(false)
        unmount = () => wrapper.unmount()

        expect(wrapper.find(".q-field").classes()).not.toContain("col-12")
        expect(wrapper.classes()).toContain("no-wrap")
    })
})
