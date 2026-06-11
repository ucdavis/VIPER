<template>
    <q-dialog
        :model-value="modelValue"
        aria-labelledby="file-dialog-title"
        @update:model-value="emit('update:modelValue', $event)"
        @hide="resetForm"
    >
        <q-card style="width: 600px; max-width: 95vw">
            <q-card-section class="row items-center q-pb-none">
                <div
                    id="file-dialog-title"
                    class="text-h6"
                >
                    {{ isEdit ? "Edit File" : "Upload File" }}
                </div>
                <q-space />
                <q-btn
                    icon="close"
                    flat
                    round
                    dense
                    aria-label="Close dialog"
                    v-close-popup
                />
            </q-card-section>

            <q-form
                ref="formRef"
                greedy
                @submit.prevent="save"
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
                        <q-checkbox
                            v-if="!isEdit"
                            v-model="form.makeUnique"
                            label="Rename automatically if name exists"
                        />
                    </div>
                </q-card-section>

                <q-card-actions align="right">
                    <q-btn
                        flat
                        label="Cancel"
                        dense
                        no-caps
                        v-close-popup
                    />
                    <q-btn
                        type="submit"
                        :label="isEdit ? 'Save Changes' : 'Upload'"
                        color="primary"
                        dense
                        no-caps
                        class="q-pr-md"
                        :loading="saving"
                    />
                </q-card-actions>
            </q-form>
        </q-card>
    </q-dialog>
</template>

<script setup lang="ts">
import { computed, inject, ref, watch } from "vue"
import { useQuasar } from "quasar"
import { useFetch } from "@/composables/ViperFetch"
import PermissionSelector from "@/CMS/components/PermissionSelector.vue"
import PersonSelector from "@/CMS/components/PersonSelector.vue"
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

const acceptedExtensions =
    ".pdf,.docx,.doc,.xls,.xlsx,.csv,.ppt,.pptx,.pptm,.txt,.html,.gif,.png,.jpg,.jpeg,.tiff,.mp3,.wav,.mp4,.webm,.oft,.eps,.zip,.7z,.dmg,.exe"

const isEdit = computed(() => props.file !== null)
const formRef = ref()
const saving = ref(false)

type FileForm = {
    upload: File | null
    folder: string | null
    description: string
    oldUrl: string
    allowPublicAccess: boolean
    encrypt: boolean
    makeUnique: boolean
    permissions: string[]
    people: CmsFilePerson[]
}

const emptyForm = (): FileForm => ({
    upload: null,
    folder: null,
    description: "",
    oldUrl: "",
    allowPublicAccess: false,
    encrypt: false,
    makeUnique: false,
    permissions: [],
    people: [],
})

const form = ref<FileForm>(emptyForm())

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
                makeUnique: false,
                permissions: [...props.file.permissions],
                people: props.file.people.map((p) => ({ ...p })),
            }
        } else {
            form.value = emptyForm()
        }
    },
)

function resetForm() {
    form.value = emptyForm()
    formRef.value?.resetValidation()
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

function buildFormData(): FormData {
    const data = new FormData()
    if (form.value.upload) {
        data.append("file", form.value.upload)
    }
    if (!isEdit.value) {
        data.append("folder", form.value.folder ?? "")
        data.append("makeUnique", form.value.makeUnique.toString())
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
    const valid = await formRef.value?.validate()
    if (!valid) return

    saving.value = true
    const { postForm, putForm } = useFetch()
    const res = isEdit.value
        ? await putForm(apiURL + props.file!.fileGuid, buildFormData())
        : await postForm(apiURL, buildFormData())
    saving.value = false

    if (!res.success) {
        $q.notify({
            type: "negative",
            message: res.errors?.[0] ?? `Failed to ${isEdit.value ? "save" : "upload"} file`,
        })
        return
    }

    $q.notify({ type: "positive", message: isEdit.value ? "File updated" : "File uploaded" })
    emit("saved", res.result)
    emit("update:modelValue", false)
}
</script>
