<template>
    <table class="report-table">
        <thead>
            <tr>
                <th class="col-group-name">Group</th>
                <th class="col-count">Count</th>
                <th
                    v-for="type in effortTypes"
                    :key="`hdr_${type}`"
                    class="col-effort"
                    :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                >
                    {{ type }}
                </th>
                <th class="col-effort">Teach Hrs</th>
            </tr>
        </thead>
        <tbody>
            <template
                v-for="group in groups"
                :key="group.groupName"
            >
                <!-- Sum row -->
                <tr class="totals-row bg-grey-1">
                    <td class="instructor-cell">{{ group.groupName }}</td>
                    <td>{{ group.instructorCount }}</td>
                    <td
                        v-for="type in effortTypes"
                        :key="`sum_${type}`"
                        class="total"
                        :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                    >
                        {{ getValue(group.sums, type) }}
                    </td>
                    <td class="total">{{ formatHours(group.teachingHoursSum) }}</td>
                </tr>
                <!-- Average row -->
                <tr>
                    <td class="text-right text-italic">Avg</td>
                    <td></td>
                    <td
                        v-for="type in effortTypes"
                        :key="`avg_${type}`"
                        :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                    >
                        {{ getAverage(group.averages, type) }}
                    </td>
                    <td>{{ formatDecimal(group.teachingHoursAverage) }}</td>
                </tr>
                <!-- Median row -->
                <tr class="grouping-bottom-row">
                    <td class="text-right text-italic">Med</td>
                    <td></td>
                    <td
                        v-for="type in effortTypes"
                        :key="`med_${type}`"
                        :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                    >
                        {{ getAverage(group.medians, type) }}
                    </td>
                    <td>{{ formatDecimal(group.teachingHoursMedian) }}</td>
                </tr>
            </template>
        </tbody>
    </table>
</template>

<script setup lang="ts">
import type { YearStatsGrouping } from "../types"

defineProps<{
    groups: YearStatsGrouping[]
    effortTypes: string[]
    getValue: (totals: Record<string, number>, type: string) => string
    getAverage: (averages: Record<string, number>, type: string) => string
}>()

function formatHours(value: number): string {
    if (value === 0) return ""
    return value.toString()
}

function formatDecimal(value: number): string {
    return value.toFixed(1)
}
</script>

<style scoped>
.col-group-name {
    min-width: 8rem;
}

.col-count {
    min-width: 3rem;
}

.grouping-bottom-row {
    border-bottom: 2px solid var(--ucdavis-black-40);
}
</style>
