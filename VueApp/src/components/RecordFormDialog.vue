<template>
    <q-dialog
        :model-value="modelValue"
        persistent
        :aria-labelledby="titleId"
        @update:model-value="emit('update:modelValue', $event)"
        @hide="onHide"
        @keydown.escape="handleClose"
    >
        <q-card class="dialog-card-md">
            <q-card-section class="row items-center q-pb-none">
                <div
                    :id="titleId"
                    class="text-h6"
                >
                    {{ title }}
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
                @submit.prevent="emit('submit')"
                @validation-error="emit('validation-error')"
            >
                <q-card-section class="q-gutter-y-sm">
                    <slot />

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
                        :label="isEdit ? 'Save Changes' : submitLabel"
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
                            {{ isEdit ? "Save Changes" : submitLabel }}
                        </template>
                    </q-btn>
                </q-card-actions>
            </q-form>
        </q-card>
    </q-dialog>
</template>

<script setup lang="ts">
import { ref } from "vue"
import StatusBanner from "@/components/StatusBanner.vue"

const props = defineProps<{
    modelValue: boolean
    titleId: string
    title: string
    isEdit: boolean
    saving: boolean
    formError: string
    submitLabel: string
    /** Optional guard run before closing (e.g. an unsaved-changes prompt); closes unconditionally if omitted. */
    confirmClose?: () => Promise<boolean>
}>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    submit: []
    "validation-error": []
    hide: []
}>()

const formRef = ref()

async function handleClose() {
    if (props.confirmClose && !(await props.confirmClose())) return
    emit("update:modelValue", false)
}

// Which fields are invalid are stored in formRef and reset here.
// The actual form data reset varies by form and so must be handled
// elsewhere, triggered by the 'hide' event,
function onHide() {
    formRef.value?.resetValidation()
    emit("hide")
}
</script>
