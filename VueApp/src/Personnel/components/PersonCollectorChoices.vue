<template>
    <q-select
        v-model="selected"
        :options="options"
        :label="label"
        multiple
        emit-value
        map-options
        outlined
        dense
        options-dense
        clearable
        :clear-value="[]"
    >
        <template #selected-item="scope">
            <q-chip
                removable
                dense
                square
                :tabindex="scope.tabindex"
                class="q-ml-none q-mr-xs"
                @remove="scope.removeAtIndex(scope.index)"
            >
                {{ scope.opt.label }}
            </q-chip>
        </template>
        <template #before-options>
            <q-item
                clickable
                dense
                @click="toggleAll"
            >
                <q-item-section side>
                    <q-checkbox
                        :model-value="allState"
                        dense
                        tabindex="-1"
                        @update:model-value="toggleAll"
                    />
                </q-item-section>
                <q-item-section>{{ allLabel }}</q-item-section>
            </q-item>
            <q-separator />
        </template>
    </q-select>
</template>

<script setup lang="ts">
import { computed } from "vue"
import type { PersonCollectorChoice } from "../types/person-collector-types"

/**
 * A multi-select picker for one kind of choice (departments, titles, classes), showing the
 * chosen ones as removable chips. The first menu entry selects or clears the whole list.
 */
const props = withDefaults(
    defineProps<{
        label: string
        choices: PersonCollectorChoice[]
        allLabel?: string
    }>(),
    { allLabel: "Select all" },
)

const selected = defineModel<string[]>({ required: true })

const options = computed(() => props.choices.map((choice) => ({ label: choice.label, value: choice.key })))

/** True when every choice is selected, false when none is, null (partly) otherwise. */
const allState = computed<boolean | null>(() => {
    if (selected.value.length === 0) {
        return false
    }
    return selected.value.length === props.choices.length ? true : null
})

function toggleAll() {
    selected.value = allState.value === true ? [] : props.choices.map((choice) => choice.key)
}
</script>
