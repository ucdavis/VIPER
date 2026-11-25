<template>
    <section
        aria-labelledby="report-admin-parameters"
        class="q-mb-lg"
    >
        <h2 id="report-admin-parameters">Parameters</h2>
        <p class="text-grey">
            {{
                isView
                    ? "Filters the user fills in, each on one of the view's columns."
                    : "Values passed to the procedure."
            }}
        </p>
        <SortableList
            v-model="parameters"
            item-key="key"
            :announce="
                (parameter: FormParameter, index: number, total: number) =>
                    `${parameter.name || 'Parameter'} moved to position ${index + 1} of ${total}`
            "
        >
            <template #row="{ item, index }">
                <ReportAdminParameterFields
                    :model-value="item"
                    :targets="targets"
                    :is-view="isView"
                    :views="views"
                    @update:model-value="replace(index, $event)"
                />
            </template>
            <template #actions="{ item, index }">
                <q-btn
                    dense
                    flat
                    size="sm"
                    color="negative"
                    icon="delete"
                    :aria-label="`Remove ${item.name || 'parameter'}`"
                    @click="remove(index)"
                />
            </template>
            <template #empty>
                <p class="text-grey">No parameters: the report shows every row the source returns.</p>
            </template>
        </SortableList>
        <q-btn
            flat
            no-caps
            icon="add"
            label="Add parameter"
            color="primary"
            class="q-mt-sm"
            @click="parameters = [...parameters, newParameter(isView)]"
        />
    </section>
</template>

<script setup lang="ts">
import SortableList from "@/components/SortableList.vue"
import ReportAdminParameterFields from "./ReportAdminParameterFields.vue"
import { newParameter } from "../admin/report-admin-form"
import type { FormParameter } from "../admin/report-admin-form"

/** The report's parameters, in the order the form shows them. */
defineProps<{
    targets: string[]
    isView: boolean
    views: string[]
}>()

const parameters = defineModel<FormParameter[]>({ required: true })

function replace(index: number, parameter: FormParameter): void {
    parameters.value = parameters.value.map((current, at) => (at === index ? parameter : current))
}

function remove(index: number): void {
    parameters.value = parameters.value.filter((_, at) => at !== index)
}
</script>
