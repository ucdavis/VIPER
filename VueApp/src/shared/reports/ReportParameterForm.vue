<template>
    <q-form
        class="row q-col-gutter-md items-start q-mb-md"
        @submit="emit('submit')"
    >
        <div
            v-for="parameter in parameters"
            :key="parameter.name"
            class="col-12 col-sm-6 col-md-4"
        >
            <q-select
                v-if="parameter.type === 'Choice' || parameter.type === 'MultiChoice'"
                :model-value="model[parameter.name]"
                :label="fieldLabel(parameter)"
                :options="parameter.options"
                :multiple="parameter.type === 'MultiChoice'"
                :use-chips="parameter.type === 'MultiChoice'"
                :clearable="!parameter.required"
                :rules="rules(parameter)"
                option-value="value"
                option-label="label"
                emit-value
                map-options
                outlined
                dense
                options-dense
                @update:model-value="update(parameter.name, $event)"
            />
            <q-checkbox
                v-else-if="parameter.type === 'Boolean'"
                :model-value="model[parameter.name] === true"
                :label="parameter.label"
                @update:model-value="update(parameter.name, $event)"
            />
            <q-input
                v-else
                :model-value="inputValue(parameter.name)"
                :label="fieldLabel(parameter)"
                :type="INPUT_TYPES[parameter.type]"
                :rules="rules(parameter)"
                :clearable="parameter.type === 'Text' && !parameter.required"
                stack-label
                outlined
                dense
                @update:model-value="update(parameter.name, toValue(parameter, $event))"
            />
        </div>
        <div class="col-12">
            <q-btn
                type="submit"
                color="primary"
                label="Run report"
                no-caps
                :loading="running"
            >
                <template #loading>
                    <q-spinner
                        size="1em"
                        class="q-mr-sm"
                    />
                    Run report
                </template>
            </q-btn>
        </div>
    </q-form>
</template>

<script setup lang="ts">
import { isEmptyValue } from "./report-query"
import type {
    ReportParameterMetadata,
    ReportParameterType,
    ReportParameterValue,
    ReportParameterValues,
} from "./report-types"

/**
 * One input per report parameter, chosen by its type. Required fields are checked here for quick
 * feedback; the server checks them again, along with every other rule.
 */
defineProps<{
    parameters: ReportParameterMetadata[]
    running?: boolean
}>()

const emit = defineEmits<{ (e: "submit"): void }>()

const model = defineModel<ReportParameterValues>({ required: true })

const INPUT_TYPES: Partial<Record<ReportParameterType, "date" | "number" | "text">> = {
    Date: "date",
    Number: "number",
    Text: "text",
}

function fieldLabel(parameter: ReportParameterMetadata): string {
    return parameter.required ? `${parameter.label} *` : parameter.label
}

function rules(parameter: ReportParameterMetadata) {
    return parameter.required
        ? [(value: ReportParameterValue) => !isEmptyValue(value) || `${parameter.label} is required.`]
        : []
}

/** Inputs report "" when cleared and strings for numbers; the model holds null and numbers. */
function toValue(parameter: ReportParameterMetadata, value: string | number | null): ReportParameterValue {
    if (value === null || value === "") {
        return null
    }
    return parameter.type === "Number" ? Number(value) : value
}

/** The value for a text, date or number input, which never holds a list or a checkbox value. */
function inputValue(name: string): string | number | null {
    const value = model.value[name]
    return typeof value === "string" || typeof value === "number" ? value : null
}

function update(name: string, value: ReportParameterValue): void {
    model.value = { ...model.value, [name]: value }
}
</script>
