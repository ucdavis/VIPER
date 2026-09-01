<template>
    <q-select
        :model-value="modelValue"
        :multiple="multiple"
        :outlined="outlined"
        use-input
        use-chips
        input-debounce="300"
        dense
        options-dense
        :label="label"
        :options="options"
        :loading="loading"
        option-value="iamId"
        :option-label="displayName"
        hint="Type at least 2 characters to search people"
        @update:model-value="emit('update:modelValue', $event)"
        @filter="searchPeople"
    >
        <template #option="{ itemProps, opt }">
            <q-item v-bind="itemProps">
                <q-item-section>
                    <q-item-label>{{ displayName(opt) }}</q-item-label>
                    <!-- The search is capped and matches on name, so a common surname fills the
                         list with rows that read alike. What tells them apart differs by caller,
                         so each supplies its own caption. -->
                    <q-item-label caption>
                        <slot
                            name="option-caption"
                            :opt="opt"
                        />
                    </q-item-label>
                </q-item-section>
            </q-item>
        </template>
        <!-- A caller whose "nobody selected" value is a sparse record rather than null still gets
             one selected item from QSelect, which wraps any non-null model value. Without this
             guard a pristine form shows a removable chip with nothing in it. -->
        <template #selected-item="scope">
            <q-chip
                v-if="scope.opt.iamId"
                removable
                dense
                :tabindex="scope.tabindex"
                @remove="scope.removeAtIndex(scope.index)"
            >
                {{ displayName(scope.opt) }}
            </q-chip>
        </template>
        <template #no-option>
            <q-item>
                <q-item-section class="text-grey">No matching people</q-item-section>
            </q-item>
        </template>
    </q-select>
</template>

<script setup lang="ts" generic="TOption extends { iamId: string }, TValue extends { iamId: string } = TOption">
import { usePersonSearch } from "@/composables/use-person-search"

/**
 * The QSelect half of a person picker: the search wiring, the min-length hint, the option and
 * chip rendering, and the empty state. Every area's picker wants all of that identically and
 * differs only in what it searches, what names a person, and whether one person or several may
 * be picked - so those are props, and the caption is a slot because what distinguishes two
 * similarly named people is not the same question in every area.
 *
 * Callers wrap this rather than using it directly, so that each keeps a concrete model type:
 * `multiple` decides between one person and several, which a single generic signature cannot
 * express, and the one cast that costs belongs in a wrapper that knows the answer rather than at
 * every call site.
 *
 * The two type parameters are not the same thing. TOption is what a search returns - a whole
 * person record. TValue is what a saved record stored about a person, often just an id and a
 * name, which is all a form has to render before anyone searches. So the field holds a TValue
 * and hands back a TOption once a choice is made.
 */
const props = defineProps<{
    /** One for a single-select caller, an array for a `multiple` one. */
    modelValue: TValue | TValue[] | null
    /** Required so the combobox always has an accessible name. */
    label: string
    search: (value: string) => Promise<TOption[] | null>
    /** What to show for a person, in the option list and on the chip. */
    optionLabel: (person: TOption | TValue) => string | null
    multiple?: boolean
    outlined?: boolean
}>()

const emit = defineEmits<{ "update:modelValue": [value: TOption | TOption[] | null] }>()

const { options, loading, searchPeople } = usePersonSearch<TOption>((value) => props.search(value))

/**
 * Falls back to the IAM ID: a person carried in from a saved record has only what that record
 * stored, which may not include a name.
 */
function displayName(person: TOption | TValue): string {
    return props.optionLabel(person) ?? person.iamId
}
</script>
