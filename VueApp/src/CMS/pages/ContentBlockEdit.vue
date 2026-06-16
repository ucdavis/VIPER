<template>
    <div class="q-pa-md">
        <BreadcrumbHeading
            :label="isNew ? 'Add Content Block' : 'Edit Content Block'"
            parent-label="Manage Content Blocks"
            :parent-to="{ name: 'CmsContentBlocks' }"
        />

        <StatusBanner
            v-if="block.deletedOn"
            type="warning"
            class="q-mb-md"
        >
            This content block is marked as deleted. Restore it to make it available again.
            <template #action>
                <q-btn
                    flat
                    dense
                    no-caps
                    color="primary"
                    label="Restore"
                    @click="restoreBlock"
                />
            </template>
        </StatusBanner>

        <q-form
            ref="formRef"
            greedy
            @submit.prevent="saveBlock"
            @validation-error="onValidationError"
        >
            <div class="row q-col-gutter-lg">
                <div class="col-12 col-lg-8">
                    <q-input
                        v-model="block.title"
                        dense
                        outlined
                        label="Title"
                        class="q-mb-sm required-field"
                        :rules="[(v: string | null) => (v && v.trim().length > 0) || 'Title is required']"
                        aria-required="true"
                        hide-bottom-space
                    />

                    <div
                        id="content-editor-label"
                        class="text-subtitle2 q-mb-xs"
                    >
                        Content
                    </div>
                    <q-editor
                        ref="contentEditorRef"
                        v-model="block.content"
                        min-height="20rem"
                        :toolbar="editorToolbar"
                        :definitions="{}"
                    />

                    <div
                        v-if="!isNew && history.length"
                        class="q-mt-md row items-center q-col-gutter-sm"
                    >
                        <div class="col-auto text-subtitle2">Version history:</div>
                        <div class="col-12 col-sm-5 col-md-4">
                            <q-select
                                v-model="selectedHistory"
                                dense
                                options-dense
                                outlined
                                clearable
                                label="Load a previous version"
                                :options="history"
                                :option-label="historyLabel"
                                @update:model-value="loadHistoryVersion"
                            />
                        </div>
                        <div
                            v-if="viewingVersion"
                            class="col-auto"
                        >
                            <q-chip
                                color="orange-2"
                                dense
                                square
                            >
                                Viewing an old version — Save to restore it
                            </q-chip>
                        </div>
                    </div>
                </div>

                <div class="col-12 col-lg-4">
                    <q-card
                        flat
                        bordered
                        class="q-mb-md"
                    >
                        <q-card-section class="q-gutter-y-sm">
                            <h2 class="text-h6 q-my-none">Settings</h2>

                            <q-select
                                v-model="block.system"
                                dense
                                options-dense
                                outlined
                                label="System"
                                class="required-field"
                                :options="['Viper', 'Public']"
                                :rules="[(v: string | null) => !!v || 'System is required']"
                                aria-required="true"
                                hide-bottom-space
                                @update:model-value="onSystemChange"
                            />

                            <q-select
                                v-model="block.viperSectionPath"
                                dense
                                options-dense
                                outlined
                                clearable
                                use-input
                                new-value-mode="add-unique"
                                input-debounce="0"
                                label="VIPER section path"
                                hint="Pick an existing section or type a new one"
                                :options="sectionPaths"
                            />

                            <q-input
                                v-model="block.page"
                                dense
                                outlined
                                label="Page"
                            />

                            <q-input
                                v-model.number="block.blockOrder"
                                dense
                                outlined
                                type="number"
                                label="Block order"
                            />

                            <q-input
                                v-model="block.friendlyName"
                                dense
                                outlined
                                label="Friendly name"
                                hint="Used for URL-friendly access; must be unique"
                            />

                            <q-toggle
                                v-model="block.allowPublicAccess"
                                label="Public access"
                                @update:model-value="autoEnabledPublicAccess = false"
                            />

                            <StatusBanner
                                v-if="autoEnabledPublicAccess"
                                type="info"
                                live="assertive"
                            >
                                Public system content is visible to everyone, so we turned on Public access. Switch it
                                off if this block should stay restricted.
                            </StatusBanner>
                        </q-card-section>
                    </q-card>

                    <q-card
                        flat
                        bordered
                        class="q-mb-md"
                    >
                        <q-card-section>
                            <h2 class="text-h6 q-my-none q-mb-sm">Permissions</h2>
                            <PermissionSelector v-model="block.permissions" />
                        </q-card-section>
                    </q-card>

                    <q-card
                        flat
                        bordered
                    >
                        <q-card-section>
                            <h2 class="text-h6 q-my-none q-mb-sm">Attached Files</h2>
                            <div
                                v-for="file in block.files"
                                :key="file.fileGuid"
                                class="row items-center q-mb-xs"
                            >
                                <a
                                    :href="file.url"
                                    target="_blank"
                                    rel="noopener"
                                    class="col ellipsis"
                                >
                                    {{ file.friendlyName }}
                                    <span class="sr-only">(opens in new window)</span>
                                </a>
                                <q-btn
                                    dense
                                    flat
                                    size="sm"
                                    icon="close"
                                    color="negative"
                                    aria-label="Detach file"
                                    @click="detachFile(file)"
                                >
                                    <q-tooltip>Detach</q-tooltip>
                                </q-btn>
                            </div>
                            <q-select
                                v-model="fileToAttach"
                                dense
                                options-dense
                                outlined
                                use-input
                                input-debounce="300"
                                label="Attach a file"
                                hint="Search managed files by name"
                                :options="fileOptions"
                                option-label="friendlyName"
                                :loading="searchingFiles"
                                @filter="searchFiles"
                                @update:model-value="attachFile"
                            />
                        </q-card-section>
                    </q-card>
                </div>
            </div>

            <StatusBanner
                v-if="formError"
                type="error"
                class="q-mt-md"
            >
                {{ formError }}
            </StatusBanner>

            <div class="q-mt-md">
                <q-btn
                    type="submit"
                    color="primary"
                    :label="isNew ? 'Create' : 'Save'"
                    dense
                    no-caps
                    :loading="saving"
                >
                    <template #loading>
                        <q-spinner
                            size="1em"
                            class="q-mr-sm"
                        />
                        {{ isNew ? "Create" : "Save" }}
                    </template>
                </q-btn>
                <q-btn
                    flat
                    label="Cancel"
                    dense
                    no-caps
                    class="q-ml-sm"
                    :to="{ name: 'CmsContentBlocks' }"
                />
            </div>
        </q-form>
    </div>
</template>

<script setup lang="ts">
import { computed, inject, onMounted, ref } from "vue"
import { useRoute, useRouter, onBeforeRouteLeave } from "vue-router"
import { useQuasar } from "quasar"
import { useFetch } from "@/composables/ViperFetch"
import { useUnsavedChanges } from "@/composables/use-unsaved-changes"
import BreadcrumbHeading from "@/components/BreadcrumbHeading.vue"
import PermissionSelector from "@/CMS/components/PermissionSelector.vue"
import StatusBanner from "@/components/StatusBanner.vue"
import type { CmsContentBlock, CmsContentBlockFile, CmsContentHistoryItem, CmsFile } from "@/CMS/types/"

const apiURL = inject("apiURL") + "CMS/content"
const filesApiURL = inject("apiURL") + "cms/files/"
const route = useRoute()
const router = useRouter()
const $q = useQuasar()
const { get, post, put, createUrlSearchParams } = useFetch()

const blockId = computed(() => (route.params.id ? Number(route.params.id) : null))
const isNew = computed(() => blockId.value === null)

const formRef = ref()
const contentEditorRef = ref()
const saving = ref(false)
const formError = ref("")

const emptyBlock = (): CmsContentBlock => ({
    contentBlockId: 0,
    content: "",
    title: null,
    system: "Viper",
    application: null,
    page: null,
    viperSectionPath: null,
    blockOrder: 1,
    friendlyName: null,
    allowPublicAccess: false,
    modifiedOn: "",
    modifiedBy: "",
    deletedOn: null,
    permissions: [],
    files: [],
})

const block = ref<CmsContentBlock>(emptyBlock())

const { setInitialState, resetDirtyState, confirmClose } = useUnsavedChanges(block)

// Prompt before leaving with unsaved edits, matching the Effort forms' guard.
onBeforeRouteLeave(async () => await confirmClose())

const sectionPaths = ref<string[]>([])
const history = ref<CmsContentHistoryItem[]>([])
const selectedHistory = ref<CmsContentHistoryItem | null>(null)
const viewingVersion = ref(false)
const autoEnabledPublicAccess = ref(false)

const fileToAttach = ref<CmsFile | null>(null)
const fileOptions = ref<CmsFile[]>([])
const searchingFiles = ref(false)

const editorToolbar = [
    ["bold", "italic", "underline", "strike"],
    [{ icon: "format_size", options: ["p", "h2", "h3", "h4", "h5"] }],
    ["unordered", "ordered", "outdent", "indent"],
    ["link", "hr", "quote"],
    ["removeFormat"],
    ["undo", "redo"],
]

async function loadBlock() {
    if (blockId.value === null) return
    const res = await get(apiURL + "/" + blockId.value)
    if (!res.success) {
        $q.notify({ type: "negative", message: "Content block not found" })
        void router.push({ name: "CmsContentBlocks" })
        return
    }
    block.value = res.result
    autoEnabledPublicAccess.value = false
    setInitialState()
    await loadHistory()
}

async function loadHistory() {
    if (blockId.value === null) return
    const res = await get(apiURL + "/" + blockId.value + "/history")
    history.value = res.success ? res.result : []
}

async function loadSectionPaths() {
    const res = await get(apiURL + "/section-paths")
    sectionPaths.value = res.success ? res.result : []
}

function historyLabel(h: CmsContentHistoryItem): string {
    const when = h.modifiedOn ? new Date(h.modifiedOn).toLocaleString() : "unknown date"
    return `${when} (${h.modifiedBy ?? "unknown"})`
}

async function loadHistoryVersion() {
    if (!selectedHistory.value) {
        viewingVersion.value = false
        return
    }
    const res = await get(apiURL + "/" + blockId.value + "/history/" + selectedHistory.value.contentHistoryId)
    if (res.success) {
        block.value.content = res.result.content
        viewingVersion.value = true
        $q.notify({
            type: "info",
            message: "Loaded an older version into the editor. Save to make it the current version.",
        })
    }
}

async function searchFiles(val: string, update: (fn: () => void) => void) {
    if (val.trim().length < 2) {
        update(() => {
            fileOptions.value = []
        })
        return
    }
    searchingFiles.value = true
    const params = createUrlSearchParams({ search: val.trim(), status: "active", perPage: 15 })
    const res = await get(filesApiURL + "?" + params)
    searchingFiles.value = false
    update(() => {
        const attachedGuids = new Set(block.value.files.map((f) => f.fileGuid))
        fileOptions.value = res.success ? res.result.filter((f: CmsFile) => !attachedGuids.has(f.fileGuid)) : []
    })
}

function attachFile() {
    if (!fileToAttach.value) return
    block.value.files.push({
        fileGuid: fileToAttach.value.fileGuid,
        friendlyName: fileToAttach.value.friendlyName,
        url: fileToAttach.value.friendlyUrl,
    })
    fileToAttach.value = null
}

function detachFile(file: CmsContentBlockFile) {
    block.value.files = block.value.files.filter((f) => f.fileGuid !== file.fileGuid)
}

// "Public" system content is publicly visible by convention, but AllowPublicAccess is the only
// field that actually gates unauthenticated access. Enable it for the user and tell them, so the
// two stay in sync without silently exposing a block they meant to keep restricted.
function onSystemChange(value: string | null) {
    if (value === "Public" && !block.value.allowPublicAccess) {
        block.value.allowPublicAccess = true
        autoEnabledPublicAccess.value = true
    } else {
        autoEnabledPublicAccess.value = false
    }
}

// Fires when the form's submit-time validation fails. The q-form focuses the first invalid
// field; this surfaces a matching message next to the Create button so the failure is obvious
// without scrolling back up to the field.
function onValidationError() {
    formError.value = isNew.value
        ? "Please complete the required fields before creating this content block."
        : "Please complete the required fields before saving your changes."
}

async function saveBlock() {
    formError.value = ""

    saving.value = true
    const payload = {
        contentBlockId: block.value.contentBlockId,
        content: block.value.content,
        title: block.value.title,
        system: block.value.system,
        application: block.value.application,
        page: block.value.page,
        viperSectionPath: block.value.viperSectionPath,
        blockOrder: block.value.blockOrder,
        friendlyName: block.value.friendlyName || null,
        allowPublicAccess: block.value.allowPublicAccess,
        permissions: block.value.permissions,
        fileGuids: block.value.files.map((f) => f.fileGuid),
        lastModifiedOn: isNew.value ? null : block.value.modifiedOn,
    }

    const res = isNew.value
        ? await post(apiURL, payload)
        : await put(apiURL + "/" + block.value.contentBlockId, payload)
    saving.value = false

    if (res.status === 409) {
        $q.dialog({
            title: "Edit Conflict",
            message:
                (res.errors?.[0] ?? "This content block was changed by someone else.") +
                " Reload the latest version? Your unsaved changes will be lost.",
            cancel: { label: "Keep editing", flat: true },
            persistent: true,
            ok: { label: "Reload", color: "primary", unelevated: true },
        }).onOk(() => {
            void loadBlock()
            viewingVersion.value = false
            selectedHistory.value = null
        })
        return
    }

    if (!res.success) {
        formError.value = res.errors?.[0] ?? "Failed to save content block"
        return
    }

    $q.notify({ type: "positive", message: isNew.value ? "Content block created" : "Content block saved" })
    viewingVersion.value = false
    selectedHistory.value = null
    autoEnabledPublicAccess.value = false

    if (isNew.value) {
        void router.push({ name: "CmsContentBlockEdit", params: { id: res.result.contentBlockId } })
        block.value = res.result
        resetDirtyState()
        await loadHistory()
    } else {
        block.value = res.result
        resetDirtyState()
        await loadHistory()
    }
}

async function restoreBlock() {
    const res = await post(apiURL + "/" + block.value.contentBlockId + "/restore")
    if (res.success) {
        $q.notify({ type: "positive", message: "Content block restored" })
        await loadBlock()
    }
}

onMounted(() => {
    // QEditor renders the focusable contenteditable as an inner element, so its accessible
    // name has to be set there rather than on the wrapper the "Content" label sits beside.
    contentEditorRef.value?.getContentEl()?.setAttribute("aria-labelledby", "content-editor-label")
    loadSectionPaths()
    loadBlock()
    // loadBlock sets the baseline for an existing block after it loads; a brand-new form's
    // baseline is just the empty block, so capture it synchronously here.
    if (isNew.value) {
        setInitialState()
    }
})
</script>

<style scoped>
.required-field :deep(.q-field__label)::after {
    content: " *";
    color: var(--q-negative);
}
</style>
