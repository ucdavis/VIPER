<template>
    <div>
        <button
            ref="dropZoneRef"
            type="button"
            class="inline-upload"
            :class="{ 'inline-upload--over': isOverDropZone && !disabled }"
            :disabled="disabled"
            :aria-label="ariaLabel"
            @click="activate"
        >
            <q-spinner
                v-if="busy"
                size="24px"
                color="primary"
            />
            <q-icon
                v-else
                :name="folder ? 'upload_file' : 'block'"
                size="24px"
                class="inline-upload__icon"
            />
            <span class="inline-upload__text">
                <template v-if="!folder">{{ disabledHint }}</template>
                <template v-else>Drag a file here, or click to choose one. It uploads when you save.</template>
            </span>
        </button>

        <!-- Files staged for upload on Save. Nothing is created on the server until the block is saved. -->
        <div
            v-for="item in staged"
            :key="item.key"
            class="staged-file q-mt-sm"
        >
            <div class="row items-center no-wrap">
                <q-icon
                    :name="item.conflict ? 'warning' : 'schedule'"
                    :color="item.conflict ? 'warning' : 'grey-7'"
                    size="xs"
                    class="q-mr-sm"
                />
                <span class="col ellipsis">{{ item.file.name }}</span>
                <q-btn
                    flat
                    dense
                    size="sm"
                    icon="close"
                    color="negative"
                    :aria-label="`Remove ${item.file.name}`"
                    @click="removeStaged(item.key)"
                >
                    <q-tooltip>Remove</q-tooltip>
                </q-btn>
            </div>

            <div
                v-if="item.conflict"
                class="staged-file__conflict"
            >
                <div class="text-caption text-warning">
                    A file named "{{ item.file.name }}" already exists in {{ folder }}. Choose what to do:
                </div>
                <q-option-group
                    v-model="item.action"
                    :options="conflictOptions(item)"
                    type="radio"
                    dense
                    class="q-mt-xs"
                />
            </div>
        </div>

        <StatusBanner
            v-if="error"
            type="error"
            class="q-mt-sm"
        >
            {{ error }}
        </StatusBanner>
    </div>
</template>

<script setup lang="ts">
import { computed, inject, ref, watch } from "vue"
import { useDropZone, useFileDialog } from "@vueuse/core"
import { useFetch } from "@/composables/ViperFetch"
import StatusBanner from "@/components/StatusBanner.vue"
import { CMS_ACCEPTED_EXTENSIONS } from "@/CMS/file-types"
import type { CmsFile } from "@/CMS/types/"

// Stages files for upload on the block's Save (nothing is created server-side until then), using the
// block's own folder + permissions. On a name conflict the user picks per file: overwrite the
// existing file, upload under a new name, or attach the existing file as-is (legacy's three options).
const props = defineProps<{
    folder: string | null
    permissions: string[]
    // Uploaded files inherit the block's public-access flag: a file on a public block must itself be
    // publicly downloadable, or anonymous viewers get a broken/denied link.
    allowPublicAccess?: boolean
}>()

const emit = defineEmits<{ "staged-count": [count: number] }>()

const apiURL = inject("apiURL") + "cms/files/"
const { get, postForm, putForm, del, createUrlSearchParams } = useFetch()

type NameCheck = {
    inUse: boolean
    suggestedName: string
    existingFileGuid: string | null
    existingFriendlyName: string | null
    existingDeleted: boolean
}
type StagedFile = {
    key: string
    file: File
    conflict: NameCheck | null
    action: "rename" | "overwrite" | "existing"
}

const dropZoneRef = ref<HTMLElement | null>(null)
const staged = ref<StagedFile[]>([])
const busy = ref(false)
const error = ref("")
let keyCounter = 0

const disabled = computed(() => !props.folder || busy.value)
const disabledHint = "Set a VIPER section path to upload files for this block."
const ariaLabel = computed(() =>
    props.folder ? "Add a file to upload when the block is saved, by clicking or dragging" : disabledHint,
)

watch(
    () => staged.value.length,
    (n) => emit("staged-count", n),
)

const { open: openFileDialog, onChange: onFileDialogChange } = useFileDialog({
    accept: CMS_ACCEPTED_EXTENSIONS,
    multiple: false,
})
onFileDialogChange((files) => stageFile(files?.[0]))
const { isOverDropZone } = useDropZone(dropZoneRef, {
    multiple: false,
    onDrop: (files) => stageFile(files?.[0]),
})

function activate() {
    if (disabled.value) return
    openFileDialog()
}

function conflictOptions(item: StagedFile) {
    const opts = [
        { label: `Upload under a new name (${item.conflict!.suggestedName})`, value: "rename" },
        { label: "Overwrite the existing file", value: "overwrite" },
    ]
    // "Use existing" only works when the conflict is a real file record we can attach.
    if (item.conflict!.existingFileGuid) {
        opts.push({ label: "Use the existing file instead (don't upload)", value: "existing" })
    }
    return opts
}

async function stageFile(file: File | null | undefined) {
    if (!file || disabled.value) return
    error.value = ""

    // Drag-drop bypasses the picker's accept filter, so validate the extension ourselves.
    const ext = "." + (file.name.split(".").pop() ?? "").toLowerCase()
    if (!CMS_ACCEPTED_EXTENSIONS.split(",").includes(ext)) {
        error.value = `Files of type ${ext} aren't allowed.`
        return
    }

    busy.value = true
    try {
        const params = createUrlSearchParams({ folder: props.folder!, fileName: file.name })
        const check = await get(apiURL + "check-name?" + params)
        const conflict: NameCheck | null = check.success && check.result.inUse ? check.result : null
        staged.value.push({ key: String(++keyCounter), file, conflict, action: "rename" })
    } finally {
        busy.value = false
    }
}

function removeStaged(key: string) {
    staged.value = staged.value.filter((s) => s.key !== key)
}

// Uploads (or resolves) every staged file per its chosen action. Returns the files to attach plus
// the GUIDs that were newly created (new uploads/renames) so the parent can roll them back if the
// block save then fails. Called as part of Save; throws on the first failure so the save aborts,
// after rolling back files created earlier in the batch (soft delete, same as the parent's
// rollbackFiles) so a partial failure doesn't strand new uploads attached to nothing.
async function commit(): Promise<{ attached: CmsFile[]; createdGuids: string[] }> {
    if (staged.value.length === 0) return { attached: [], createdGuids: [] }
    busy.value = true
    const attached: CmsFile[] = []
    const createdGuids: string[] = []
    try {
        for (const item of staged.value) {
            const { file, created } = await commitOne(item)
            attached.push(file)
            if (created) createdGuids.push(file.fileGuid)
        }
        staged.value = []
        return { attached, createdGuids }
    } catch (e) {
        for (const guid of createdGuids) {
            await del(apiURL + guid)
        }
        throw e
    } finally {
        busy.value = false
    }
}

// Returns the resulting file and whether a NEW record was created (true = safe to roll back).
async function commitOne(item: StagedFile): Promise<{ file: CmsFile; created: boolean }> {
    // Attach the existing file without uploading anything - never roll this back, it pre-existed.
    if (item.conflict && item.action === "existing" && item.conflict.existingFileGuid) {
        const res = await get(apiURL + item.conflict.existingFileGuid)
        if (!res.success) throw new Error(res.errors?.[0] ?? `Could not load ${item.conflict.existingFriendlyName}`)
        return { file: res.result as CmsFile, created: false }
    }

    const data = new FormData()
    data.append("file", item.file)
    data.append("allowPublicAccess", String(props.allowPublicAccess ?? false))
    for (const permission of props.permissions) {
        data.append("permissions", permission)
    }

    // Overwrite replaces an existing record's content in place (legacy editFile), keeping its GUID.
    // It is destructive to a pre-existing file and can't be un-done, so it is NOT rolled back.
    if (item.conflict && item.action === "overwrite" && item.conflict.existingFileGuid) {
        const res = await putForm(apiURL + item.conflict.existingFileGuid, data)
        if (!res.success) throw new Error(res.errors?.[0] ?? `Failed to overwrite ${item.file.name}`)
        return { file: res.result as CmsFile, created: false }
    }

    // New record created (new upload, rename, or overwriting an orphaned on-disk file with no
    // record); folder is required. This is safe to roll back on a failed save.
    data.append("folder", props.folder!)
    if (item.conflict && item.action === "overwrite") {
        data.append("overwrite", "true")
    } else if (item.conflict && item.action === "rename") {
        data.append("fileName", item.conflict.suggestedName)
    }
    const res = await postForm(apiURL, data)
    if (!res.success) throw new Error(res.errors?.[0] ?? `Failed to upload ${item.file.name}`)
    return { file: res.result as CmsFile, created: true }
}

defineExpose({ commit })
</script>

<style scoped>
.inline-upload {
    display: flex;
    width: 100%;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 4px;
    padding: 16px;
    text-align: center;
    border: 2px dashed var(--q-primary);
    border-radius: 4px;
    background-color: transparent;
    font: inherit;
    color: var(--ucdavis-black-60);
    cursor: pointer;
    appearance: none;
    transition:
        box-shadow 0.15s ease,
        border-style 0.15s ease;
}

/* Emphasise via the border/ring rather than a fill so the hint text keeps its AA
   contrast against the dialog background in every state. */
.inline-upload--over {
    border-style: solid;
    box-shadow: 0 0 0 2px var(--ucdavis-blue-20);
}

.inline-upload:focus-visible {
    outline: none;
    box-shadow:
        0 0 0 0.1rem white,
        0 0 0 0.25rem #258cfb;
}

.inline-upload:disabled {
    border-color: var(--ucdavis-black-60);
    cursor: not-allowed;
    opacity: 0.85;
}

.inline-upload__icon {
    color: var(--q-primary);
}

.inline-upload:disabled .inline-upload__icon {
    color: var(--ucdavis-black-60);
}

.inline-upload__text {
    font-size: 0.85rem;
}

.staged-file {
    border: 1px solid var(--ucdavis-blue-20);
    border-radius: 4px;
    padding: 8px;
}

.staged-file__conflict {
    margin-top: 4px;
    padding-left: 24px;
}

@media (prefers-reduced-motion: reduce) {
    .inline-upload {
        transition: none;
    }
}
</style>
