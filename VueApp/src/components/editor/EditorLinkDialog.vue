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
        />

        <q-input
            v-model="address"
            data-autofocus
            outlined
            dense
            :label="field.label"
            :placeholder="field.placeholder"
            :rules="[(v: string) => !!v.trim() || `Please enter ${field.article} ${field.label.toLowerCase()}`]"
            reactive-rules
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
import RecordFormDialog from "@/components/RecordFormDialog.vue"
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

const FIELDS: Record<LinkKind, { label: string; placeholder: string; article: string }> = {
    url: { label: "Web address", placeholder: "https://...", article: "a" },
    email: { label: "Email address", placeholder: "name@ucdavis.edu", article: "an" },
    phone: { label: "Phone number", placeholder: "530-...", article: "a" },
}

const kind = ref<LinkKind>("url")
const address = ref("")
const text = ref("")
const newWindow = ref(false)

// The text the field was prefilled with, so submit can report whether the user actually retyped it.
// An untouched prefill means the parent can keep the anchor's existing markup instead of flattening
// formatted content (<a><strong>...</strong></a>) to escaped plain text.
let prefilledText = ""

const field = computed(() => FIELDS[kind.value])

watch(
    () => props.modelValue,
    (open) => {
        if (!open) return
        prefilledText = props.existing?.text ?? props.selectionText ?? ""
        kind.value = props.existing?.kind ?? "url"
        address.value = props.existing?.address ?? ""
        text.value = prefilledText
        newWindow.value = props.existing?.newWindow ?? false
    },
    { immediate: true },
)

function reset() {
    prefilledText = ""
    kind.value = "url"
    address.value = ""
    text.value = ""
    newWindow.value = false
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
