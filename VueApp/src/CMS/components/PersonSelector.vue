<template>
    <PersonSearchSelect
        :model-value="modelValue"
        :label="label"
        :search="searchPeopleOptions"
        :option-label="(person) => person.name"
        multiple
        @update:model-value="emitSelection"
    >
        <template #option-caption="{ opt }">{{ opt.loginId ?? opt.iamId }}</template>
    </PersonSearchSelect>
</template>

<script setup lang="ts">
import PersonSearchSelect from "@/components/PersonSearchSelect.vue"
import { searchPeopleOptions } from "@/CMS/services/cms-options-service"
import type { CmsPersonOption } from "@/CMS/types/"

type SelectedPerson = { iamId: string; name: string | null }

// Selected people keep { iamId, name } so chips can show names for people
// loaded from an existing file (where only iamId + name are known).
defineProps<{
    modelValue: SelectedPerson[]
    // Required so the combobox always has an accessible name.
    label: string
}>()

const emit = defineEmits<{ "update:modelValue": [value: SelectedPerson[]] }>()

/** Multi-select, so QSelect emits an array; clearing the last chip emits null. */
function emitSelection(value: CmsPersonOption | CmsPersonOption[] | null) {
    emit("update:modelValue", (value as SelectedPerson[] | null) ?? [])
}
</script>
