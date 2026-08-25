<template>
    <q-select
        :model-value="modelValue"
        multiple
        use-chips
        use-input
        input-debounce="300"
        dense
        options-dense
        :label="label"
        :options="options"
        :loading="loading"
        option-value="iamId"
        option-label="name"
        hint="Type at least 2 characters to search people"
        @update:model-value="emit('update:modelValue', $event ?? [])"
        @filter="searchPeople"
    >
        <template #option="{ itemProps, opt }">
            <q-item v-bind="itemProps">
                <q-item-section>
                    <q-item-label>{{ opt.name }}</q-item-label>
                    <q-item-label caption>{{ opt.loginId ?? opt.iamId }}</q-item-label>
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
                {{ scope.opt.name ?? scope.opt.iamId }}
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
import { searchPeopleOptions } from "@/CMS/services/cms-options-service"
import { usePersonSearch } from "@/composables/use-person-search"
import type { CmsPersonOption } from "@/CMS/types/"

// Selected people keep { iamId, name } so chips can show names for people
// loaded from an existing file (where only iamId + name are known).
defineProps<{
    modelValue: { iamId: string; name: string | null }[]
    // Required so the combobox always has an accessible name.
    label: string
}>()

const emit = defineEmits<{ "update:modelValue": [value: { iamId: string; name: string | null }[]] }>()

const { options, loading, searchPeople } = usePersonSearch<CmsPersonOption>(searchPeopleOptions)
</script>
