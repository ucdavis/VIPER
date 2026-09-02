<template>
    <q-editor
        ref="editorRef"
        :model-value="modelValue"
        :toolbar="toolbar"
        :definitions="editorDefinitions"
        v-bind="$attrs"
        @update:model-value="(value: string) => emit('update:modelValue', value)"
        @keydown="onKeydown"
    />

    <EditorLinkDialog
        v-model="linkDialogOpen"
        :existing="linkExisting"
        :selection-text="linkSelectionText"
        @submit="onLinkSubmit"
        @remove="onLinkRemove"
    />

    <EditorImageDialog
        v-model="imageDialogOpen"
        :options="imageOptions ?? []"
        :options-hint="imageOptionsHint"
        :upload-image="uploadImage"
        :upload-unavailable-hint="uploadUnavailableHint"
        @submit="onImageSubmit"
    />

    <EditorTableDialog
        v-model="tableDialogOpen"
        @submit="onTableSubmit"
    />
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, ref, watch } from "vue"
import { useQuasar } from "quasar"
import EditorLinkDialog from "@/components/editor/EditorLinkDialog.vue"
import EditorImageDialog from "@/components/editor/EditorImageDialog.vue"
import EditorTableDialog from "@/components/editor/EditorTableDialog.vue"
import { buildImageHtml, buildLinkHtml, buildTableHtml, parseLinkHref } from "@/components/editor/editor-html"
import type { LinkKind } from "@/components/editor/editor-html"

/**
 * Shared rich-text (HTML) editor wrapping Quasar's QEditor. Centralizes the accessibility and
 * source-view behaviour that QEditor doesn't provide out of the box, so every area that embeds an
 * editor gets it for free instead of re-solving (or forgetting) it:
 *  - Accessible names on every icon toolbar button. QEditor leaves each button's `label` undefined,
 *    which yields no aria-label at all; passing `label: null` opts each button into QEditor's
 *    aria-label = tooltip fallback.
 *  - A "view source" button whose tooltip/name describes the action ("Edit HTML source" /
 *    "Back to formatted view") rather than QEditor's static "View Source", which reads as the
 *    current state once every other button hides in source mode.
 *  - An accessible name on the contenteditable region itself (QEditor renders it as an inner
 *    element, so the name has to be set there), via `aria-label` or `aria-labelledby`.
 *  - Dialogs for the `link`, `image` and `table` toolbar tokens, replacing QEditor's inline URL bar
 *    (which can't set link text, mailto/tel, or target) and adding the two commands it has no
 *    button for at all. All three are opt-in by token: a toolbar without them is untouched.
 *
 * Callers pass their own `toolbar` (button sets differ by area) and an accessible name; all other
 * QEditor props (min-height, dense, outlined, class, ...) fall through via $attrs.
 */

const props = defineProps<{
    modelValue: string
    toolbar: (string | Record<string, unknown>)[][]
    /** Accessible name for the editing area. Prefer `labelId` when a visible label already exists. */
    ariaLabel?: string
    /** id of an existing visible label, wired as aria-labelledby on the editing area. */
    labelId?: string
    /** Images already attached to the record, offered by the insert-image dialog. */
    imageOptions?: { label: string; value: string }[]
    /** Explains where `imageOptions` comes from, shown in the insert-image dialog. */
    imageOptionsHint?: string
    /** Uploads an image and resolves with its URL. Omit to hide the dialog's upload source. */
    uploadImage?: (file: File) => Promise<string>
    /** Shown in place of the upload source when `uploadImage` is omitted. */
    uploadUnavailableHint?: string
}>()

// Every caller must give the editing area an accessible name (a QEditor with none is invisible to
// screen readers): pass `labelId` (preferred, the id of a visible label) or `ariaLabel`. This can't
// be a type-level union - that stops vue-tsc mapping the kebab-case `aria-label` attribute callers
// use onto the prop - so enforce it as a dev-only invariant instead. It compiles out of production
// and never fires for a compliant caller; passing both is fine, `labelId` wins in applyAccessibleName.
if (import.meta.env.DEV && !props.labelId && !props.ariaLabel) {
    throw new Error("RichTextEditor requires a `labelId` or `ariaLabel` so the editing area is announced.")
}

const emit = defineEmits<{
    "update:modelValue": [value: string]
}>()

// The variable QEditor props (min-height, dense, class, ...) are forwarded through $attrs rather
// than landing on the root by default, so the accessible-name props above can't collide with them.
defineOptions({ inheritAttrs: false })

/** The slice of QEditor's Caret and component instance the dialog handlers need. */
type EditorCaret = {
    save: (range?: Range) => void
    readonly range: Range | null
    hasParents: (list: string[], recursive?: boolean) => boolean
}
type EditorVm = { runCmd: (cmd: string, param?: string) => void; caret?: EditorCaret }

const $q = useQuasar()
const editorRef = ref()
const viewingSource = ref(false)

const linkDialogOpen = ref(false)
const imageDialogOpen = ref(false)
const tableDialogOpen = ref(false)
const linkExisting = ref<{ kind: LinkKind; address: string; text: string; newWindow: boolean } | null>(null)
const linkSelectionText = ref("")

// Held from the handler call until the dialog submits a tick or more later. Not reactive: nothing
// renders from them, and the anchor is a live DOM node we only want to point a Range at.
// `savedRange` is a clone on purpose: QEditor keeps the live selection Range, which the browser
// rewrites as the user clicks and types inside the dialog, so the live one is useless by submit.
let activeVm: EditorVm | null = null
let activeCaret: EditorCaret | null = null
let existingAnchor: HTMLAnchorElement | null = null
let savedRange: Range | null = null
let selectionHtml = ""

const hasLinkButton = computed(() => props.toolbar.some((group) => group.includes("link")))

const editorDefinitions = computed(() => {
    const definitions: Record<string, Record<string, unknown>> = {}
    for (const group of props.toolbar) {
        for (const token of group) {
            // Dropdown tokens are objects (e.g. the heading picker) and aren't addressable by name;
            // only the plain string command buttons take a definition override.
            if (typeof token === "string") definitions[token] = { label: null }
        }
    }
    if (definitions.viewsource) {
        definitions.viewsource = {
            cmd: "viewsource",
            label: null,
            icon: $q.iconSet.editor.viewSource,
            tip: viewingSource.value ? "Back to formatted view" : "Edit HTML source",
            // `handler` replaces QEditor's internal runCmd call, so we drive it ourselves to keep
            // our mode flag in step. `cmd` must stay "viewsource" or the toolbar filters the button
            // out in source mode, leaving no way back.
            handler: () => {
                editorRef.value?.runCmd("viewsource")
                viewingSource.value = !viewingSource.value
            },
        }
    }
    if (definitions.link) {
        // Deep-merged over QEditor's own `link` definition, so its icon and disable rule survive.
        // `key: null` unregisters the Ctrl+L shortcut, which bypasses `handler` entirely and would
        // otherwise still open the stock URL bar this dialog replaces.
        definitions.link = {
            label: null,
            key: null,
            tip: "Insert link",
            handler: (_event: Event, vm: EditorVm, caret: EditorCaret) => openLinkDialog(vm, caret),
        }
    }
    // `image` and `table` are our own tokens with no QEditor built-in behind them. Leaving `cmd`
    // unset keeps QEditor from wrapping them as toggle buttons (it would call caret.is() with an
    // unknown command); the source-mode filter hides every button without cmd "viewsource" anyway.
    if (definitions.image) {
        definitions.image = {
            label: null,
            icon: "image",
            tip: "Insert image",
            handler: (_event: Event, vm: EditorVm, caret: EditorCaret) => openDialog(imageDialogOpen, vm, caret),
        }
    }
    if (definitions.table) {
        definitions.table = {
            label: null,
            icon: "table_chart",
            tip: "Insert table",
            handler: (_event: Event, vm: EditorVm, caret: EditorCaret) => openDialog(tableDialogOpen, vm, caret),
            // Nested tables are a formatting dead end in contenteditable, so the button greys out
            // once the caret is inside one. Re-evaluated on every toolbar refresh.
            disable: (vm: EditorVm) => !!vm.caret?.hasParents(["table"], true),
        }
    }
    return definitions
})

/** Snapshot the caret so the dialog's later insert lands where the user was, then open the dialog. */
function openDialog(open: typeof imageDialogOpen, vm: EditorVm, caret: EditorCaret | undefined) {
    if (!caret) return
    activeVm = vm
    activeCaret = caret
    savedRange = caret.range?.cloneRange() ?? null
    open.value = true
}

/**
 * Shrink a selection to its non-blank content. A double-click in Chrome selects the word plus its
 * trailing space, and wrapping that puts the space inside the link and the next word flush against it.
 */
function trimRange(range: Range) {
    const start = range.startContainer
    const end = range.endContainer
    if (end.nodeType === Node.TEXT_NODE) {
        const text = end.textContent ?? ""
        let offset = range.endOffset
        while (offset > 0 && /\s/u.test(text[offset - 1]) && !(end === start && offset <= range.startOffset)) offset--
        range.setEnd(end, offset)
    }
    if (start.nodeType === Node.TEXT_NODE) {
        const text = start.textContent ?? ""
        let offset = range.startOffset
        while (offset < text.length && /\s/u.test(text[offset]) && !(start === end && offset >= range.endOffset))
            offset++
        range.setStart(start, offset)
    }
}

/** The <a> the caret sits in, or null. QEditor only checks the caret's immediate parent. */
function anchorAtCaret(): HTMLAnchorElement | null {
    const selection = document.getSelection()
    const node = selection?.anchorNode
    if (!node) return null
    const element = node.nodeType === Node.TEXT_NODE ? node.parentElement : (node as Element)
    const anchor = element?.closest?.("a") ?? null
    const contentEl = editorRef.value?.getContentEl()
    return anchor && contentEl?.contains(anchor) ? anchor : null
}

function openLinkDialog(vm: EditorVm, caret: EditorCaret | undefined) {
    if (!caret) return
    activeVm = vm
    activeCaret = caret
    existingAnchor = anchorAtCaret()
    selectionHtml = ""
    linkSelectionText.value = ""

    if (existingAnchor) {
        linkExisting.value = {
            ...parseLinkHref(existingAnchor.getAttribute("href") ?? ""),
            text: existingAnchor.textContent ?? "",
            newWindow: existingAnchor.target === "_blank",
        }
    } else {
        linkExisting.value = null
        savedRange = caret.range?.cloneRange() ?? null
        if (savedRange && !savedRange.collapsed) {
            trimRange(savedRange)
            linkSelectionText.value = savedRange.toString()
            // Keep the selection's markup so an unedited link text stays formatted rather than
            // being flattened to escaped plain text by the builder.
            const holder = document.createElement("div")
            holder.append(savedRange.cloneContents())
            selectionHtml = holder.innerHTML
        }
    }
    linkDialogOpen.value = true
}

/** A range around the whole anchor, so insertHTML replaces it instead of nesting a new <a> inside. */
function anchorRange(): Range | null {
    if (!existingAnchor) return null
    const range = document.createRange()
    range.selectNode(existingAnchor)
    return range
}

/**
 * Close the dialog, then run the editor command over `range`. The await matters: QDialog keeps its
 * focus trap until the hide runs on the next tick, and runCmd focuses the editor before it works,
 * so running synchronously lets the dialog pull focus straight back and execCommand then lands on
 * whatever selection the browser last had. An explicit caret.save(range) is the only thing
 * runCmd's own caret.restore() honours, which is why the range is handed over here and not earlier.
 */
async function closeThenRun(open: typeof linkDialogOpen, range: Range | null, cmd: string, param?: string) {
    open.value = false
    await nextTick()
    if (range) activeCaret?.save(range)
    activeVm?.runCmd(cmd, param)
}

function onLinkSubmit(value: {
    kind: LinkKind
    address: string
    text: string
    textChanged: boolean
    newWindow: boolean
}) {
    const { kind, address, text, textChanged, newWindow } = value
    const innerHtml = textChanged ? undefined : (existingAnchor?.innerHTML ?? (selectionHtml || undefined))
    const html = buildLinkHtml({ kind, address, text, innerHtml, newWindow })
    void closeThenRun(linkDialogOpen, anchorRange() ?? savedRange, "insertHTML", html)
}

function onLinkRemove() {
    void closeThenRun(linkDialogOpen, anchorRange(), "unlink")
}

function onImageSubmit(value: { src: string; alt: string }) {
    void closeThenRun(imageDialogOpen, savedRange, "insertHTML", buildImageHtml(value))
}

function onTableSubmit(value: { rows: number; cols: number; header: boolean }) {
    void closeThenRun(tableDialogOpen, savedRange, "insertHTML", buildTableHtml(value))
}

// A definition's `key` can never reach its `handler` (QEditor's keydown map calls runCmd directly),
// so the link dialog's shortcut has to be a plain listener on the editor's keydown event.
function onKeydown(event: KeyboardEvent) {
    if (!hasLinkButton.value || !(event.ctrlKey || event.metaKey) || event.key?.toLowerCase() !== "k") return
    event.preventDefault()
    openLinkDialog(editorRef.value, editorRef.value?.caret)
}

function applyAccessibleName() {
    const el = editorRef.value?.getContentEl()
    if (!el) return
    // Set one naming attribute and clear the other so flipping between labelId and ariaLabel never
    // leaves a stale attribute behind (ARIA would otherwise keep honouring the old one).
    if (props.labelId) {
        el.setAttribute("aria-labelledby", props.labelId)
        el.removeAttribute("aria-label")
    } else if (props.ariaLabel) {
        el.setAttribute("aria-label", props.ariaLabel)
        el.removeAttribute("aria-labelledby")
    } else {
        el.removeAttribute("aria-label")
        el.removeAttribute("aria-labelledby")
    }
}

onMounted(applyAccessibleName)
watch(() => [props.ariaLabel, props.labelId], applyAccessibleName)
</script>

<style scoped>
/* Let the editor toolbar wrap onto multiple rows on narrow screens instead of scrolling
   horizontally; `dense` keeps each button group intact so groups wrap as whole units. */
:deep(.q-editor__toolbar) {
    flex-wrap: wrap;
}

/* On phones, trim the inter-button and inter-group gaps so the toolbar packs into fewer rows.
   Only the gaps shrink - the buttons keep their size, so touch targets are unchanged. */
@media (width <= 599.98px) {
    :deep(.q-editor__toolbar-group) {
        margin: 0 0.125rem;
    }

    :deep(.q-editor__toolbar .q-btn) {
        margin: 0.125rem;
    }
}
</style>
