<script setup lang="ts">
import type { ValidationRule } from "quasar"
import type { PercentageFormState, TypeOption } from "../composables/use-percentage-form"
import { requiredRule, percentRule } from "../validation"

type SelectOption<T> = { label: string; value: T }

defineProps<{
    groupedTypeOptions: TypeOption[]
    modifierOptions: SelectOption<string | null>[]
    unitOptions: SelectOption<number>[]
    monthOptions: SelectOption<number>[]
    yearOptions: SelectOption<number>[]
    endMonthRules: ValidationRule[]
    endYearRules: ValidationRule[]
}>()

const form = defineModel<PercentageFormState>({ required: true })
</script>

<template>
    <!-- Type Selection (grouped by class) -->
    <q-select
        v-model="form.percentAssignTypeId"
        :options="groupedTypeOptions"
        label="Type *"
        dense
        options-dense
        outlined
        emit-value
        map-options
        :rules="[requiredRule('Type')]"
        lazy-rules="ondemand"
    >
        <template #option="scope">
            <q-item-label
                v-if="scope.opt.isHeader"
                header
                class="text-weight-bold text-primary q-py-xs"
            >
                {{ scope.opt.label }}
            </q-item-label>
            <q-item
                v-else
                v-bind="scope.itemProps"
            >
                <q-item-section>
                    <q-item-label>{{ scope.opt.label }}</q-item-label>
                </q-item-section>
            </q-item>
        </template>
    </q-select>

    <!-- Modifier Selection -->
    <q-select
        v-model="form.modifier"
        :options="modifierOptions"
        label="Modifier"
        dense
        options-dense
        outlined
        emit-value
        map-options
        clearable
    />

    <!-- Unit Selection -->
    <q-select
        v-model="form.unitId"
        :options="unitOptions"
        label="Unit *"
        dense
        options-dense
        outlined
        emit-value
        map-options
        clearable
        :rules="[requiredRule('Unit')]"
        lazy-rules="ondemand"
    />

    <!-- Start Date (Month/Year) -->
    <div class="row q-col-gutter-sm">
        <div class="col">
            <q-select
                v-model="form.startMonth"
                :options="monthOptions"
                label="Start Month *"
                dense
                options-dense
                outlined
                emit-value
                map-options
                :rules="[requiredRule('Start month')]"
                lazy-rules="ondemand"
            />
        </div>
        <div class="col">
            <q-select
                v-model="form.startYear"
                :options="yearOptions"
                label="Start Year *"
                dense
                options-dense
                outlined
                emit-value
                map-options
                :rules="[requiredRule('Start year')]"
                lazy-rules="ondemand"
            />
        </div>
    </div>

    <!-- End Date (Month/Year) - Optional -->
    <div class="row q-col-gutter-sm">
        <div class="col">
            <q-select
                v-model="form.endMonth"
                :options="monthOptions"
                label="End Month"
                dense
                options-dense
                outlined
                emit-value
                map-options
                clearable
                :rules="endMonthRules"
                lazy-rules="ondemand"
            />
        </div>
        <div class="col">
            <q-select
                v-model="form.endYear"
                :options="yearOptions"
                label="End Year"
                dense
                options-dense
                outlined
                emit-value
                map-options
                clearable
                :rules="endYearRules"
                lazy-rules="ondemand"
            />
        </div>
    </div>

    <!-- Percent Input -->
    <q-input
        v-model.number="form.percentageValue"
        label="Percent *"
        type="number"
        dense
        outlined
        min="0"
        max="100"
        step="0.1"
        :rules="[percentRule]"
        lazy-rules="ondemand"
    />

    <!-- Comment Input -->
    <q-input
        v-model="form.comment"
        label="Comment"
        type="textarea"
        dense
        outlined
        maxlength="100"
        counter
        autogrow
    />

    <!-- Compensated Checkbox -->
    <q-checkbox
        v-model="form.compensated"
        label="Compensated"
        dense
    />
</template>
