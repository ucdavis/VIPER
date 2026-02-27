<template>
    <div>
        <template v-if="subReport.instructors.length === 0">
            <div class="text-grey-6 text-center q-pa-lg">No data found for this category.</div>
        </template>

        <template v-else>
            <!-- Detail table: per-instructor rows -->
            <div class="q-mb-lg">
                <table class="report-table">
                    <thead>
                        <tr>
                            <th class="col-instructor">Instructor</th>
                            <th class="col-dept">Department</th>
                            <th class="col-job-group">Job Group</th>
                            <th
                                v-for="type in effortTypes"
                                :key="type"
                                class="col-effort"
                                :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                            >
                                {{ type }}
                            </th>
                            <th class="col-effort">Teach Hrs</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr
                            v-for="instructor in subReport.instructors"
                            :key="instructor.mothraId"
                        >
                            <td class="instructor-cell">{{ instructor.instructor }}</td>
                            <td>{{ instructor.department }}</td>
                            <td>{{ instructor.jobGroup }}</td>
                            <td
                                v-for="type in effortTypes"
                                :key="type"
                                :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                            >
                                {{ getValue(instructor.efforts, type) }}
                            </td>
                            <td>{{ formatHours(instructor.teachingHours) }}</td>
                        </tr>

                        <!-- Summary section -->
                        <tr class="header-repeat">
                            <th></th>
                            <th></th>
                            <th></th>
                            <th
                                v-for="type in effortTypes"
                                :key="type"
                                class="col-effort"
                                :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                            >
                                {{ type }}
                            </th>
                            <th class="col-effort">Teach Hrs</th>
                        </tr>
                        <tr class="totals-row bg-grey-1">
                            <th
                                class="subt"
                                colspan="3"
                            >
                                Sum ({{ subReport.instructorCount }} instructors):
                            </th>
                            <td
                                v-for="type in effortTypes"
                                :key="type"
                                class="total"
                                :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                            >
                                {{ getValue(subReport.sums, type) }}
                            </td>
                            <td class="total">{{ formatHours(subReport.teachingHoursSum) }}</td>
                        </tr>
                        <tr class="totals-row">
                            <th
                                class="subt"
                                colspan="3"
                            >
                                Average:
                            </th>
                            <td
                                v-for="type in effortTypes"
                                :key="type"
                                :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                            >
                                {{ getAverage(subReport.averages, type) }}
                            </td>
                            <td>{{ formatDecimal(subReport.teachingHoursAverage) }}</td>
                        </tr>
                        <tr class="totals-row">
                            <th
                                class="subt"
                                colspan="3"
                            >
                                Median:
                            </th>
                            <td
                                v-for="type in effortTypes"
                                :key="type"
                                :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                            >
                                {{ getAverage(subReport.medians, type) }}
                            </td>
                            <td>{{ formatDecimal(subReport.teachingHoursMedian) }}</td>
                        </tr>
                    </tbody>
                </table>
            </div>

            <!-- Grouping tables (SVM and DVM only) -->
            <template v-if="showGroupings">
                <q-expansion-item
                    v-if="subReport.byDepartment.length > 0"
                    default-opened
                    dense
                    header-class="text-weight-bold"
                    label="By Department"
                    class="q-mb-md"
                >
                    <GroupingTable
                        :groups="subReport.byDepartment"
                        :effort-types="effortTypes"
                        :get-value="getValue"
                        :get-average="getAverage"
                    />
                </q-expansion-item>

                <q-expansion-item
                    v-if="subReport.byDiscipline.length > 0"
                    default-opened
                    dense
                    header-class="text-weight-bold"
                    label="By Discipline"
                    class="q-mb-md"
                >
                    <GroupingTable
                        :groups="subReport.byDiscipline"
                        :effort-types="effortTypes"
                        :get-value="getValue"
                        :get-average="getAverage"
                    />
                </q-expansion-item>

                <q-expansion-item
                    v-if="subReport.byTitle.length > 0"
                    default-opened
                    dense
                    header-class="text-weight-bold"
                    label="By Title"
                    class="q-mb-md"
                >
                    <GroupingTable
                        :groups="subReport.byTitle"
                        :effort-types="effortTypes"
                        :get-value="getValue"
                        :get-average="getAverage"
                    />
                </q-expansion-item>
            </template>
        </template>
    </div>
</template>

<script setup lang="ts">
import type { YearStatsSubReport } from "../types"
import GroupingTable from "./YearStatsGroupingTable.vue"

defineProps<{
    subReport: YearStatsSubReport
    effortTypes: string[]
    getValue: (totals: Record<string, number>, type: string) => string
    getAverage: (averages: Record<string, number>, type: string) => string
    showGroupings: boolean
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
.col-dept {
    min-width: 4rem;
}

.col-job-group {
    min-width: 5rem;
}
</style>
