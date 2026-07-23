<template>
    <q-editor
        ref="editorRef"
        :model-value="modelValue"
        :toolbar="toolbar"
        :definitions="editorDefinitions"
        v-bind="$attrs"
        @update:model-value="(value: string) => emit('update:modelValue', value)"
    />
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue"
import { useQuasar } from "quasar"

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

const $q = useQuasar()
const editorRef = ref()
const viewingSource = ref(false)

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
    return definitions
})

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
