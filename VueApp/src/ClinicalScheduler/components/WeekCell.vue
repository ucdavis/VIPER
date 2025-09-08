<template>
    <div
        class="week-cell"
        :class="{
            'week-cell--clickable': isClickable,
            'week-cell--has-assignments': hasAssignments,
            'week-cell--requires-primary': requiresPrimary && !hasPrimary,
            'week-cell--past': isPast,
        }"
        @click="handleClick"
        @keydown.enter="handleClick"
        @keydown.space.prevent="handleClick"
        :tabindex="isClickable ? 0 : -1"
        :role="isClickable ? 'button' : undefined"
        :aria-label="`Week ${week.weekNumber} from ${formatDateRange(week.dateStart, week.dateEnd)}`"
    >
        <!-- Week header -->
        <div class="week-cell__header">
            <div class="week-cell__number">Week {{ week.weekNumber }}</div>
            <q-icon
                v-if="requiresPrimary && !hasPrimary"
                name="warning"
                size="xs"
                color="orange"
                class="week-cell__warning-icon"
                title="Primary evaluator required for this week"
            />
        </div>

        <!-- Week dates -->
        <div class="week-cell__dates">
            {{ formatDateRange(week.dateStart, week.dateEnd) }}
        </div>

        <!-- Assignments -->
        <div class="week-cell__assignments">
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
                        <q-icon
                            v-if="canEdit && !isPast && !assignment.isPrimary"
                            name="close"
                            size="xs"
                            color="negative"
                            class="week-cell__remove-btn"
                            title="Remove this clinician from the schedule"
                            @click.stop="$emit('remove-assignment', assignment)"
                        />
                        <q-icon
                            v-else-if="canEdit && !isPast && assignment.isPrimary"
                            name="close"
                            size="xs"
                            color="grey-4"
                            class="week-cell__remove-btn week-cell__remove-btn--disabled"
                            title="Cannot remove primary clinician. Make another clinician primary first."
                        />

                        <!-- Clinician name -->
                        <span class="week-cell__clinician-name">{{ assignment.clinicianName }}</span>

                        <!-- Primary star -->
                        <q-icon
                            v-if="isPast && assignment.isPrimary"
                            name="star"
                            size="xs"
                            color="amber"
                            title="Primary evaluator"
                        />
                        <q-icon
                            v-else-if="canEdit && !isPast"
                            :name="assignment.isPrimary ? 'star' : 'star_outline'"
                            size="xs"
                            :color="assignment.isPrimary ? 'amber' : 'grey-5'"
                            class="week-cell__primary-btn"
                            :title="
                                assignment.isPrimary
                                    ? 'Primary evaluator. Click another clinician\'s star to transfer primary status.'
                                    : 'Click to make this clinician the primary evaluator.'
                            "
                            @click.stop="$emit('toggle-primary', assignment)"
                        />
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
    </div>
</template>

<script setup lang="ts">
import { computed } from "vue"

export interface WeekAssignment {
    id: number
    clinicianName: string
    isPrimary: boolean
    mothraId: string
}

interface Props {
    week: {
        weekId: number
        weekNumber: number
        dateStart: string
        dateEnd: string
        requiresPrimaryEvaluator: boolean
    }
    assignments?: WeekAssignment[]
    canEdit?: boolean
    isPast?: boolean
    isClickable?: boolean
}

const props = withDefaults(defineProps<Props>(), {
    assignments: () => [],
    canEdit: false,
    isPast: false,
    isClickable: true,
})

const emit = defineEmits<{
    click: [week: Props["week"]]
    "remove-assignment": [assignment: WeekAssignment]
    "toggle-primary": [assignment: WeekAssignment]
}>()

// Computed properties
const hasAssignments = computed(() => props.assignments.length > 0)

const requiresPrimary = computed(() => props.week.requiresPrimaryEvaluator)

const hasPrimary = computed(() => {
    return props.assignments.some((a) => a.isPrimary)
})

const emptyStateText = computed(() => {
    if (props.isPast) return "No assignments"
    if (props.canEdit && props.isClickable) return "Click to add clinician"
    return "No assignments"
})

// Methods
function handleClick() {
    if (props.isClickable && !props.isPast) {
        emit("click", props.week)
    }
}

function formatDateRange(start: string, end: string): string {
    const startDate = new Date(start)
    const endDate = new Date(end)
    const options: Intl.DateTimeFormatOptions = { month: "short", day: "numeric" }

    return `${startDate.toLocaleDateString("en-US", options)} - ${endDate.toLocaleDateString("en-US", options)}`
}
</script>

<style scoped>
@media screen and (prefers-reduced-motion: reduce) {
    .week-cell {
        background: white;
        border: 1px solid #e0e0e0;
        border-radius: 4px;
        padding: 12px;
        min-width: 200px;
        transition: none;
    }
}

.week-cell {
    background: white;
    border: 1px solid #e0e0e0;
    border-radius: 4px;
    padding: 12px;
    min-width: 200px;
    transition: all 0.2s ease;
}

.week-cell--clickable {
    cursor: pointer;
}

.week-cell--clickable:hover,
.week-cell--clickable:focus {
    box-shadow: 0 2px 4px rgb(0 0 0 / 10%);
    border-color: #1976d2;
}

.week-cell--requires-primary {
    border: 2px solid #f44 !important;
    background-color: #fff5f5;
}

.week-cell--past {
    opacity: 0.7;
    cursor: default;
}

.week-cell__header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 4px;
}

.week-cell__number {
    font-weight: 600;
    font-size: 14px;
    color: #333;
}

.week-cell__warning-icon {
    flex-shrink: 0;
}

.week-cell__dates {
    font-size: 12px;
    color: #666;
    margin-bottom: 8px;
}

.week-cell__assignments {
    min-height: 60px;
}

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
    cursor: pointer;
    flex-shrink: 0;
}

.week-cell__remove-btn--disabled {
    cursor: not-allowed;
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
    cursor: pointer;
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
</style>
