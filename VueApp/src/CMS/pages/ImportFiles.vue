<template>
    <div class="q-pa-md">
        <BreadcrumbHeading
            label="Import from VIPER"
            parent-label="Manage Files"
            :parent-to="{ name: 'CmsFiles' }"
        />

        <p class="text-body2 text-grey-8 intro-copy">
            Moves files out of the legacy VIPER webroot into the managed file store. The original path is saved as the
            file's Old URL, so existing links keep working through the file handler. Enter one path per line, relative
            to the legacy webroot (e.g.
            <code>/cats/docs/manual.pdf</code>).
        </p>

        <q-form
            v-if="!preview"
            greedy
            @submit.prevent="runPreview"
        >
            <div class="import-form">
                <q-input
                    v-model="paths"
                    outlined
                    type="textarea"
                    rows="12"
                    label="File paths (one per line)"
                    class="q-mb-md"
                    :rules="[(v: string | null) => (v && v.trim().length > 0) || 'Enter at least one path']"
                    hide-bottom-space
                />
                <q-select
                    v-model="options.folder"
                    dense
                    options-dense
                    outlined
                    label="Destination folder"
                    class="q-mb-sm"
                    :options="folders"
                    :rules="[(v: string | null) => !!v || 'Please select a folder']"
                    hide-bottom-space
                />
                <PermissionSelector
                    v-model="options.permissions"
                    class="q-mb-sm"
                />
                <div class="column items-start">
                    <q-toggle
                        v-model="options.useDefaultPermission"
                        label="Add default permission (SVMSecure.{folder})"
                    />
                    <q-toggle
                        v-model="options.allowPublicAccess"
                        label="Public access"
                    />
                    <q-toggle
                        v-model="options.encrypt"
                        label="Encrypt files"
                    />
                </div>
                <div class="q-mt-md">
                    <q-btn
                        type="submit"
                        color="primary"
                        label="Preview Import"
                        dense
                        no-caps
                        class="q-pr-md"
                        :loading="previewing"
                    >
                        <template #loading>
                            <q-spinner
                                size="1em"
                                class="q-mr-sm"
                            />
                            Preview Import
                        </template>
                    </q-btn>
                </div>
            </div>
        </q-form>

        <template v-else>
            <h2 class="text-h6 text-primary q-mb-sm">Confirm Import</h2>
            <p class="text-body2">
                {{ importableCount }} of {{ preview.length }} {{ inflect("file", preview.length) }} will be imported
                into <strong>{{ options.folder }}</strong
                >. Nothing has been moved yet.
            </p>
            <ul class="text-body2 q-mt-none">
                <li>
                    Permissions:
                    {{ effectivePermissions.length ? effectivePermissions.join(", ") : "none (any VIPER user)" }}
                </li>
                <li>Public access: {{ options.allowPublicAccess ? "yes" : "no" }}</li>
                <li>Encrypt: {{ options.encrypt ? "yes" : "no" }}</li>
            </ul>
            <q-table
                :rows="preview"
                :columns="previewColumns"
                row-key="filePath"
                :pagination="{ rowsPerPage: 0 }"
                hide-bottom
                dense
                flat
                bordered
            >
                <template #body-cell-status="cellProps">
                    <q-td :props="cellProps">
                        <q-icon
                            :name="previewStatus(cellProps.row).icon"
                            :color="previewStatus(cellProps.row).color"
                            size="sm"
                        />
                        <span class="sr-only">{{ previewStatus(cellProps.row).label }}</span>
                    </q-td>
                </template>
                <template #body-cell-fileName="cellProps">
                    <q-td :props="cellProps">
                        <template v-if="cellProps.row.fileName">
                            {{ cellProps.row.fileName }}
                            <div
                                v-if="cellProps.row.renamedFrom"
                                class="text-caption text-warning"
                            >
                                renamed from {{ cellProps.row.renamedFrom }}
                            </div>
                        </template>
                    </q-td>
                </template>
            </q-table>
            <div class="q-mt-md q-gutter-sm">
                <q-btn
                    flat
                    dense
                    no-caps
                    label="Back"
                    @click="preview = null"
                />
                <q-btn
                    color="primary"
                    dense
                    no-caps
                    class="q-pr-md"
                    :label="`Confirm and Import ${importableCount} ${inflect('file', importableCount)}`"
                    :disable="importableCount === 0"
                    :loading="importing"
                    @click="runImport"
                >
                    <template #loading>
                        <q-spinner
                            size="1em"
                            class="q-mr-sm"
                        />
                        Importing...
                    </template>
                </q-btn>
            </div>
        </template>

        <template v-if="results.length">
            <h2 class="text-h6 text-primary q-mb-sm q-mt-lg">Results</h2>
            <q-table
                :rows="results"
                :columns="resultColumns"
                row-key="filePath"
                :pagination="{ rowsPerPage: 0 }"
                hide-bottom
                dense
                flat
                bordered
            >
                <template #body-cell-success="cellProps">
                    <q-td :props="cellProps">
                        <q-icon
                            :name="cellProps.row.success ? 'check_circle' : 'error'"
                            :color="cellProps.row.success ? 'positive' : 'negative'"
                            size="sm"
                        />
                        <span class="sr-only">{{ cellProps.row.success ? "Imported" : "Failed" }}</span>
                    </q-td>
                </template>
            </q-table>
        </template>
    </div>
</template>

<script setup lang="ts">
// Template-size synthetic complexity only (large wizard/table markup); script logic is small.
// fallow-ignore-file complexity
import { computed, inject, onMounted, ref } from "vue"
import { inflect } from "inflection"
import { useQuasar, type QTableProps } from "quasar"
import { useFetch } from "@/composables/ViperFetch"
import BreadcrumbHeading from "@/components/BreadcrumbHeading.vue"
import PermissionSelector from "@/CMS/components/PermissionSelector.vue"

type ImportResult = {
    filePath: string
    success: boolean
    message: string | null
    fileGuid: string | null
    friendlyName: string | null
}

type ImportPreview = {
    filePath: string
    canImport: boolean
    message: string | null
    fileName: string | null
    renamedFrom: string | null
    friendlyName: string | null
    oldUrl: string | null
}

const apiURL = inject("apiURL") + "cms/files/"
const $q = useQuasar()
const { get, post } = useFetch()

const folders = ref<string[]>([])
const paths = ref("")
const importing = ref(false)
const previewing = ref(false)
const preview = ref<ImportPreview[] | null>(null)
const results = ref<ImportResult[]>([])

const options = ref({
    folder: null as string | null,
    permissions: [] as string[],
    useDefaultPermission: false,
    allowPublicAccess: false,
    encrypt: false,
})

const resultColumns: QTableProps["columns"] = [
    { name: "success", label: "Result", field: "success", align: "center" },
    { name: "filePath", label: "Path", field: "filePath", align: "left" },
    { name: "friendlyName", label: "Imported as", field: "friendlyName", align: "left" },
    { name: "message", label: "Message", field: "message", align: "left" },
]

const previewColumns: QTableProps["columns"] = [
    { name: "status", label: "Result", field: "canImport", align: "center" },
    { name: "filePath", label: "Path", field: "filePath", align: "left" },
    { name: "fileName", label: "Will be imported as", field: "fileName", align: "left" },
    { name: "message", label: "Note", field: "message", align: "left" },
]

const importableCount = computed(() => preview.value?.filter((p) => p.canImport).length ?? 0)

function previewStatus(row: ImportPreview) {
    if (!row.canImport) return { icon: "error", color: "negative", label: "Will be skipped" }
    if (row.message) return { icon: "warning", color: "warning", label: "Ready (with note)" }
    return { icon: "check_circle", color: "positive", label: "Ready" }
}

const effectivePermissions = computed(() => {
    const perms = [...options.value.permissions]
    if (options.value.useDefaultPermission && options.value.folder) {
        perms.push("SVMSecure." + options.value.folder.split(/[\\/]/)[0])
    }
    // The default permission can duplicate one already chosen; the import dedupes
    // server-side, so collapse it here too to avoid a doubled label in the summary.
    return [...new Set(perms)]
})

async function loadFolders() {
    const res = await get(apiURL + "folders")
    folders.value = res.success ? res.result : []
}

function buildRequest() {
    return {
        filePaths: paths.value
            .split("\n")
            .map((p) => p.trim())
            .filter((p) => p.length > 0),
        folder: options.value.folder,
        permissions: options.value.permissions,
        useDefaultPermission: options.value.useDefaultPermission,
        allowPublicAccess: options.value.allowPublicAccess,
        encrypt: options.value.encrypt,
    }
}

async function runPreview() {
    const request = buildRequest()
    if (!request.filePaths.length || !request.folder) return

    previewing.value = true
    results.value = []
    const res = await post(apiURL + "import/preview", request)
    previewing.value = false

    if (!res.success) {
        $q.notify({ type: "negative", message: res.errors?.[0] ?? "Preview failed" })
        return
    }
    preview.value = res.result
}

async function runImport() {
    const request = buildRequest()
    if (!request.filePaths.length || !request.folder) return

    importing.value = true
    const res = await post(apiURL + "import", request)
    importing.value = false

    if (!res.success) {
        $q.notify({ type: "negative", message: res.errors?.[0] ?? "Import failed" })
        return
    }
    preview.value = null
    results.value = res.result
    const succeeded = res.result.filter((r: ImportResult) => r.success).length
    $q.notify({
        type: succeeded === res.result.length ? "positive" : "warning",
        message: `${succeeded} of ${res.result.length} ${inflect("file", res.result.length)} imported`,
    })
    // Imported paths are consumed; keep failures in the textarea for fixing.
    paths.value = res.result
        .filter((r: ImportResult) => !r.success)
        .map((r: ImportResult) => r.filePath)
        .join("\n")
}

onMounted(loadFolders)
</script>

<style scoped>
.intro-copy {
    max-width: 60rem;
}

.import-form {
    max-width: 40rem;
}
</style>
