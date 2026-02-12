<template>
    <div class="eval-cell">
        <!-- No entry or no course match -->
        <template v-if="!entry || entry.status === 'None'">
            <q-btn
                v-if="canEditAdHoc && entry"
                icon="add"
                color="grey"
                dense
                flat
                round
                size="sm"
                aria-label="Add evaluation data"
                @click="emitAdd"
            >
                <q-tooltip>Add evaluation data</q-tooltip>
            </q-btn>
            <span
                v-else
                class="text-grey-5"
                >&mdash;</span
            >
        </template>

        <!-- CERE or Ad-hoc data -->
        <template v-else>
            <span class="row no-wrap items-center justify-center q-gutter-xs">
                <span
                    v-if="entry.mean !== null"
                    class="text-body2"
                >
                    {{ entry.mean.toFixed(2) }}
                </span>
                <span
                    v-if="entry.standardDeviation !== null"
                    class="text-caption text-grey-7"
                >
                    SD={{ entry.standardDeviation.toFixed(2) }}
                </span>
                <span
                    v-if="entry.respondents !== null"
                    class="text-caption text-grey-7"
                >
                    N={{ entry.respondents }}
                </span>
                <span
                    v-if="showSource"
                    class="text-caption text-grey-6"
                >
                    {{ entry.status === "CERE" ? "CERE" : "Ad-Hoc" }}
                </span>
                <q-btn
                    v-if="entry.status === 'AdHoc' && entry.canEdit && canEditAdHoc"
                    icon="edit"
                    color="primary"
                    dense
                    flat
                    round
                    size="xs"
                    aria-label="Edit evaluation data"
                    @click="emitEdit"
                >
                    <q-tooltip>Edit</q-tooltip>
                </q-btn>
                <q-btn
                    v-if="entry.status === 'AdHoc' && entry.canEdit && canEditAdHoc"
                    icon="delete"
                    color="negative"
                    dense
                    flat
                    round
                    size="xs"
                    aria-label="Delete evaluation data"
                    @click="emitDelete"
                >
                    <q-tooltip>Delete</q-tooltip>
                </q-btn>
                <q-tooltip v-if="hasDistribution">
                    <div class="text-subtitle2 q-mb-xs">Rating Distribution</div>
                    <div>5 (High): {{ entry.count5 }}</div>
                    <div>4: {{ entry.count4 }}</div>
                    <div>3: {{ entry.count3 }}</div>
                    <div>2: {{ entry.count2 }}</div>
                    <div>1 (Low): {{ entry.count1 }}</div>
                </q-tooltip>
            </span>
        </template>
    </div>
</template>

<script setup lang="ts">
import { computed } from "vue"
import type { CourseEvalEntryDto } from "../types"

const props = defineProps<{
    entry: CourseEvalEntryDto | null
    canEditAdHoc: boolean
    mothraId: string
    instructorName: string
    courseName: string
    showSource?: boolean
}>()

const emit = defineEmits<{
    add: [entry: { courseId: number; crn: string; mothraId: string; instructorName: string; courseName: string }]
    edit: [
        entry: {
            courseId: number
            crn: string
            mothraId: string
            instructorName: string
            courseName: string
            data: CourseEvalEntryDto
        },
    ]
    delete: [entry: { courseId: number; quantId: number; instructorName: string; courseName: string }]
}>()

const hasDistribution = computed(
    () =>
        props.entry !== null &&
        (props.entry.count1 !== null || props.entry.count2 !== null || props.entry.count3 !== null),
)

function emitAdd() {
    if (!props.entry) return
    emit("add", {
        courseId: props.entry.courseId,
        crn: props.entry.crn,
        mothraId: props.mothraId,
        instructorName: props.instructorName,
        courseName: props.courseName,
    })
}

function emitEdit() {
    if (!props.entry) return
    emit("edit", {
        courseId: props.entry.courseId,
        crn: props.entry.crn,
        mothraId: props.mothraId,
        instructorName: props.instructorName,
        courseName: props.courseName,
        data: props.entry,
    })
}

function emitDelete() {
    if (!props.entry?.quantId) return
    emit("delete", {
        courseId: props.entry.courseId,
        quantId: props.entry.quantId,
        instructorName: props.instructorName,
        courseName: props.courseName,
    })
}
</script>
