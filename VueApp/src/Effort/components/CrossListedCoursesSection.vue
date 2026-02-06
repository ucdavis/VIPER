<template>
    <div
        v-if="courses.length > 0"
        class="q-mb-lg"
    >
        <h4 class="q-mt-none q-mb-sm">Cross-Listed / Sectioned Courses</h4>

        <!-- Mobile card view (only shown when showParentCourse is true) -->
        <div
            v-if="showParentCourse"
            class="lt-sm"
        >
            <q-card
                v-for="child in courses"
                :key="childKey(child)"
                flat
                bordered
                class="q-mb-sm"
                :class="child.relationshipType === 'CrossList' ? 'crosslist-card' : 'section-card'"
            >
                <q-card-section class="q-py-sm">
                    <div class="row items-center justify-between q-mb-xs">
                        <span class="text-weight-bold">
                            {{ child.subjCode }} {{ child.crseNumb.trim() }}-{{ child.seqNumb }}
                        </span>
                        <q-badge
                            :color="child.relationshipType === 'CrossList' ? 'positive' : 'info'"
                            :label="child.relationshipType === 'CrossList' ? 'Cross List' : 'Section'"
                        />
                    </div>
                    <div class="text-caption text-grey-7">
                        Parent: {{ child.parentCourseCode }} &bull; {{ child.units }} units &bull; Enroll:
                        {{ child.enrollment }}
                    </div>
                </q-card-section>
            </q-card>
        </div>

        <!-- Table view -->
        <q-table
            :rows="courses"
            :columns="tableColumns"
            row-key="id"
            dense
            flat
            bordered
            hide-pagination
            :rows-per-page-options="[0]"
            :class="showParentCourse ? 'gt-xs' : ''"
        >
            <template #body-cell-relationshipType="slotProps">
                <q-td :props="slotProps">
                    <q-badge
                        :color="slotProps.row.relationshipType === 'CrossList' ? 'positive' : 'info'"
                        :label="slotProps.row.relationshipType === 'CrossList' ? 'Cross List' : 'Section'"
                    />
                </q-td>
            </template>
        </q-table>
    </div>
</template>

<script setup lang="ts">
import { computed } from "vue"
import type { QTableColumn } from "quasar"

type CrossListedCourseRow = {
    id: number
    parentCourseId?: number
    parentCourseCode?: string
    subjCode: string
    crseNumb: string
    seqNumb: string
    units: number
    enrollment: number
    relationshipType: string
}

const props = defineProps<{
    courses: CrossListedCourseRow[]
    showParentCourse: boolean
}>()

function childKey(child: CrossListedCourseRow): string {
    return child.parentCourseId ? `${child.parentCourseId}-${child.id}` : `${child.id}`
}

const baseColumns: QTableColumn[] = [
    {
        name: "course",
        label: "Course",
        field: (row: CrossListedCourseRow) => `${row.subjCode} ${row.crseNumb.trim()}-${row.seqNumb}`,
        align: "left",
    },
    {
        name: "units",
        label: "Units",
        field: "units",
        align: "left",
    },
    {
        name: "enrollment",
        label: "Enroll",
        field: "enrollment",
        align: "left",
    },
]

const parentColumn: QTableColumn = {
    name: "parentCourse",
    label: "Parent Course",
    field: "parentCourseCode",
    align: "left",
}

const typeColumn: QTableColumn = {
    name: "relationshipType",
    label: "Type",
    field: "relationshipType",
    align: "center",
}

const tableColumns = computed<QTableColumn[]>(() => {
    if (props.showParentCourse) {
        return [...baseColumns, parentColumn, typeColumn]
    }
    return [...baseColumns, typeColumn]
})
</script>

<style scoped>
.crosslist-card {
    border-left: 3px solid #21ba45;
}

.section-card {
    border-left: 3px solid #2196f3;
}
</style>
