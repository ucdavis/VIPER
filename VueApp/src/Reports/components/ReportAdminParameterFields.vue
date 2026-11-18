<template>
    <div class="row q-col-gutter-sm items-start">
        <q-select
            :model-value="model.name || null"
            :label="isView ? 'Column to filter *' : 'Procedure parameter *'"
            :options="targets"
            outlined
            dense
            options-dense
            class="col-12 col-sm-4"
            @update:model-value="update({ name: $event ?? '' })"
        />
        <q-input
            :model-value="model.label"
            label="Label"
            :placeholder="model.name"
            maxlength="500"
            outlined
            dense
            class="col-12 col-sm-4"
            @update:model-value="update({ label: emptyAsNull($event) })"
        />
        <q-select
            :model-value="model.binding"
            label="Value comes from"
            :options="BINDINGS"
            emit-value
            map-options
            outlined
            dense
            options-dense
            class="col-12 col-sm-4"
            @update:model-value="update({ binding: $event })"
        />
        <template v-if="model.binding === 'UserInput'">
            <q-select
                :model-value="model.type"
                label="Type"
                :options="TYPES"
                emit-value
                map-options
                outlined
                dense
                options-dense
                class="col-12 col-sm-3"
                @update:model-value="model = withType(model, $event, isView)"
            />
            <q-select
                v-if="isView"
                :model-value="model.operator"
                label="Match"
                :options="operatorOptions"
                emit-value
                map-options
                outlined
                dense
                options-dense
                class="col-12 col-sm-3"
                @update:model-value="update({ operator: $event })"
            />
            <q-input
                :model-value="model.defaultValue"
                label="Default"
                :hint="DEFAULT_HINTS[model.type]"
                maxlength="500"
                outlined
                dense
                class="col-12 col-sm-3"
                @update:model-value="update({ defaultValue: emptyAsNull($event) })"
            />
            <div class="col-12 col-sm-3 q-pt-md">
                <q-checkbox
                    :model-value="model.required"
                    label="Required"
                    dense
                    @update:model-value="update({ required: $event })"
                />
            </div>
            <ReportAdminChoices
                v-if="model.type === 'Choice' || model.type === 'MultiChoice'"
                v-model="model"
                :views="views"
                class="col-12"
            />
        </template>
        <p
            v-else
            class="col-12 text-grey q-mb-none"
        >
            Filled in from the signed-in user, who can't change it.
        </p>
    </div>
</template>

<script setup lang="ts">
import { computed } from "vue"
import ReportAdminChoices from "./ReportAdminChoices.vue"
import { operatorsFor, withType } from "../admin/report-admin-form"
import type { FormParameter } from "../admin/report-admin-form"
import type { ReportFilterOperator, ReportParameterBinding } from "../admin/report-admin-types"
import type { ReportParameterType } from "@/shared/report-builder/report-builder-types"

/** One parameter: what it fills or filters, its label and type, and where its value comes from. */
const { targets, isView, views } = defineProps<{
    /** The view's columns or the procedure's parameters. */
    targets: string[]
    isView: boolean
    /** The [report] views, for choice lists read from a view. */
    views: string[]
}>()

const model = defineModel<FormParameter>({ required: true })

const BINDINGS: { value: ReportParameterBinding; label: string }[] = [
    { value: "UserInput", label: "The user" },
    { value: "CurrentUserLoginId", label: "The user's login ID" },
    { value: "CurrentUserMothraId", label: "The user's Mothra ID" },
]

const TYPES: { value: ReportParameterType; label: string }[] = [
    { value: "Text", label: "Text" },
    { value: "Number", label: "Number" },
    { value: "Date", label: "Date" },
    { value: "Boolean", label: "Yes/No" },
    { value: "Choice", label: "One choice" },
    { value: "MultiChoice", label: "Several choices" },
]

const OPERATOR_LABELS: Record<ReportFilterOperator, string> = {
    Equals: "Equals",
    Contains: "Contains",
    From: "On or after (from)",
    To: "On or before (to)",
    In: "Any of",
}

const DEFAULT_HINTS: Record<ReportParameterType, string> = {
    Text: "",
    Number: "A number, such as 2026",
    Date: "YYYY-MM-DD",
    Boolean: "true or false",
    Choice: "One of the choices' values",
    MultiChoice: "One of the choices' values",
}

const operatorOptions = computed(() =>
    operatorsFor(model.value.type).map((operator) => ({ value: operator, label: OPERATOR_LABELS[operator] })),
)

function emptyAsNull(value: string | number | null): string | null {
    return value === null || String(value).trim() === "" ? null : String(value)
}

function update(change: Partial<FormParameter>): void {
    model.value = { ...model.value, ...change }
}
</script>
