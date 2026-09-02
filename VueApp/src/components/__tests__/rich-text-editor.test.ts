import { flushPromises, mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import { nextTick } from "vue"
import RichTextEditor from "@/components/RichTextEditor.vue"
import EditorImageDialog from "@/components/editor/EditorImageDialog.vue"
import EditorLinkDialog from "@/components/editor/EditorLinkDialog.vue"
// <script setup> exposes nothing to the type system, so the tests reach each dialog's refs through
// a narrowed view of the instance instead of `any`.
type LinkDialogVm = { address: string; text: string; newWindow: boolean }
type ImageDialogVm = { file: File | null; alt: string }

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
// Stub them for this file only, saving and restoring the originals so the patched globals don't
// leak into other test files sharing this environment.
const COMMAND_KEYS = [
    "execCommand",
    "queryCommandState",
    "queryCommandValue",
    "queryCommandSupported",
    "queryCommandEnabled",
] as const
const savedCommands: Record<string, unknown> = {}

beforeAll(() => {
    const d = document as unknown as Record<string, unknown>
    for (const key of COMMAND_KEYS) {
        savedCommands[key] = d[key]
    }
    d.execCommand = () => true
    d.queryCommandState = () => false
    d.queryCommandValue = () => ""
    d.queryCommandSupported = () => true
    d.queryCommandEnabled = () => true
})

afterAll(() => {
    const d = document as unknown as Record<string, unknown>
    for (const key of COMMAND_KEYS) {
        if (key in savedCommands && savedCommands[key] !== undefined) {
            d[key] = savedCommands[key]
        } else {
            // oxlint-disable-next-line typescript/no-dynamic-delete -- restoring document requires removing the key, not setting it undefined
            delete d[key]
        }
    }
})

const TOOLBAR = [
    ["bold", "italic"],
    // A dropdown token is an object, not a string; the definitions builder must skip it.
    [{ icon: "format_size", options: ["p", "h2"] }],
    ["viewsource"],
]

// The CMS-style toolbar, carrying every token that opens a dialog.
const FULL_TOOLBAR = [["bold"], ["link", "image", "table"], ["viewsource"]]

// Default to a name so mounts that don't care about the accessible name still satisfy the
// component's "labelId or ariaLabel required" invariant; name-specific tests override it.
// Attached to the document because the link handler reads document.getSelection(), which only sees
// nodes that are actually in the document, and because QDialog teleports its content to the body.
async function mountEditor(props: Record<string, unknown> = {}) {
    const wrapper = mount(RichTextEditor, {
        props: { modelValue: "<p>hi</p>", toolbar: TOOLBAR, ariaLabel: "Editor", ...props },
        global: { plugins: [[Quasar, {}]] },
        attachTo: document.body,
    })
    await flush()
    return wrapper
}

afterEach(() => {
    document.body.innerHTML = ""
})

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

/**
 * The link/image/table dialogs. QEditor's `definitions` are the whole contract here: a `handler`
 * replaces its internal runCmd call, so these tests read the definitions the component feeds
 * QEditor and drive each handler with a stubbed editor vm + caret, exactly as QEditor would.
 */

type Handler = (event: Event, vm: unknown, caret: unknown) => void
type Definition = { key?: unknown; handler?: Handler; disable?: (vm: unknown) => boolean }

function definitionsOf(wrapper: Awaited<ReturnType<typeof mountEditor>>) {
    return wrapper.findComponent({ name: "QEditor" }).props("definitions") as Record<string, Definition>
}

// QEditor hands a handler its own vm and caret; stub both so nothing reaches execCommand and every
// call the component makes is observable.
function fakeEditor() {
    const caret = {
        save: vi.fn<(range?: Range) => void>(),
        restore: vi.fn<() => void>(),
        hasParents: vi.fn<(list: string[], recursive?: boolean) => boolean>(() => false),
        range: null as Range | null,
        selection: null,
    }
    return { caret, vm: { runCmd: vi.fn<(cmd: string, param?: string) => void>(), caret } }
}

async function openViaHandler(wrapper: Awaited<ReturnType<typeof mountEditor>>, token: string) {
    const editor = fakeEditor()
    definitionsOf(wrapper)[token].handler?.(new MouseEvent("click"), editor.vm, editor.caret)
    await flush()
    return editor
}

// Submits through the real QForm so the dialogs' field rules run, as they do for a user.
async function submitDialog(wrapper: Awaited<ReturnType<typeof mountEditor>>) {
    await wrapper.findComponent({ name: "QForm" }).find("form").trigger("submit")
    await flushPromises()
    await nextTick()
}

test("the dialog-backed toolbar buttons carry accessible names", async () => {
    const wrapper = await mountEditor({ toolbar: FULL_TOOLBAR })

    const names = toolbarButtons(wrapper).map((button) => button.attributes("aria-label"))
    expect(names).toStrictEqual(expect.arrayContaining(["Insert link", "Insert image", "Insert table"]))
})

test("a toolbar without the link, image and table tokens gets no dialog wiring", async () => {
    // The CTS description editor's toolbar: defining these for an area that never asked for them
    // would put buttons on its toolbar that its content model has no room for.
    const definitions = definitionsOf(await mountEditor())

    expect(definitions.image).toBeUndefined()
    expect(definitions.table).toBeUndefined()
    expect(definitions.link).toBeUndefined()
})

test("the link definition unregisters Ctrl+L so the stock URL bar can't be reached", async () => {
    // A definition's `key` bypasses `handler` entirely - QEditor's shortcut map calls runCmd
    // directly - so leaving the built-in key in place would strand Ctrl+L on the old inline bar.
    const { link } = definitionsOf(await mountEditor({ toolbar: FULL_TOOLBAR }))

    expect(link.key).toBeNull()
    expect(link.handler).toBeTypeOf("function")
})

test("the link dialog edits the anchor at the caret and replaces it on submit", async () => {
    const wrapper = await mountEditor({ toolbar: FULL_TOOLBAR })
    const content = wrapper.find(".q-editor__content").element
    content.innerHTML = '<p>Go <a href="https://a.test">there</a></p>'

    const anchor = content.querySelector("a")!
    const textNode = anchor.firstChild as Text
    const caretRange = document.createRange()
    caretRange.setStart(textNode, 1)
    caretRange.collapse(true)
    const selection = document.getSelection()!
    selection.removeAllRanges()
    selection.addRange(caretRange)
    expect(selection.anchorNode).toBe(textNode)

    const editor = await openViaHandler(wrapper, "link")

    const dialog = wrapper.findComponent(EditorLinkDialog).vm as unknown as LinkDialogVm
    expect(dialog.address).toBe("https://a.test")
    expect(dialog.text).toBe("there")

    // Change only the checkbox: an untouched text field must keep the anchor's own markup rather
    // than have it rebuilt from escaped plain text.
    dialog.newWindow = true
    await submitDialog(wrapper)

    // Only a range handed to caret.save() survives runCmd's own caret.restore(), and only a range
    // bracketing the whole anchor makes insertHTML replace it instead of nesting inside it.
    const savedRange = editor.caret.save.mock.calls.at(-1)?.[0] as unknown as Range
    expect(savedRange.startContainer).toBe(anchor.parentNode)
    expect(savedRange.endContainer).toBe(anchor.parentNode)
    expect(savedRange.endOffset - savedRange.startOffset).toBe(1)
    expect(editor.vm.runCmd).toHaveBeenCalledWith(
        "insertHTML",
        '<a href="https://a.test" target="_blank" rel="noopener">there</a>',
    )
})

test("the table button reports whatever the caret says about being inside a table", async () => {
    const { table } = definitionsOf(await mountEditor({ toolbar: FULL_TOOLBAR }))
    const caret = { hasParents: vi.fn<(list: string[], recursive?: boolean) => boolean>(() => true) }

    expect(table.disable?.({ caret })).toBeTruthy()
    expect(caret.hasParents).toHaveBeenCalledWith(["table"], true)

    caret.hasParents.mockReturnValue(false)
    expect(table.disable?.({ caret })).toBeFalsy()
})

test("the image dialog uploads the chosen file and inserts the URL the upload resolved with", async () => {
    const uploadImage = vi.fn<(file: File) => Promise<string>>().mockResolvedValue("/2/CMS/Files?id=x")
    const wrapper = await mountEditor({ toolbar: FULL_TOOLBAR, uploadImage })
    const editor = await openViaHandler(wrapper, "image")

    const dialog = wrapper.findComponent(EditorImageDialog).vm as unknown as ImageDialogVm
    dialog.file = new File(["x"], "a.png", { type: "image/png" })
    dialog.alt = "A cat"
    await nextTick()
    await submitDialog(wrapper)

    expect(uploadImage).toHaveBeenCalledOnce()
    expect(editor.vm.runCmd).toHaveBeenCalledWith("insertHTML", '<img src="/2/CMS/Files?id=x" alt="A cat">')
})

test("a failed upload keeps the image dialog open with the server's message", async () => {
    const uploadImage = vi
        .fn<(file: File) => Promise<string>>()
        .mockRejectedValue(new Error("A file with the name a.png already exists."))
    const wrapper = await mountEditor({ toolbar: FULL_TOOLBAR, uploadImage })
    const editor = await openViaHandler(wrapper, "image")

    const dialog = wrapper.findComponent(EditorImageDialog).vm as unknown as ImageDialogVm
    dialog.file = new File(["x"], "a.png", { type: "image/png" })
    dialog.alt = "A cat"
    await nextTick()
    await submitDialog(wrapper)

    expect(document.body.textContent).toContain("A file with the name a.png already exists.")
    expect(document.querySelector(".q-dialog")).not.toBeNull()
    expect(editor.vm.runCmd).not.toHaveBeenCalled()
})

test("the image dialog offers no upload source when the caller can't upload", async () => {
    const wrapper = await mountEditor({ toolbar: FULL_TOOLBAR })
    await openViaHandler(wrapper, "image")

    expect(document.body.textContent).not.toContain("Upload a new image")
    expect(document.body.textContent).toContain("Image URL")
})

test("ctrl+K opens the link dialog only for a toolbar that has a link button", async () => {
    const withLink = await mountEditor({ toolbar: FULL_TOOLBAR })
    await withLink.find(".q-editor__content").trigger("keydown", { key: "k", ctrlKey: true })
    await flush()
    expect(withLink.findComponent(EditorLinkDialog).props("modelValue")).toBeTruthy()

    const withoutLink = await mountEditor()
    await withoutLink.find(".q-editor__content").trigger("keydown", { key: "k", ctrlKey: true })
    await flush()
    expect(withoutLink.findComponent(EditorLinkDialog).props("modelValue")).toBeFalsy()
})

test("a new link is inserted over a snapshot of the selection, not QEditor's live range", async () => {
    // QEditor hands out the live selection Range, and the browser rewrites that object as the user
    // clicks and types inside the dialog. Inserting over it put the link wherever the caret had
    // gone last, duplicating the selected word or landing at the start of the content. The range
    // also carries the trailing space a double-click selects; that must stay outside the link.
    const wrapper = await mountEditor({ toolbar: FULL_TOOLBAR })
    const content = wrapper.find(".q-editor__content").element
    content.innerHTML = "<p>Go there now</p>"
    const textNode = content.querySelector("p")!.firstChild!
    const live = document.createRange()
    live.setStart(textNode, 3)
    live.setEnd(textNode, 9)

    const editor = fakeEditor()
    editor.caret.range = live
    definitionsOf(wrapper).link.handler?.(new MouseEvent("click"), editor.vm, editor.caret)
    await flush()
    // What the browser does to the live range once focus is in the dialog.
    live.collapse(true)

    const dialog = wrapper.findComponent(EditorLinkDialog).vm as unknown as LinkDialogVm
    expect(dialog.text).toBe("there")
    dialog.address = "https://a.test"
    await nextTick()
    await submitDialog(wrapper)

    const saved = editor.caret.save.mock.calls.at(-1)?.[0] as unknown as Range
    expect(saved).not.toBe(live)
    expect([saved.startOffset, saved.endOffset]).toStrictEqual([3, 8])
    expect(editor.vm.runCmd).toHaveBeenCalledWith("insertHTML", '<a href="https://a.test">there</a>')
})
