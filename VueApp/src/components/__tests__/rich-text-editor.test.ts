import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import { nextTick } from "vue"
import RichTextEditor from "@/components/RichTextEditor.vue"

// QEditor renders its toolbar buttons only after a deferred (setTimeout-based) refresh, so let a
// real macrotask elapse before reading the toolbar, then settle the resulting re-render.
async function flush() {
    await new Promise((resolve) => {
        setTimeout(resolve, 5)
    })
    await nextTick()
}

/**
 * RichTextEditor wraps Quasar's QEditor to add the accessibility and source-view behaviour QEditor
 * lacks out of the box: an accessible name on every icon toolbar button, a "view source" button
 * whose tooltip describes the action rather than the current mode, and an accessible name on the
 * contenteditable region. These tests mount the real QEditor (the behaviour is entirely in how the
 * component feeds QEditor its definitions and wires the content element).
 */

// QEditor probes rich-text support during setup; happy-dom ships none of the execCommand family.
beforeAll(() => {
    const d = document as unknown as Record<string, unknown>
    d.execCommand = () => true
    d.queryCommandState = () => false
    d.queryCommandValue = () => ""
    d.queryCommandSupported = () => true
    d.queryCommandEnabled = () => true
})

const TOOLBAR = [
    ["bold", "italic"],
    // A dropdown token is an object, not a string; the definitions builder must skip it.
    [{ icon: "format_size", options: ["p", "h2"] }],
    ["viewsource"],
]

async function mountEditor(props: Record<string, unknown> = {}) {
    const wrapper = mount(RichTextEditor, {
        props: { modelValue: "<p>hi</p>", toolbar: TOOLBAR, ...props },
        global: { plugins: [[Quasar, {}]] },
    })
    await flush()
    return wrapper
}

function toolbarButtons(wrapper: Awaited<ReturnType<typeof mountEditor>>) {
    return wrapper.findAll(".q-editor__toolbar .q-btn")
}

// In source mode QEditor hides every button whose cmd isn't "viewsource", so the toolbar collapsing
// to a single button is what "we're in source view" looks like.
async function clickSourceButton(wrapper: Awaited<ReturnType<typeof mountEditor>>) {
    await toolbarButtons(wrapper).at(-1)?.trigger("click")
    await flush()
}

test("every icon toolbar button gets its tooltip as an accessible name", async () => {
    const wrapper = await mountEditor()

    // Without label: null in the definitions, QEditor renders these icon buttons with no aria-label
    // at all, so a screen reader announces each as an unlabeled button.
    for (const [icon, name] of [
        ["format_bold", "Bold"],
        ["code", "Edit HTML source"],
    ] as const) {
        const button = toolbarButtons(wrapper).find((b) => b.find(".q-icon").text() === icon)
        expect(button?.attributes("aria-label")).toBe(name)
    }
})

test("the view-source button describes the action, survives source mode, and toggles back", async () => {
    const wrapper = await mountEditor()
    expect(wrapper.find(".q-editor").classes()).toContain("q-editor--default")

    await clickSourceButton(wrapper)

    // Dropping cmd: "viewsource" from the override would filter the button out here, stranding the
    // user in source view. Its accessible name tracks the tooltip, so it flips too.
    const inSource = toolbarButtons(wrapper)
    expect(inSource).toHaveLength(1)
    expect(inSource[0].attributes("aria-label")).toBe("Back to formatted view")
    expect(inSource[0].classes()).toContain("text-primary")
    expect(wrapper.find(".q-editor").classes()).toContain("q-editor--source")

    await clickSourceButton(wrapper)
    expect(toolbarButtons(wrapper).length).toBeGreaterThan(1)
    expect(wrapper.find(".q-editor").classes()).toContain("q-editor--default")
})

test("names the contenteditable region via aria-label", async () => {
    const wrapper = await mountEditor({ ariaLabel: "EPA description" })
    expect(wrapper.find(".q-editor__content").attributes("aria-label")).toBe("EPA description")
})

test("names the contenteditable region via aria-labelledby when a label id is given", async () => {
    const wrapper = await mountEditor({ labelId: "content-editor-label", ariaLabel: "ignored" })
    const content = wrapper.find(".q-editor__content")
    // The labelId wins over ariaLabel: an existing visible label is the better accessible name.
    expect(content.attributes("aria-labelledby")).toBe("content-editor-label")
    expect(content.attributes("aria-label")).toBeUndefined()
})
