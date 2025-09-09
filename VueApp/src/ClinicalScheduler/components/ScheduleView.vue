<template>
    <div class="schedule-view">
        <!-- Loading State -->
        <div
            v-if="isLoading"
            class="text-center q-my-lg"
        >
            <q-spinner-dots
                size="3rem"
                color="primary"
            />
            <div class="q-mt-md text-body1">Loading schedule...</div>
        </div>

        <!-- Error State -->
        <ScheduleBanner
            v-else-if="error"
            type="error"
            :error-message="error"
        />

        <!-- No Data State -->
        <ScheduleBanner
            v-else-if="!schedulesBySemester || schedulesBySemester.length === 0"
            type="no-entries"
            :custom-message="noDataMessage"
        />

        <!-- Schedule Display -->
        <div v-else>
            <!-- Schedule by semester -->
            <q-expansion-item
                v-for="semester in schedulesBySemester"
                :key="semester.semester || semester.name"
                :model-value="shouldExpandSemester(semester)"
                :label="semester.semester || semester.displayName"
                header-class="text-h6"
                class="q-mb-md"
            >
                <!-- Week grid -->
                <div class="row q-gutter-md q-mb-lg q-pa-md">
                    <WeekCell
                        v-for="week in semester.weeks"
                        :key="week.weekId"
                        :week="week"
                        :assignments="getAssignments(week)"
                        :can-edit="canEdit"
                        :is-past-year="isPastYear"
                        :requires-primary-for-week="requiresPrimaryEvaluator(week)"
                        :show-primary-toggle="showPrimaryToggle"
                        :additional-classes="getWeekAdditionalClasses(week)"
                        @click="onWeekClick"
                        @remove-assignment="handleRemoveAssignment"
                        @toggle-primary="handleTogglePrimary"
                    />
                </div>
            </q-expansion-item>
        </div>

        <!-- Legend -->
        <ScheduleLegend
            v-if="showLegend && schedulesBySemester && schedulesBySemester.length > 0"
            :show-warning="showWarningInLegend"
        />
    </div>
</template>

<script setup lang="ts">
import { computed } from "vue"
import WeekCell from "./WeekCell.vue"

export interface WeekItem {
    weekId: number
    weekNumber: number
    dateStart: string
    dateEnd?: string
    requiresPrimaryEvaluator?: boolean
    [key: string]: unknown
}
import ScheduleLegend from "./ScheduleLegend.vue"
import ScheduleBanner from "./ScheduleBanner.vue"

export interface ScheduleAssignment {
    id: number
    displayName: string
    isPrimary?: boolean
    [key: string]: unknown
}

export interface ScheduleSemester {
    semester?: string // For clinician view
    name?: string // For rotation view
    displayName?: string // For rotation view
    weeks: (WeekItem & { dateEnd: string })[]
}

export type ViewMode = "rotation" | "clinician"

// Helper callable type to avoid parameter-name linting within Props
interface WeekFn<T> {
    (arg: WeekItem): T
}

interface Props {
    // Core schedule data
    schedulesBySemester: ScheduleSemester[]
    viewMode: ViewMode
    isPastYear?: boolean
    isLoading?: boolean
    error?: string | null

    // Permissions
    canEdit?: boolean

    // Display options
    showLegend?: boolean
    showWarningInLegend?: boolean
    showWarningIcon?: boolean
    showPrimaryToggle?: boolean
    requiresPrimaryForWeek?: boolean

    // Custom messages
    noDataMessage?: string
    emptyStateMessage?: string
    readOnlyEmptyMessage?: string
    warningIconTitle?: string
    primaryEvaluatorTitle?: string
    makePrimaryTitle?: string
    primaryRemovalDisabledMessage?: string

    // Assignment data getter
    getAssignments?: WeekFn<ScheduleAssignment[]>

    // Helper functions
    requiresPrimaryEvaluator?: WeekFn<boolean>
    getWeekAdditionalClasses?: WeekFn<string | string[] | Record<string, boolean>>
}

const props = withDefaults(defineProps<Props>(), {
    isPastYear: false,
    isLoading: false,
    error: null,
    canEdit: false,
    showLegend: true,
    showWarningInLegend: false,
    showWarningIcon: false,
    showPrimaryToggle: true,
    requiresPrimaryForWeek: false,
    noDataMessage: "No schedule data available",
    emptyStateMessage: "Click to add assignment",
    readOnlyEmptyMessage: "No assignments",
    warningIconTitle: "Primary evaluator required for this week",
    primaryEvaluatorTitle: "Primary evaluator. To transfer primary status, click the star on another clinician.",
    makePrimaryTitle: "Click to make this the primary evaluator.",
    primaryRemovalDisabledMessage: "Cannot remove primary. Make another primary first.",
    getAssignments: undefined,
    requiresPrimaryEvaluator: undefined,
    getWeekAdditionalClasses: undefined,
})

const emit = defineEmits<{
    weekClick: [week: WeekItem]
    removeAssignment: [assignmentId: number, displayName: string]
    togglePrimary: [assignmentId: number, isPrimary: boolean, displayName: string]
}>()

// Check if a semester/term has ended
function isSemesterPast(semester: { weeks: { dateEnd: string }[] }): boolean {
    if (!semester.weeks || semester.weeks.length === 0) return false

    // Get the last week's end date
    const lastWeek = semester.weeks[semester.weeks.length - 1]
    if (!lastWeek || !lastWeek.dateEnd) return false

    const endDate = new Date(lastWeek.dateEnd)
    const today = new Date()

    return endDate < today
}

// Determine if a semester should be expanded
function shouldExpandSemester(semester: { weeks: { dateEnd: string }[] }): boolean {
    // When viewing historical years, always expand all semesters
    if (props.isPastYear) {
        return true
    }

    // When viewing current year, collapse past semesters
    return !isSemesterPast(semester)
}

// Event handlers
function onWeekClick(week: WeekItem) {
    if (!props.isPastYear && props.canEdit) {
        emit("weekClick", week)
    }
}

function handleRemoveAssignment(assignmentId: number, displayName: string) {
    if (!props.isPastYear && props.canEdit) {
        emit("removeAssignment", assignmentId, displayName)
    }
}

function handleTogglePrimary(assignmentId: number, isPrimary: boolean | undefined, displayName: string) {
    if (!props.isPastYear && props.canEdit) {
        emit("togglePrimary", assignmentId, isPrimary ?? false, displayName)
    }
}

// Computed property to get assignments for a week
const getAssignments = computed(() => {
    return (week: WeekItem) => {
        return props.getAssignments?.(week) ?? []
    }
})

// Computed property for primary evaluator check
const requiresPrimaryEvaluator = computed(() => {
    return (week: WeekItem) => {
        return props.requiresPrimaryEvaluator?.(week) ?? false
    }
})

// Computed property for week additional classes
const getWeekAdditionalClasses = computed(() => {
    return (week: WeekItem) => {
        return props.getWeekAdditionalClasses?.(week) ?? ""
    }
})
</script>

<style scoped>
.schedule-view {
    width: 100%;
}

.assignment-item {
    padding: 2px 0;
}

.assignment-item .text-body2 {
    overflow-wrap: break-word;
    hyphens: auto;
    line-height: 1.2;
}

.cursor-pointer {
    cursor: pointer;
}
</style>
