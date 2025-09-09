<template>
    <q-card
        :class="cardClasses"
        clickable
        @click="handleClick"
    >
        <q-card-section class="q-pa-sm">
            <!-- Week header -->
            <div class="text-center text-weight-medium text-grey-8 q-mb-sm row items-center justify-center q-gutter-xs">
                <q-icon
                    v-if="requiresPrimary && !hasPrimary"
                    name="warning"
                    size="xs"
                    color="orange"
                    title="Primary evaluator required for this week"
                />
                <span>Week {{ week.weekNumber }} ({{ formatDate(week.dateStart) }})</span>
            </div>

            <!-- Assignments -->
            <div>
                <div
                    v-if="assignments.length > 0"
                    class="week-cell__assignment-list"
                >
                    <div
                        v-for="assignment in assignments"
                        :key="assignment.id"
                        class="week-cell__assignment-item"
                    >
                        <div class="week-cell__assignment-content">
                            <!-- Remove button -->
                            <q-btn
                                v-if="canEdit && !isPastYear && (!assignment.isPrimary || !requiresPrimaryForWeek)"
                                flat
                                round
                                dense
                                size="sm"
                                icon="close"
                                color="negative"
                                class="week-cell__remove-btn"
                                aria-label="Remove this clinician from the schedule"
                                @click.stop="$emit('remove-assignment', assignment.id, assignment.displayName)"
                            >
                                <q-tooltip :delay="600">Remove {{ assignment.displayName }} from schedule</q-tooltip>
                            </q-btn>
                            <q-btn
                                v-else-if="canEdit && !isPastYear && requiresPrimaryForWeek && assignment.isPrimary"
                                flat
                                round
                                dense
                                size="sm"
                                icon="close"
                                color="grey-4"
                                class="week-cell__remove-btn week-cell__remove-btn--disabled"
                                aria-label="Cannot remove primary clinician. Make another clinician primary first."
                                :disable="true"
                            >
                                <q-tooltip :delay="600"
                                    >Cannot remove primary clinician. Make another clinician primary first.</q-tooltip
                                >
                            </q-btn>

                            <!-- Clinician name -->
                            <span class="week-cell__clinician-name">{{ assignment.displayName }}</span>

                            <!-- Primary star -->
                            <q-btn
                                v-if="isPastYear && assignment.isPrimary"
                                flat
                                round
                                dense
                                icon="star"
                                size="sm"
                                color="amber"
                                class="week-cell__primary-btn"
                                aria-label="Primary evaluator"
                                :disable="true"
                            >
                                <q-tooltip :delay="600">Primary evaluator</q-tooltip>
                            </q-btn>
                            <q-btn
                                v-else-if="canEdit && !isPastYear && showPrimaryToggle"
                                flat
                                round
                                dense
                                :icon="assignment.isPrimary ? 'star' : 'star_outline'"
                                size="sm"
                                :color="assignment.isPrimary ? 'amber' : 'grey-5'"
                                class="week-cell__primary-btn"
                                :aria-label="
                                    assignment.isPrimary
                                        ? 'Primary evaluator. Click another clinician\'s star to transfer primary status.'
                                        : 'Click to make this clinician the primary evaluator.'
                                "
                                @click.stop="
                                    $emit(
                                        'toggle-primary',
                                        assignment.id,
                                        assignment.isPrimary ?? false,
                                        assignment.displayName,
                                    )
                                "
                            >
                                <q-tooltip :delay="600">{{
                                    assignment.isPrimary
                                        ? "Primary evaluator. Click another clinician's star to transfer primary status."
                                        : "Click to make this clinician the primary evaluator."
                                }}</q-tooltip>
                            </q-btn>
                        </div>
                    </div>
                </div>

                <!-- Empty state -->
                <div
                    v-else
                    class="week-cell__empty"
                >
                    <div class="week-cell__empty-text">
                        {{ emptyStateText }}
                    </div>
                </div>
            </div>
        </q-card-section>
    </q-card>
</template>

<script setup lang="ts">
import { computed } from "vue"
import { useDateFunctions } from "@/composables/DateFunctions"

const { formatDate } = useDateFunctions()

export interface WeekAssignment {
    id: number
    displayName: string
    isPrimary?: boolean
    [key: string]: unknown // Allow additional properties
}

interface Props {
    week: {
        weekId: number
        weekNumber: number
        dateStart: string
        dateEnd?: string
        requiresPrimaryEvaluator?: boolean
        [key: string]: unknown // Allow additional properties
    }
    assignments?: WeekAssignment[]
    canEdit?: boolean
    isPastYear?: boolean
    requiresPrimaryForWeek?: boolean
    showPrimaryToggle?: boolean
    additionalClasses?: string | string[] | Record<string, boolean>
}

const props = withDefaults(defineProps<Props>(), {
    assignments: () => [],
    canEdit: false,
    isPastYear: false,
    requiresPrimaryForWeek: false,
    showPrimaryToggle: true,
    additionalClasses: "",
})

const emit = defineEmits<{
    click: [week: Props["week"]]
    "remove-assignment": [assignmentId: number, displayName: string]
    "toggle-primary": [assignmentId: number, isPrimary: boolean, displayName: string]
}>()

// Computed properties
const requiresPrimary = computed(() => props.week.requiresPrimaryEvaluator ?? false)

const hasPrimary = computed(() => {
    return props.assignments.some((a) => a.isPrimary)
})

const emptyStateText = computed(() => {
    if (props.isPastYear) return "No assignments"
    if (props.canEdit) return "Click to add assignment"
    return "No assignments"
})

// Methods
function handleClick() {
    if (!props.isPastYear && props.canEdit) {
        emit("click", props.week)
    }
}

// Computed property for card classes
const cardClasses = computed(() => {
    const baseClasses = "col-xs-12 col-sm-6 col-md-4 col-lg-3 col-xl-2 cursor-pointer week-schedule-card"

    // Add the requires-primary class if needed
    const requiresPrimaryClass = requiresPrimary.value && !hasPrimary.value ? "requires-primary-card" : ""

    // Handle additional classes prop
    let additional = ""
    if (Array.isArray(props.additionalClasses)) {
        additional = props.additionalClasses.join(" ")
    } else if (typeof props.additionalClasses === "object") {
        additional = Object.entries(props.additionalClasses)
            .filter(([, value]) => value)
            .map(([key]) => key)
            .join(" ")
    } else {
        additional = props.additionalClasses || ""
    }

    return `${baseClasses} ${requiresPrimaryClass} ${additional}`.trim()
})
</script>

<style scoped>
.week-schedule-card {
    max-width: 280px;
    min-width: 200px;
}

.week-schedule-card .q-card {
    height: 100%;
}

/* Style for weeks requiring primary evaluator */
.requires-primary-card {
    border: 2px solid #f44 !important;
    background-color: #fff5f5;
}

/* Assignment item styling */
.week-cell__assignment-list {
    display: flex;
    flex-direction: column;
    gap: 4px;
}

.week-cell__assignment-item {
    display: flex;
    align-items: center;
}

.week-cell__assignment-content {
    display: flex;
    align-items: center;
    gap: 4px;
    width: 100%;
}

.week-cell__remove-btn {
    flex-shrink: 0;
}

.week-cell__remove-btn--disabled {
    opacity: 0.5;
}

.week-cell__clinician-name {
    flex: 1;
    font-size: 13px;
    color: #333;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.week-cell__primary-btn {
    flex-shrink: 0;
}

.week-cell__empty {
    display: flex;
    align-items: center;
    justify-content: center;
    min-height: 40px;
}

.week-cell__empty-text {
    font-size: 12px;
    color: #999;
    font-style: italic;
}

.cursor-pointer {
    cursor: pointer;
}
</style>
