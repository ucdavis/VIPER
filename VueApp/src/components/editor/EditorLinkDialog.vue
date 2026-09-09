<template>
    <RecordFormDialog
        :model-value="modelValue"
        title-id="editor-link-dialog-title"
        :title="existing ? 'Edit Link' : 'Insert Link'"
        :is-edit="false"
        :saving="false"
        form-error=""
        :submit-label="existing ? 'Update' : 'Insert'"
        @update:model-value="emit('update:modelValue', $event)"
        @submit="onSubmit"
        @hide="reset"
    >
        <q-select
            v-model="kind"
            outlined
            dense
            options-dense
            emit-value
            map-options
            label="Link type"
            :options="KIND_OPTIONS"
            @update:model-value="onKindChange"
        />

        <q-input
            ref="addressField"
            v-model="address"
            data-autofocus
            outlined
            dense
            :label="field.label"
            :placeholder="field.placeholder"
            lazy-rules="ondemand"
            :rules="[
                (v: string) => !!v.trim() || `Please enter ${field.article} ${field.label.toLowerCase()}`,
                shapeRule,
            ]"
        />

        <q-input
            v-model="text"
            outlined
            dense
            label="Text to display"
            hint="Leave blank to show the address itself"
        />

        <q-checkbox
            v-model="newWindow"
            label="Open in new window"
        />

        <div v-if="existing">
            <q-btn
                flat
                dense
                no-caps
                color="negative"
                icon="link_off"
                label="Remove link"
                @click="emit('remove')"
            />
        </div>
    </RecordFormDialog>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue"
import { patterns } from "quasar"
import RecordFormDialog from "@/components/RecordFormDialog.vue"
import { isSafeHref } from "@/shared/url-safety"
import type { LinkKind } from "@/components/editor/editor-html"

/**
 * Replaces QEditor's inline URL bar, which offers a bare text box with no way to set the link
 * text, pick mailto/tel, or open in a new window. The parent (RichTextEditor) owns the caret and
 * builds the HTML; this dialog only collects fields.
 */

const props = defineProps<{
    modelValue: boolean
    /** Fields of the anchor being edited, or null when inserting a new link. */
    existing?: { kind: LinkKind; address: string; text: string; newWindow: boolean } | null
    /** Selected text in the editor, prefilled as the link text for a new link. */
    selectionText?: string
}>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    submit: [value: { kind: LinkKind; address: string; text: string; textChanged: boolean; newWindow: boolean }]
    remove: []
}>()

const KIND_OPTIONS: { label: string; value: LinkKind }[] = [
    { label: "Web address", value: "url" },
    { label: "Email", value: "email" },
    { label: "Phone", value: "phone" },
]

// `shape` is the per-kind format check run on submit. Without one, whatever was typed for the
// previous kind (or anything else) goes straight into the href: picking Email after typing a web
// address built `mailto:https://...`, a link that silently fails for every reader.
const FIELDS: Record<
    LinkKind,
    { label: string; placeholder: string; article: string; shape?: (value: string) => true | string }
> = {
    url: {
        label: "Web address",
        placeholder: "https://...",
        article: "a",
        // The same protocol allowlist Link Collections holds its URLs to, minus its
        // absolute-only rule: relative paths, fragments and query-only values are all legitimate
        // links inside a content block, and normalizeHref supplies the scheme for a bare domain.
        shape: (value) => isSafeHref(value) || "Enter a web address starting with http:// or https://",
    },
    email: {
        label: "Email address",
        placeholder: "name@ucdavis.edu",
        article: "an",
        // Quasar's own check, as the emergency-contact form uses. Whether an address can actually
        // receive mail is only ever settled by sending some; this just catches the wrong thing in
        // the wrong field.
        shape: (value) => patterns.testPattern.email(value) || "Enter an email address like name@ucdavis.edu",
    },
    phone: {
        label: "Phone number",
        placeholder: "530-...",
        article: "a",
        // No equivalent built-in, and phone formats vary too much between countries for a format
        // check to be worth writing. Counting digits keeps extensions and every separator style
        // working while still catching a URL or an email address pasted into this field.
        shape: (value) => value.replaceAll(/\D/gu, "").length >= 7 || "Enter a phone number like 530-752-1360",
    },
}

const addressField = ref<{ resetValidation: () => void } | null>(null)
const kind = ref<LinkKind>("url")
const address = ref("")
const text = ref("")
const newWindow = ref(false)

// The text the field was prefilled with, so submit can report whether the user actually retyped it.
// An untouched prefill means the parent can keep the anchor's existing markup instead of flattening
// formatted content (<a><strong>...</strong></a>) to escaped plain text.
let prefilledText = ""

const field = computed(() => FIELDS[kind.value])

// Blank is the required rule's business, so an empty field shows one message rather than two.
function shapeRule(value: string): true | string {
    const trimmed = value.trim()
    return !trimmed || !field.value.shape ? true : field.value.shape(trimmed)
}

// A type change starts the address over. Carrying it across built hrefs like
// `mailto:https://ucdavis.edu`, and a message left from the previous type names the wrong field.
function onKindChange() {
    address.value = ""
    addressField.value?.resetValidation()
}

const BLANK_LINK = { kind: "url" as LinkKind, address: "", text: "", newWindow: false }

function fill(values: typeof BLANK_LINK) {
    prefilledText = values.text
    kind.value = values.kind
    address.value = values.address
    text.value = values.text
    newWindow.value = values.newWindow
}

watch(
    () => props.modelValue,
    (open) => {
        if (!open) return
        fill(props.existing ?? { ...BLANK_LINK, text: props.selectionText ?? "" })
    },
    { immediate: true },
)

function reset() {
    fill(BLANK_LINK)
}

function onSubmit() {
    emit("submit", {
        kind: kind.value,
        address: address.value.trim(),
        text: text.value.trim(),
        textChanged: text.value !== prefilledText,
        newWindow: newWindow.value,
    })
}
</script>
