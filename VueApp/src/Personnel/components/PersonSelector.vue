<template>
    <q-select
        :model-value="modelValue"
        use-input
        use-chips
        input-debounce="300"
        dense
        options-dense
        :label="label"
        :options="options"
        :loading="loading"
        option-value="iamId"
        option-label="fullName"
        hint="Type at least 2 characters to search people"
        @update:model-value="emit('update:modelValue', $event ?? getSparseAugmentedViperPerson())"
        @filter="searchPeople"
    >
        <template #option="{ itemProps, opt }">
            <q-item v-bind="itemProps">
                <q-item-section>
                    <q-item-label>{{ opt.fullName }}</q-item-label>
                </q-item-section>
            </q-item>
        </template>
        <template #selected-item="scope">
            <q-chip
                removable
                dense
                :tabindex="scope.tabindex"
                @remove="scope.removeAtIndex(scope.index)"
            >
                {{ scope.opt.fullName ?? scope.opt.iamId }}
            </q-chip>
        </template>
        <template #no-option>
            <q-item>
                <q-item-section class="text-grey">No matching people</q-item-section>
            </q-item>
        </template>
    </q-select>
</template>

<script setup lang="ts">
import { searchPeopleOptions } from "../services/phone-person-options-service"
import { usePersonSearch } from "@/composables/use-person-search"
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

const { options, loading, searchPeople } = usePersonSearch<AugmentedViperPerson>((val) =>
    searchPeopleOptions(val, props.listCode),
)
</script>
