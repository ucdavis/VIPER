<template>
    <q-dialog
        :model-value="modelValue"
        persistent
        aria-labelledby="file-dialog-title"
        @update:model-value="emit('update:modelValue', $event)"
        @hide="resetForm"
        @keydown.escape="handleClose"
    >
        <q-card class="dialog-card-md">
            <q-card-section class="row items-center q-pb-none">
                <div
                    id="file-dialog-title"
                    class="text-h6"
                >
                    {{ isEdit ? "Edit File" : "Add File" }}
                </div>
                <q-space />
                <q-btn
                    icon="close"
                    flat
                    round
                    dense
                    aria-label="Close dialog"
                    @click="handleClose"
                />
            </q-card-section>

            <q-form
                ref="formRef"
                greedy
                @submit.prevent="save"
                @validation-error="onValidationError"
            >
                <q-card-section class="q-gutter-y-sm">
                    <div
                        v-if="isEdit"
                        class="text-body2 q-mb-sm"
                    >
                        <strong>{{ file?.friendlyName }}</strong>
                        <div class="text-caption text-grey-8">
                            Link:
                            <a
                                :href="file?.friendlyUrl"
                                target="_blank"
                                rel="noopener"
                            >
                                {{ file?.friendlyUrl }}
                            </a>
                            <q-btn
                                flat
                                dense
                                size="sm"
                                icon="content_copy"
                                aria-label="Copy link"
                                @click="copyUrl"
                            >
                                <q-tooltip>Copy link</q-tooltip>
                            </q-btn>
                        </div>
                    </div>

                    <q-file
                        v-model="form.upload"
                        dense
                        outlined
                        :label="isEdit ? 'Replace file (optional)' : 'File'"
                        :accept="acceptedExtensions"
                        :rules="isEdit ? [] : [(v: File | null) => !!v || 'Please choose a file']"
                        :hint="
                            isEdit
                                ? 'Uploading a new file replaces the current content, keeping the same name'
                                : undefined
                        "
                    >
                        <template #prepend>
                            <q-icon name="attach_file" />
                        </template>
                    </q-file>

                    <q-select
                        v-if="!isEdit"
                        v-model="form.folder"
                        dense
                        options-dense
                        outlined
                        label="VIPER app (folder)"
                        :options="folders"
                        :rules="[(v: string | null) => !!v || 'Please select a folder']"
                    />

                    <q-input
                        v-model="form.description"
                        dense
                        outlined
                        type="textarea"
                        rows="2"
                        label="Description"
                        maxlength="1000"
                    />

                    <q-input
                        v-model="form.oldUrl"
                        dense
                        outlined
                        label="Old URL"
                        maxlength="256"
                        hint="Legacy VIPER 1 path this file replaces (optional)"
                    />

                    <PermissionSelector v-model="form.permissions" />

                    <PersonSelector
                        v-model="form.people"
                        label="People with access"
                    />

                    <div class="row q-gutter-x-lg">
                        <q-toggle
                            v-model="form.allowPublicAccess"
                            label="Public access"
                        />
                        <q-toggle
                            v-model="form.encrypt"
                            label="Encrypt file"
                        />
                    </div>

                    <StatusBanner
                        v-if="formError"
                        type="error"
                    >
                        {{ formError }}
                    </StatusBanner>
                </q-card-section>

                <q-card-actions align="right">
                    <q-btn
                        flat
                        label="Cancel"
                        dense
                        no-caps
                        @click="handleClose"
                    />
                    <q-btn
                        type="submit"
                        :label="isEdit ? 'Save Changes' : 'Upload'"
                        color="primary"
                        dense
                        no-caps
                        :loading="saving"
                    >
                        <template #loading>
                            <q-spinner
                                size="1em"
                                class="q-mr-sm"
                            />
                            {{ isEdit ? "Save Changes" : "Upload" }}
                        </template>
                    </q-btn>
                </q-card-actions>
            </q-form>

            <q-dialog
                v-model="showConflict"
                persistent
                aria-labelledby="conflict-dialog-title"
            >
                <q-card class="dialog-card-sm">
                    <q-card-section class="row items-center q-pb-none">
                        <div
                            id="conflict-dialog-title"
                            class="text-h6"
                        >
                            File name already exists
                        </div>
                        <q-space />
                        <q-btn
                            icon="close"
                            flat
                            round
                            dense
                            aria-label="Close dialog"
                            @click="showConflict = false"
                        />
                    </q-card-section>
                    <q-card-section>
                        <p class="text-body2">
                            <strong>{{ form.upload?.name }}</strong> already exists in <strong>{{ form.folder }}</strong
                            >{{ conflictDetail }}. Choose how to continue:
                        </p>
                        <q-option-group
                            v-model="conflictChoice"
                            :options="conflictOptions"
                        />
                        <q-input
                            v-if="conflictChoice === 'rename'"
                            v-model="renameTo"
                            dense
                            outlined
                            label="New file name"
                            class="q-mt-sm"
                            :rules="[(v: string) => !!v?.trim() || 'Enter a file name']"
                            hide-bottom-space
                        />
                    </q-card-section>
                    <q-card-actions align="right">
                        <q-btn
                            color="primary"
                            dense
                            no-caps
                            :label="conflictChoice === 'rename' ? 'Upload with new name' : 'Overwrite'"
                            :loading="saving"
                            @click="resolveConflict"
                        >
                            <template #loading>
                                <q-spinner
                                    size="1em"
                                    class="q-mr-sm"
                                />
                                {{ conflictChoice === "rename" ? "Upload with new name" : "Overwrite" }}
                            </template>
                        </q-btn>
                    </q-card-actions>
                </q-card>
            </q-dialog>
        </q-card>
    </q-dialog>
</template>

<script setup lang="ts">
import { computed, inject, ref, watch } from "vue"
import { useQuasar } from "quasar"
import { useFetch } from "@/composables/ViperFetch"
import { useUnsavedChanges } from "@/composables/use-unsaved-changes"
import PermissionSelector from "@/CMS/components/PermissionSelector.vue"
import PersonSelector from "@/CMS/components/PersonSelector.vue"
import StatusBanner from "@/components/StatusBanner.vue"
import { CMS_ACCEPTED_EXTENSIONS } from "@/CMS/file-types"
import type { CmsFile, CmsFilePerson } from "@/CMS/types/"

const props = defineProps<{
    modelValue: boolean
    folders: string[]
    file: CmsFile | null
}>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    saved: [file: CmsFile]
}>()

const apiURL = inject("apiURL") + "cms/files/"
const $q = useQuasar()
const { get, postForm, putForm, createUrlSearchParams } = useFetch()

const acceptedExtensions = CMS_ACCEPTED_EXTENSIONS

const isEdit = computed(() => props.file !== null)
const formRef = ref()
const saving = ref(false)
const formError = ref("")

type FileForm = {
    upload: File | null
    folder: string | null
    description: string
    oldUrl: string
    allowPublicAccess: boolean
    encrypt: boolean
    permissions: string[]
    people: CmsFilePerson[]
}

type NameCheck = {
    inUse: boolean
    suggestedName: string
    existingFileGuid: string | null
    existingFriendlyName: string | null
    existingDeleted: boolean
}

const emptyForm = (): FileForm => ({
    upload: null,
    folder: null,
    description: "",
    oldUrl: "",
    allowPublicAccess: false,
    encrypt: false,
    permissions: [],
    people: [],
})

const form = ref<FileForm>(emptyForm())

const { setInitialState, confirmClose } = useUnsavedChanges(form)

const conflict = ref<NameCheck | null>(null)
const showConflict = ref(false)
const conflictChoice = ref<"rename" | "overwrite">("rename")
const renameTo = ref("")

const conflictOptions = computed(() => [
    { label: "Upload with a new name", value: "rename" },
    {
        label: conflict.value?.existingFileGuid
            ? "Overwrite the existing file (replaces its content and details)"
            : "Overwrite the existing file on disk",
        value: "overwrite",
    },
])

const conflictDetail = computed(() => {
    if (!conflict.value?.existingFriendlyName) return ""
    return ` (${conflict.value.existingFriendlyName}${conflict.value.existingDeleted ? ", deleted" : ""})`
})

watch(
    () => [props.modelValue, props.file] as const,
    ([open]) => {
        if (!open) return
        if (props.file) {
            form.value = {
                upload: null,
                folder: props.file.folder,
                description: props.file.description ?? "",
                oldUrl: props.file.oldUrl ?? "",
                allowPublicAccess: props.file.allowPublicAccess,
                encrypt: props.file.encrypted,
                permissions: [...props.file.permissions],
                people: props.file.people.map((p) => ({ ...p })),
            }
        } else {
            form.value = emptyForm()
        }
        formError.value = ""
        setInitialState()
    },
)

function resetForm() {
    form.value = emptyForm()
    conflict.value = null
    showConflict.value = false
    formError.value = ""
    formRef.value?.resetValidation()
}

async function handleClose() {
    if (await confirmClose()) {
        emit("update:modelValue", false)
    }
}

// The q-form focuses the first invalid field on a failed submit; this surfaces a matching
// message next to the action buttons so the failure is obvious.
function onValidationError() {
    formError.value = isEdit.value
        ? "Please complete the required fields before saving."
        : "Please choose a file and complete the required fields before uploading."
}

async function copyUrl() {
    if (!props.file) return
    try {
        await navigator.clipboard.writeText(props.file.friendlyUrl)
        $q.notify({ type: "positive", message: "Link copied" })
    } catch {
        $q.notify({ type: "negative", message: "Failed to copy link" })
    }
}

type SubmitOptions = {
    fileName?: string
    overwrite?: boolean
    overwriteGuid?: string
}

function buildFormData(opts: SubmitOptions = {}): FormData {
    const data = new FormData()
    if (form.value.upload) {
        data.append("file", form.value.upload)
    }
    if (!isEdit.value && !opts.overwriteGuid) {
        data.append("folder", form.value.folder ?? "")
        if (opts.fileName) {
            data.append("fileName", opts.fileName)
        }
        if (opts.overwrite) {
            data.append("overwrite", "true")
        }
    }
    data.append("description", form.value.description)
    data.append("oldUrl", form.value.oldUrl)
    data.append("allowPublicAccess", form.value.allowPublicAccess.toString())
    data.append("encrypt", form.value.encrypt.toString())
    for (const p of form.value.permissions) {
        data.append("permissions", p)
    }
    for (const person of form.value.people) {
        data.append("iamIds", person.iamId)
    }
    return data
}

async function save() {
    if (saving.value) return
    formError.value = ""

    // New uploads check the destination name first; a conflict prompts for rename/overwrite.
    if (!isEdit.value && form.value.upload && form.value.folder) {
        saving.value = true
        const params = createUrlSearchParams({ folder: form.value.folder, fileName: form.value.upload.name })
        const check = await get(apiURL + "check-name?" + params)
        saving.value = false
        if (check.success && check.result.inUse) {
            conflict.value = check.result
            conflictChoice.value = "rename"
            renameTo.value = check.result.suggestedName
            showConflict.value = true
            return
        }
    }
    await submitSave()
}

async function resolveConflict() {
    if (saving.value) return
    // Keep the conflict dialog open until the upload succeeds, so its buttons show the
    // loading state and a failure leaves the user here instead of stranded on the form.
    if (conflictChoice.value === "rename") {
        const name = renameTo.value.trim()
        if (!name) return
        if (await submitSave({ fileName: name })) showConflict.value = false
        return
    }
    const opts: SubmitOptions = conflict.value?.existingFileGuid
        ? { overwriteGuid: conflict.value.existingFileGuid }
        : { overwrite: true }
    if (await submitSave(opts)) showConflict.value = false
}

async function submitSave(opts: SubmitOptions = {}): Promise<boolean> {
    if (saving.value) return false
    saving.value = true
    let res
    if (isEdit.value) {
        res = await putForm(apiURL + props.file!.fileGuid, buildFormData())
    } else if (opts.overwriteGuid) {
        // Overwriting a managed file replaces the existing record's content and details.
        res = await putForm(apiURL + opts.overwriteGuid, buildFormData(opts))
    } else {
        res = await postForm(apiURL, buildFormData(opts))
    }
    saving.value = false

    if (!res.success) {
        const message = res.errors?.[0] ?? `Failed to ${isEdit.value ? "save" : "upload"} file`
        // During conflict resolution the user is in the conflict sub-dialog, so a toast is the
        // only place they'd see the error; otherwise surface it on the main form banner.
        if (showConflict.value) {
            $q.notify({ type: "negative", message })
        } else {
            formError.value = message
        }
        return false
    }

    const overwrote = opts.overwrite || opts.overwriteGuid
    $q.notify({
        type: "positive",
        message: isEdit.value ? "File updated" : overwrote ? "File overwritten" : "File uploaded",
    })
    emit("saved", res.result)
    emit("update:modelValue", false)
    return true
}
</script>
