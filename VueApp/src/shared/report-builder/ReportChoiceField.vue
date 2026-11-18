<template>
    <q-select
        :model-value="modelValue"
        :label="label"
        :options="options"
        :multiple="multiple"
        :use-chips="multiple"
        :clearable="!required"
        :rules="rules"
        option-value="value"
        option-label="label"
        emit-value
        map-options
        use-input
        input-debounce="0"
        outlined
        dense
        options-dense
        @filter="filterChoices"
        @update:model-value="emit('update:modelValue', $event)"
    >
        <template #no-option>
            <q-item>
                <q-item-section class="text-grey">No matching choices</q-item-section>
            </q-item>
        </template>
    </q-select>
</template>

<script setup lang="ts">
import { ref, watch } from "vue"
import type { ValidationRule } from "quasar"
import type { ReportChoice, ReportParameterValue } from "./report-builder-types"

/**
 * A choice parameter. Lists can hold up to a thousand choices, so typing narrows them.
 */
const {
    modelValue,
    label,
    choices,
    multiple = false,
    required = false,
    rules = [],
} = defineProps<{
    modelValue: ReportParameterValue
    label: string
    choices: ReportChoice[]
    multiple?: boolean
    required?: boolean
    rules?: ValidationRule[]
}>()

const emit = defineEmits<{ (e: "update:modelValue", value: ReportParameterValue): void }>()

const options = ref<ReportChoice[]>(choices)

watch(
    () => choices,
    (next) => {
        options.value = next
    },
)

/** Keeps the choices whose label contains the typed text, ignoring case. */
function filterChoices(text: string, update: (callback: () => void) => void): void {
    update(() => {
        const needle = text.trim().toLowerCase()
        options.value =
            needle === "" ? choices : choices.filter((choice) => choice.label.toLowerCase().includes(needle))
    })
}
</script>
