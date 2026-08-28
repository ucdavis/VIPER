<template>
    <PersonSearchSelect
        :model-value="modelValue"
        :label="label"
        :search="searchThisList"
        :option-label="(person) => person.fullName"
        outlined
        @update:model-value="emitSelection"
    >
        <!-- The address tells alike-sounding names apart, and is always set for a current
             employee, which is all this search returns. -->
        <template #option-caption="{ opt }">{{ opt.mailId }}@ucdavis.edu</template>
    </PersonSearchSelect>
</template>

<script setup lang="ts">
import PersonSearchSelect from "@/components/PersonSearchSelect.vue"
import { searchPeopleOptions } from "../services/phone-person-options-service"
import { getSparseAugmentedViperPerson } from "../composables/use-person-helper"
import type { AugmentedViperPerson } from "../types/phone-types"

// Selected people keep { iamId, fullName } so chips can show names for people
// loaded from an existing phone record (where only iamId + fullName are known).
const props = defineProps<{
    modelValue: { iamId: string; fullName: string | null }
    // Required so the combobox always has an accessible name.
    label: string
    listCode: string
}>()

const emit = defineEmits<{ "update:modelValue": [value: AugmentedViperPerson] }>()

function searchThisList(value: string) {
    return searchPeopleOptions(value, props.listCode)
}

/**
 * Single-select, so QSelect emits one person or null. Clearing the field emits the sparse
 * placeholder both dialogs seed a pristine form with, rather than null, so the form's shape
 * never changes underneath them.
 */
function emitSelection(value: AugmentedViperPerson | AugmentedViperPerson[] | null) {
    emit("update:modelValue", (value as AugmentedViperPerson | null) ?? getSparseAugmentedViperPerson())
}
</script>
