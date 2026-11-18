<template>
    <div>
        <q-option-group
            :model-value="model.choicesFrom"
            :options="SOURCES"
            inline
            dense
            class="q-mb-sm"
            @update:model-value="update({ choicesFrom: $event })"
        />
        <q-input
            v-if="model.choicesFrom === 'list'"
            :model-value="model.choicesText"
            label="Choices"
            hint="One per line: value | label (the label is optional)"
            type="textarea"
            autogrow
            outlined
            dense
            @update:model-value="update({ choicesText: String($event ?? '') })"
        />
        <q-select
            v-else
            :model-value="model.choicesSource"
            label="View with Value and Label columns"
            :options="views"
            outlined
            dense
            options-dense
            @update:model-value="update({ choicesSource: $event })"
        />
    </div>
</template>

<script setup lang="ts">
import type { ChoicesFrom, FormParameter } from "../admin/report-admin-form"

/** A choice parameter's choices: a fixed list typed here, or a [report] view to read them from. */
defineProps<{ views: string[] }>()

const model = defineModel<FormParameter>({ required: true })

const SOURCES: { value: ChoicesFrom; label: string }[] = [
    { value: "list", label: "A fixed list" },
    { value: "view", label: "A view" },
]

function update(change: Partial<FormParameter>): void {
    model.value = { ...model.value, ...change }
}
</script>
