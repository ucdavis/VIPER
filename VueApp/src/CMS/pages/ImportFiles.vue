<template>
    <div class="q-pa-md">
        <div class="row items-center q-mb-md">
            <h1 class="q-my-none">Import Files from VIPER</h1>
            <q-space />
            <q-btn
                flat
                dense
                no-caps
                color="primary"
                icon="arrow_back"
                label="Back to Files"
                :to="{ name: 'CmsFiles' }"
            />
        </div>

        <p
            class="text-body2 text-grey-8"
            style="max-width: 60rem"
        >
            Moves files out of the legacy VIPER webroot into the managed file store. The original path is saved as the
            file's Old URL, so existing links keep working through the file handler. Enter one path per line, relative
            to the legacy webroot (e.g.
            <code>/cats/docs/manual.pdf</code>).
        </p>

        <q-form
            greedy
            @submit.prevent="runImport"
        >
            <div class="row q-col-gutter-lg">
                <div class="col-12 col-md-7">
                    <q-input
                        v-model="paths"
                        outlined
                        type="textarea"
                        rows="12"
                        label="File paths (one per line)"
                        :rules="[(v: string | null) => (v && v.trim().length > 0) || 'Enter at least one path']"
                        hide-bottom-space
                    />
                </div>
                <div class="col-12 col-md-5">
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
                    <div class="q-mt-md">
                        <q-btn
                            type="submit"
                            color="primary"
                            label="Import Files"
                            dense
                            no-caps
                            class="q-pr-md"
                            :loading="importing"
                        />
                    </div>
                </div>
            </div>
        </q-form>

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
                    </q-td>
                </template>
            </q-table>
        </template>
    </div>
</template>

<script setup lang="ts">
import { inject, onMounted, ref } from "vue"
import { useQuasar, type QTableProps } from "quasar"
import { useFetch } from "@/composables/ViperFetch"
import PermissionSelector from "@/CMS/components/PermissionSelector.vue"

type ImportResult = {
    filePath: string
    success: boolean
    message: string | null
    fileGuid: string | null
    friendlyName: string | null
}

const apiURL = inject("apiURL") + "cms/files/"
const $q = useQuasar()
const { get, post } = useFetch()

const folders = ref<string[]>([])
const paths = ref("")
const importing = ref(false)
const results = ref<ImportResult[]>([])

const options = ref({
    folder: null as string | null,
    permissions: [] as string[],
    useDefaultPermission: false,
    allowPublicAccess: false,
    encrypt: false,
})

const resultColumns: QTableProps["columns"] = [
    { name: "success", label: "", field: "success", align: "center" },
    { name: "filePath", label: "Path", field: "filePath", align: "left" },
    { name: "friendlyName", label: "Imported as", field: "friendlyName", align: "left" },
    { name: "message", label: "Message", field: "message", align: "left" },
]

async function loadFolders() {
    const res = await get(apiURL + "folders")
    folders.value = res.success ? res.result : []
}

async function runImport() {
    const filePaths = paths.value
        .split("\n")
        .map((p) => p.trim())
        .filter((p) => p.length > 0)
    if (!filePaths.length || !options.value.folder) return

    importing.value = true
    const res = await post(apiURL + "import", {
        filePaths,
        folder: options.value.folder,
        permissions: options.value.permissions,
        useDefaultPermission: options.value.useDefaultPermission,
        allowPublicAccess: options.value.allowPublicAccess,
        encrypt: options.value.encrypt,
    })
    importing.value = false

    if (!res.success) {
        $q.notify({ type: "negative", message: res.errors?.[0] ?? "Import failed" })
        return
    }
    results.value = res.result
    const succeeded = res.result.filter((r: ImportResult) => r.success).length
    $q.notify({
        type: succeeded === res.result.length ? "positive" : "warning",
        message: `${succeeded} of ${res.result.length} files imported`,
    })
    // Imported paths are consumed; keep failures in the textarea for fixing.
    paths.value = res.result
        .filter((r: ImportResult) => !r.success)
        .map((r: ImportResult) => r.filePath)
        .join("\n")
}

onMounted(loadFolders)
</script>
