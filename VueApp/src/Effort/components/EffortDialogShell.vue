<template>
    <q-dialog
        :model-value="modelValue"
        persistent
        :aria-labelledby="titleId"
        @keydown.escape="$emit('close')"
    >
        <q-card :style="{ width: '100%', maxWidth: `${maxWidth}px` }">
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
                    @click="$emit('close')"
                />
            </q-card-section>

            <q-card-section class="q-py-sm">
                <q-form
                    ref="formRef"
                    class="effort-form"
                    greedy
                >
                    <slot />
                </q-form>
            </q-card-section>

            <DialogSubmitActions
                :submit-label="submitLabel"
                :is-saving="isSaving"
                @cancel="$emit('close')"
                @submit="$emit('submit')"
            />
        </q-card>
    </q-dialog>
</template>

<script setup lang="ts">
import { ref } from "vue"
import type { QForm } from "quasar"
import DialogSubmitActions from "./DialogSubmitActions.vue"
import "../effort-forms.css"

withDefaults(
    defineProps<{
        modelValue: boolean
        title: string
        titleId: string
        submitLabel: string
        isSaving: boolean
        maxWidth?: number
    }>(),
    { maxWidth: 550 },
)

defineEmits<{
    close: []
    submit: []
}>()

const formRef = ref<QForm | null>(null)

async function validate(): Promise<boolean> {
    return (await formRef.value?.validate(true)) ?? false
}

function resetValidation(): void {
    formRef.value?.resetValidation()
}

defineExpose({ validate, resetValidation })
</script>
