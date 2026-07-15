<template>
    <div ref="rootEl">
        <q-btn
            flat
            no-caps
            class="inline-upload full-width"
            :class="{ 'inline-upload--over': isOverDropZone && !disabled }"
            :disable="disabled"
            :aria-label="ariaLabel"
            @click="activate"
        >
            <q-spinner
                v-if="busy"
                size="sm"
                color="primary"
            />
            <q-icon
                v-else
                :name="folder ? 'upload_file' : 'block'"
                size="sm"
                class="inline-upload__icon"
            />
            <span class="inline-upload__text">
                <template v-if="!folder">{{ disabledHint }}</template>
                <template v-else>Drag a file here, or click to choose one. It uploads when you save.</template>
            </span>
        </q-btn>

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
// Template-size synthetic complexity only; the upload logic is split into small helpers below.
// fallow-ignore-file complexity
import { computed, inject, ref, watch } from "vue"
import { useDropZone, useFileDialog } from "@vueuse/core"
import { useCmsFiles } from "@/CMS/composables/use-cms-files"
import StatusBanner from "@/components/StatusBanner.vue"
import { CMS_ACCEPTED_EXTENSIONS } from "@/CMS/file-types"
import type { CmsFile, CmsFileNameCheck } from "@/CMS/types/"

// Stages files for upload on the block's Save (nothing is created server-side until then), using the
// block's own folder + permissions. On a name conflict the user picks per file: overwrite the
// existing file, upload under a new name, or attach the existing file as-is (legacy's three options).
const props = defineProps<{
    folder: string | null
    permissions: string[]
    // Uploaded files inherit the block's public-access flag: a file on a public block must itself be
    // publicly downloadable, or anonymous viewers get a broken/denied link.
    allowPublicAccess?: boolean
    // When set (edit mode), uploads go through the block-scoped files API, which a delegated editor
    // can reach and which derives the folder + view permissions from the block itself. When null
    // (create mode, no id yet), the global files API is used - unchanged legacy behavior.
    contentBlockId?: number | null
}>()

const emit = defineEmits<{ "staged-count": [count: number] }>()

const apiRoot = inject("apiURL")
// All file API calls go through the CMS file service (built on useFetch); the component only chooses
// which operation to invoke. isContentScoped mirrors the service's mode: the block-scoped API exposes
// only check-name / upload / delete, so overwrite-in-place and attach-existing (below) stay global.
const files = useCmsFiles(apiRoot as string, () => props.contentBlockId)
const isContentScoped = files.isScoped
type StagedFile = {
    key: string
    file: File
    conflict: CmsFileNameCheck | null
    action: "rename" | "overwrite" | "existing"
}

const rootEl = ref<HTMLElement | null>(null)
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
const { isOverDropZone } = useDropZone(rootEl, {
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
    // "Use existing" only works when the conflict is a real file record we can attach, and only on
    // the global files API (the block-scoped API has no per-file GET to fetch it).
    if (item.conflict!.existingFileGuid && !isContentScoped.value) {
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
        const check = await files.checkName(file.name, props.folder!)
        const conflict: CmsFileNameCheck | null = check.success && check.result.inUse ? check.result : null
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
        // Best-effort rollback: isolate each delete so one failure neither stops the remaining
        // rollbacks nor masks the original upload error, which is always what we rethrow.
        for (const guid of createdGuids) {
            try {
                await files.remove(guid)
            } catch {
                // Swallow rollback failures; the leftover file falls to the trash-purge job.
            }
        }
        throw e
    } finally {
        busy.value = false
    }
}

type CommitResult = { file: CmsFile; created: boolean }

// Attach the existing file without uploading anything - never rolled back, it pre-existed.
// Global files API only (per-file GET); see commitOne.
async function attachExisting(item: StagedFile): Promise<CommitResult> {
    const res = await files.getFile(item.conflict!.existingFileGuid!)
    if (!res.success) throw new Error(res.errors?.[0] ?? `Could not load ${item.conflict!.existingFriendlyName}`)
    return { file: res.result as CmsFile, created: false }
}

// Shared multipart body. On the global files API the client supplies public-access + permissions;
// the block-scoped API derives both from the block, so only the file itself is sent there.
function buildUploadData(item: StagedFile): FormData {
    const data = new FormData()
    data.append("file", item.file)
    if (!isContentScoped.value) {
        data.append("allowPublicAccess", String(props.allowPublicAccess ?? false))
        for (const permission of props.permissions) {
            data.append("permissions", permission)
        }
    }
    return data
}

// Overwrite replaces an existing record's content in place (legacy editFile), keeping its GUID.
// It is destructive to a pre-existing file and can't be un-done, so it is NOT rolled back.
// Global files API only (per-file PUT); see commitOne.
async function overwriteInPlace(item: StagedFile, data: FormData): Promise<CommitResult> {
    const res = await files.overwriteInPlace(item.conflict!.existingFileGuid!, data)
    if (!res.success) throw new Error(res.errors?.[0] ?? `Failed to overwrite ${item.file.name}`)
    return { file: res.result as CmsFile, created: false }
}

// New record created (new upload, rename, or overwriting an orphaned on-disk file with no record).
// The global files API needs the folder in the body; the block-scoped API knows it. Safe to roll back.
async function uploadNew(item: StagedFile, data: FormData): Promise<CommitResult> {
    if (!isContentScoped.value) {
        data.append("folder", props.folder!)
    }
    if (item.conflict && item.action === "overwrite") {
        data.append("overwrite", "true")
    } else if (item.conflict && item.action === "rename") {
        data.append("fileName", item.conflict.suggestedName)
    }
    const res = await files.upload(data)
    if (!res.success) throw new Error(res.errors?.[0] ?? `Failed to upload ${item.file.name}`)
    return { file: res.result as CmsFile, created: true }
}

// Returns the resulting file and whether a NEW record was created (true = safe to roll back).
// The block-scoped API exposes only POST, so its overwrites go through uploadNew (overwrite flag)
// and attach-existing is never offered there (conflictOptions omits it).
async function commitOne(item: StagedFile): Promise<CommitResult> {
    if (!isContentScoped.value && item.conflict && item.action === "existing" && item.conflict.existingFileGuid) {
        return attachExisting(item)
    }
    const data = buildUploadData(item)
    if (!isContentScoped.value && item.conflict && item.action === "overwrite" && item.conflict.existingFileGuid) {
        return overwriteInPlace(item, data)
    }
    return uploadNew(item, data)
}

defineExpose({ commit })
</script>

<style scoped>
.inline-upload {
    width: 100%;
    padding: 1rem;
    text-align: center;
    border: 0.125rem dashed var(--q-primary);
    border-radius: 0.25rem;
    background-color: transparent;
    color: var(--ucdavis-black-60);
    transition:
        box-shadow 0.15s ease,
        border-style 0.15s ease;
}

/* q-btn lays its label out in a centred row; stack the icon over the hint text and
   keep the same gap the raw button used. */
.inline-upload :deep(.q-btn__content) {
    flex-direction: column;
    gap: 0.25rem;
}

/* Emphasise via the border/ring rather than a fill so the hint text keeps its AA
   contrast against the dialog background in every state. */
.inline-upload--over {
    border-style: solid;
    box-shadow: 0 0 0 0.125rem var(--ucdavis-blue-20);
}

.inline-upload:focus-visible {
    outline: none;
    box-shadow:
        0 0 0 0.1rem white,
        0 0 0 0.25rem var(--focus-ring-color);
}

.inline-upload.disabled {
    border-color: var(--ucdavis-black-60);
}

.inline-upload__icon {
    color: var(--q-primary);
}

.inline-upload.disabled .inline-upload__icon {
    color: var(--ucdavis-black-60);
}

.inline-upload__text {
    font-size: 0.85rem;
}

.staged-file {
    border: 1px solid var(--ucdavis-blue-20);
    border-radius: 0.25rem;
    padding: 0.5rem;
}

.staged-file__conflict {
    margin-top: 0.25rem;
    padding-left: 1.5rem;
}

@media (prefers-reduced-motion: reduce) {
    .inline-upload {
        transition: none;
    }
}
</style>
