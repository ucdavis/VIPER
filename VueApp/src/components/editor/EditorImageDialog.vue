<template>
    <RecordFormDialog
        :model-value="modelValue"
        title-id="editor-image-dialog-title"
        title="Insert Image"
        :is-edit="false"
        :saving="saving"
        :form-error="formError"
        submit-label="Insert"
        @update:model-value="emit('update:modelValue', $event)"
        @submit="onSubmit"
        @hide="reset"
    >
        <!-- QOptionGroup renders role="radiogroup" on its root and takes no visible label prop, so
             the group is named with aria-label rather than a floating <div>. -->
        <q-option-group
            v-model="source"
            data-autofocus
            dense
            aria-label="Source"
            :options="sources"
        />

        <StatusBanner
            v-if="!uploadImage && uploadUnavailableHint"
            type="info"
            live="off"
        >
            {{ uploadUnavailableHint }}
        </StatusBanner>

        <q-file
            v-if="source === 'upload'"
            v-model="file"
            outlined
            dense
            label="Image file"
            :accept="ACCEPT"
            max-file-size="100000000"
            hint="Uploads take on this block's public / restricted access."
            :rules="[(v: File | null) => !!v || 'Please choose an image']"
            @update:model-value="formError = ''"
            @rejected="formError = 'Only gif, png and jpg images up to 100 MB can be uploaded'"
        >
            <template #prepend>
                <q-icon name="attach_file" />
            </template>
        </q-file>

        <q-select
            v-if="source === 'attached'"
            v-model="attached"
            outlined
            dense
            options-dense
            emit-value
            map-options
            label="Attached file"
            :options="options"
            :hint="optionsHint"
            :rules="[(v: string) => !!v || 'Please choose an attached file']"
        />

        <q-input
            v-if="source === 'url'"
            v-model="url"
            outlined
            dense
            label="Image URL"
            hint="Must be a VIPER file URL"
            :rules="[
                (v: string) => !!v.trim() || 'Please enter an image URL',
                (v: string) =>
                    isViperUrl(v) ||
                    'Only files hosted in VIPER can be shown here; attach the image to this block first',
            ]"
        />

        <div
            v-if="optionsHint && options.length === 0"
            class="text-caption text-grey-8"
        >
            {{ optionsHint }}
        </div>

        <q-input
            v-model="alt"
            outlined
            dense
            label="Alt text"
            :disable="decorative"
            hint="Describe what the image shows, for screen readers"
            :rules="[(v: string) => decorative || !!v.trim() || 'Please describe the image, or mark it decorative']"
            reactive-rules
        />

        <q-checkbox
            v-model="decorative"
            label="Decorative image (no alt text)"
        />
    </RecordFormDialog>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue"
import RecordFormDialog from "@/components/RecordFormDialog.vue"
import StatusBanner from "@/components/StatusBanner.vue"
import { isViperUrl } from "@/components/editor/editor-html"
import { IMAGE_EXTENSIONS } from "@/CMS/file-types"

/**
 * Collects an image source (a fresh upload, a file already attached to the record, or a VIPER URL)
 * plus its alt text. The parent owns the caret and builds the <img>; the only work done here is the
 * upload itself, because its failure ("a file with that name already exists") has to be shown
 * in-dialog with the chosen file still in hand.
 */

const props = defineProps<{
    modelValue: boolean
    /** Files already attached to the record, offered as the "attached file" source. */
    options: { label: string; value: string }[]
    /** Explains where the attached-file list comes from, e.g. "Attach files to this block first". */
    optionsHint?: string
    /** Uploads the chosen file and resolves with its URL. Omit to hide the upload source entirely. */
    uploadImage?: (file: File) => Promise<string>
    /** Shown when uploading isn't available, e.g. because the record hasn't been saved yet. */
    uploadUnavailableHint?: string
}>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    submit: [value: { src: string; alt: string }]
}>()

const ACCEPT = IMAGE_EXTENSIONS.map((extension) => `.${extension}`).join(",")

type Source = "upload" | "attached" | "url"

const source = ref<Source>("url")
const file = ref<File | null>(null)
const attached = ref("")
const url = ref("")
const alt = ref("")
const decorative = ref(false)
const saving = ref(false)
const formError = ref("")

const sources = computed(() => {
    const list: { label: string; value: Source }[] = []
    if (props.uploadImage) list.push({ label: "Upload a new image", value: "upload" })
    if (props.options.length > 0) list.push({ label: "Use an attached file", value: "attached" })
    list.push({ label: "Image URL", value: "url" })
    return list
})

watch(
    () => props.modelValue,
    (open) => {
        if (open) source.value = sources.value[0].value
    },
    { immediate: true },
)

function reset() {
    file.value = null
    attached.value = ""
    url.value = ""
    alt.value = ""
    decorative.value = false
    saving.value = false
    formError.value = ""
}

async function onSubmit() {
    const altText = decorative.value ? "" : alt.value.trim()

    if (source.value !== "upload") {
        emit("submit", { src: source.value === "attached" ? attached.value : url.value.trim(), alt: altText })
        return
    }

    const upload = props.uploadImage
    const chosen = file.value
    // The q-file's required rule gates this, so a missing file here means the form was bypassed.
    if (!upload || !chosen) return

    saving.value = true
    formError.value = ""
    try {
        emit("submit", { src: await upload(chosen), alt: altText })
    } catch (error) {
        // Stay open with the file still selected so the user can rename or pick another one.
        formError.value = error instanceof Error ? error.message : "The image could not be uploaded."
    } finally {
        saving.value = false
    }
}
</script>
