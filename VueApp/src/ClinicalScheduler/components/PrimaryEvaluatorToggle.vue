<template>
    <div class="q-mt-sm q-mb-md">
        <div
            class="row items-center"
            :class="{ 'opacity-50': disabled }"
        >
            <q-checkbox
                :model-value="modelValue"
                label="Make primary evaluator"
                color="primary"
                size="sm"
                dense
                :disable="disabled"
                @update:model-value="emit('update:modelValue', $event)"
            />
            <span
                v-if="showHelperText"
                class="text-caption text-grey-6 q-ml-sm"
            >
                ({{ helperText }})
            </span>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed } from "vue"

const props = withDefaults(
    defineProps<{
        modelValue: boolean
        selectionCount: number
        itemType?: "clinician" | "rotation"
    }>(),
    {
        itemType: "clinician",
    },
)

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
}>()

const disabled = computed(() => props.selectionCount !== 1)

const showHelperText = computed(() => props.selectionCount !== 1)

const helperText = computed(() => {
    if (props.selectionCount === 0) {
        return `Select a ${props.itemType} first`
    }
    return `Only available when single ${props.itemType} is selected`
})
</script>
