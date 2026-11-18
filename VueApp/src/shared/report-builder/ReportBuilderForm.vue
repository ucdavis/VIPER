<template>
    <q-form
        class="compact-form row q-col-gutter-md items-start q-mb-md"
        @submit="emit('submit')"
    >
        <div
            v-for="parameter in parameters"
            :key="parameter.name"
            class="col-12 col-sm-6 col-md-4"
        >
            <ReportChoiceField
                v-if="isChoice(parameter)"
                :model-value="model[parameter.name] ?? null"
                :label="fieldLabel(parameter)"
                :choices="choicesFor(parameter)"
                :multiple="parameter.type === 'MultiChoice'"
                :required="parameter.required"
                :rules="rules(parameter)"
                @update:model-value="update(parameter.name, $event)"
            />
            <q-input
                v-else
                :model-value="inputValue(parameter.name)"
                :label="fieldLabel(parameter)"
                :type="INPUT_TYPES[parameter.type]"
                :rules="rules(parameter)"
                :clearable="!parameter.required"
                stack-label
                outlined
                dense
                @update:model-value="update(parameter.name, toText($event))"
            />
        </div>
    </q-form>
</template>

<script setup lang="ts">
import ReportChoiceField from "./ReportChoiceField.vue"
import { isEmptyValue } from "./report-builder-query"
import type {
    ReportChoice,
    ReportParameterType,
    ReportParameterValue,
    ReportParameterValues,
    ReportViewerParameter,
} from "./report-builder-types"

/**
 * One field per report parameter, chosen by its type. The report runs by itself when the values
 * change; pressing Enter in a field runs it straight away. Required fields are checked here for
 * quick feedback; the server checks them again, along with every other rule.
 */
defineProps<{
    parameters: ReportViewerParameter[]
}>()

const emit = defineEmits<{ (e: "submit"): void }>()

const model = defineModel<ReportParameterValues>({ required: true })

const INPUT_TYPES: Record<ReportParameterType, "date" | "number" | "text"> = {
    Text: "text",
    Number: "number",
    Date: "date",
    Boolean: "text",
    Choice: "text",
    MultiChoice: "text",
}

// A yes/no parameter is a two-way choice, so leaving it empty can mean "either".
const YES_NO: ReportChoice[] = [
    { value: "true", label: "Yes" },
    { value: "false", label: "No" },
]

function isChoice(parameter: ReportViewerParameter): boolean {
    return parameter.type === "Choice" || parameter.type === "MultiChoice" || parameter.type === "Boolean"
}

function choicesFor(parameter: ReportViewerParameter): ReportChoice[] {
    return parameter.type === "Boolean" ? YES_NO : (parameter.choices ?? [])
}

function fieldLabel(parameter: ReportViewerParameter): string {
    return parameter.required ? `${parameter.label} *` : parameter.label
}

function rules(parameter: ReportViewerParameter) {
    return parameter.required
        ? [(value: ReportParameterValue) => !isEmptyValue(value) || `${parameter.label} is required.`]
        : []
}

/** Inputs report "" or null when cleared and numbers for number fields; the model holds strings. */
function toText(value: string | number | null): string | null {
    return value === null || value === "" ? null : String(value)
}

/** The value for a text, date or number input, which never holds a list. */
function inputValue(name: string): string | null {
    const value = model.value[name]
    return typeof value === "string" ? value : null
}

function update(name: string, value: ReportParameterValue): void {
    model.value = { ...model.value, [name]: value }
}
</script>
