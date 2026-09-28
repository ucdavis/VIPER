<template>
    <q-card-section>
        <div
            :id="labelId"
            class="q-mb-xs"
        >
            {{ label }}
        </div>
        <div class="row q-col-gutter-sm">
            <div class="col-12 col-sm-6">
                <q-select
                    ref="selectRef"
                    v-model="selectModel"
                    :options="options"
                    dense
                    options-dense
                    outlined
                    :readonly="readOnly"
                    :clearable="clearable"
                    :display-value="selectModel?.label ?? emptyLabel"
                />
            </div>
            <div
                class="col-12 col-sm-6"
                v-if="selectModel?.isOther"
            >
                <q-input
                    v-model="otherModel"
                    :label="otherLabel"
                    dense
                    outlined
                    :readonly="readOnly"
                    maxlength="200"
                    counter
                />
            </div>
        </div>
    </q-card-section>
</template>

<script setup lang="ts">
import { ref, useId } from "vue"
import type { QSelect, QSelectProps } from "quasar"
import { useSelectAriaLabel } from "@/composables/use-select-aria-label"
import type { CareerDropdownOption } from "../types/index.ts"

defineProps<{
    label: string // The question, in full, shown above the field as its visible label.
    // The free-text field's own label. Each field on the form needs a distinct one, or a screen
    // reader announces the same label for each with nothing tying it to its question.
    otherLabel: string
    options: QSelectProps["options"]
    readOnly: boolean
    clearable?: boolean
    emptyLabel?: string
}>()

const labelId = `career-selection-label-${useId()}` // Per-instance, so the dropdowns on the form never point at each other's label.
const selectRef = ref<QSelect | null>(null)
useSelectAriaLabel(selectRef, labelId)

const selectModel = defineModel<CareerDropdownOption | null>("selectModel")
const otherModel = defineModel<string | null>("otherModel")
</script>
