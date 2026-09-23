<script setup lang="ts">
import CareerRecordLink from "./CareerRecordLink.vue"
import StudentEmail from "@/Students/components/StudentEmail.vue"

/**
 * One student as a card, for the narrow-screen grid view of the career selection roster and
 * report. The slot holds the field lines, which differ between the two.
 * Follows the table's column visibility choices.
 */
const props = withDefaults(
    defineProps<{
        student: {
            personId: number
            fullName: string
            hasDetailRoute: boolean
            classLevel: string
            email?: string | null
            lastUpdated?: string | null
        }
        canEdit: boolean
        visibleColumns?: string[] // Column names the table is showing. Omitted means every column.
    }>(),
    { visibleColumns: undefined },
)

function shows(column: string): boolean {
    return props.visibleColumns === undefined || props.visibleColumns.includes(column)
}
</script>

<template>
    <div class="q-pa-xs col-12">
        <q-card
            flat
            bordered
        >
            <q-card-section>
                <div
                    v-if="shows('fullName') || shows('classLevel')"
                    class="row items-center q-mb-xs"
                >
                    <CareerRecordLink
                        v-if="shows('fullName')"
                        :student="student"
                        :can-edit="canEdit"
                        emphasized
                    />
                    <q-space />
                    <span
                        v-if="shows('classLevel')"
                        class="text-caption text-grey"
                        >{{ student.classLevel }}</span
                    >
                </div>
                <div
                    v-if="shows('email') && student.email"
                    class="text-caption q-mb-xs"
                >
                    <StudentEmail :email="student.email" />
                </div>
                <slot />
                <div
                    v-if="shows('lastUpdated') && student.lastUpdated"
                    class="text-caption text-grey"
                >
                    Updated {{ new Date(student.lastUpdated).toLocaleDateString() }}
                </div>
            </q-card-section>
        </q-card>
    </div>
</template>
