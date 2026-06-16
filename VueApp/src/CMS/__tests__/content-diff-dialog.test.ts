import ContentDiffDialog from "@/CMS/components/ContentDiffDialog.vue"
import { mountCms, flushPromises } from "./test-utils"

/**
 * ContentDiffDialog is a prop-driven presentational wrapper, so this stays light: it emits
 * update:modelValue on close, shows the added/removed legend only when there is a real
 * comparison with changes (and not loading), and falls back to emptyMessage when there is
 * no comparison. The dialog teleports, so assertions read document.body.
 */

function bodyText(): string {
    return document.body.textContent ?? ""
}

function mountDialog(props: Record<string, unknown>) {
    return mountCms(ContentDiffDialog, { props: { modelValue: true, ...props } })
}

describe("ContentDiffDialog.vue", () => {
    beforeEach(() => {
        document.body.innerHTML = ""
    })

    it("re-emits update:modelValue when the dialog requests close", async () => {
        const wrapper = mountDialog({})
        await flushPromises()
        wrapper.findComponent({ name: "QDialog" }).vm.$emit("update:modelValue", false)
        await flushPromises()
        expect(wrapper.emitted("update:modelValue")?.at(-1)).toEqual([false])
    })

    it("shows the added/removed legend when there is a comparison with changes and not loading", async () => {
        mountDialog({ hasComparison: true, hasChanges: true, loading: false, diffHtml: "<ins>x</ins>" })
        await flushPromises()
        expect(bodyText()).toContain("added")
        expect(bodyText()).toContain("removed")
    })

    it("hides the legend while loading", async () => {
        mountDialog({ hasComparison: true, hasChanges: true, loading: true })
        await flushPromises()
        expect(bodyText()).not.toContain("(newer)")
    })

    it("hides the legend and states identical when there are no changes", async () => {
        mountDialog({ hasComparison: true, hasChanges: false })
        await flushPromises()
        expect(bodyText()).not.toContain("(newer)")
        expect(bodyText()).toContain("These two versions are identical.")
    })

    it("falls back to the empty message when there is no comparison", async () => {
        mountDialog({ hasComparison: false, emptyMessage: "Nothing to compare here." })
        await flushPromises()
        expect(bodyText()).toContain("Nothing to compare here.")
        // No legend without a comparison.
        expect(bodyText()).not.toContain("(newer)")
    })

    it("renders the provided subtitle and title", async () => {
        mountDialog({ title: "Diff title", subtitle: "Changes from A to B" })
        await flushPromises()
        expect(bodyText()).toContain("Diff title")
        expect(bodyText()).toContain("Changes from A to B")
    })
})
