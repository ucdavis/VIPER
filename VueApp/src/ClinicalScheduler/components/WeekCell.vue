<template>
    <q-card
        :class="cardClasses"
        clickable
        @click="handleClick"
        @touchstart="handleTouchStart"
        @touchend="handleTouchEnd"
        @touchmove="handleTouchMove"
    >
        <q-card-section class="q-pa-sm">
            <!-- Week header -->
            <div class="text-center text-weight-medium text-grey-8 q-mb-sm row items-center justify-center q-gutter-xs">
                <q-spinner-dots
                    v-if="isLoading"
                    size="1rem"
                    color="primary"
                />
                <q-icon
                    v-else-if="requiresPrimary && !hasPrimary"
                    name="warning"
                    size="xs"
                    style="color: var(--ucdavis-poppy)"
                    title="Primary evaluator required for this week"
                />
                <span>Week {{ week.weekNumber }} ({{ formatDate(week.dateStart) }})</span>
            </div>

            <!-- Assignments -->
            <div>
                <transition-group
                    name="assignment-list"
                    tag="div"
                    class="week-cell__assignment-list"
                    leave-active-class="animated fadeOutLeft"
                    move-class="assignment-list-move"
                >
                    <div
                        v-for="assignment in assignments"
                        :key="assignment.id"
                        :class="[
                            'week-cell__assignment-item',
                            { 'week-cell__assignment-item--newly-added': isNewlyAdded(assignment.id) },
                        ]"
                        :style="isNewlyAdded(assignment.id) ? { '--highlight-duration': animationDuration } : {}"
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
                                @click.stop="
                                    $emit(
                                        'remove-assignment',
                                        assignment.id,
                                        assignment.displayName,
                                        assignment.isPrimary,
                                    )
                                "
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
                    <!-- Empty state as part of transition group -->
                    <div
                        v-if="assignments.length === 0"
                        key="empty-state"
                        class="week-cell__empty"
                    >
                        <div class="week-cell__empty-text">
                            {{ emptyStateText }}
                        </div>
                    </div>
                </transition-group>
            </div>
        </q-card-section>
    </q-card>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue"
import { useTimeoutFn } from "@vueuse/core"
import { useDateFunctions } from "@/composables/DateFunctions"
import { ANIMATIONS } from "../constants/app-constants"

const { formatDate } = useDateFunctions()

// Animation constants - single source of truth from app-constants.ts
const animationDuration = `${ANIMATIONS.HIGHLIGHT_DURATION_MS}ms`

// Track newly added assignments for animation
const newlyAddedAssignments = ref<Set<number>>(new Set())

// Function to check if assignment is newly added
function isNewlyAdded(assignmentId: number): boolean {
    return newlyAddedAssignments.value.has(assignmentId)
}

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
    isLoading?: boolean
    selectable?: boolean
    selected?: boolean
}

const props = withDefaults(defineProps<Props>(), {
    assignments: () => [],
    canEdit: false,
    isPastYear: false,
    requiresPrimaryForWeek: false,
    showPrimaryToggle: true,
    additionalClasses: "",
    isLoading: false,
    selectable: false,
    selected: false,
})

const emit = defineEmits<{
    click: [week: Props["week"], event?: MouseEvent]
    "remove-assignment": [assignmentId: number, displayName: string, isPrimary?: boolean]
    "toggle-primary": [assignmentId: number, isPrimary: boolean, displayName: string]
    "shift-click": [week: Props["week"]]
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

// Watch for new assignments and trigger highlight animation
watch(
    () => props.assignments,
    (newAssignments, oldAssignments) => {
        if (!oldAssignments || !newAssignments) return

        // Find newly added assignments
        const oldIds = new Set(oldAssignments.map((a) => a.id))
        const newlyAdded = newAssignments.filter((a) => !oldIds.has(a.id))

        newlyAdded.forEach((assignment) => {
            // Create new Set to trigger reactivity
            newlyAddedAssignments.value = new Set([...newlyAddedAssignments.value, assignment.id])

            // Remove highlight after animation duration
            setTimeout(() => {
                // Create new Set without the item to trigger reactivity
                const updated = new Set(newlyAddedAssignments.value)
                updated.delete(assignment.id)
                newlyAddedAssignments.value = updated
            }, ANIMATIONS.HIGHLIGHT_DURATION_MS)
        })
    },
    { deep: true },
)

// Long press timer for mobile (auto-cleanup on unmount via useTimeoutFn)
const {
    start: startLongPress,
    stop: stopLongPress,
    isPending: isLongPressing,
} = useTimeoutFn(() => emit("click", props.week), 500, { immediate: false })

// Methods
function handleClick(event?: MouseEvent) {
    // Don't handle click during selection mode if just selecting
    if (props.selectable && !props.canEdit) {
        if (event?.shiftKey) {
            emit("shift-click", props.week)
        } else {
            emit("click", props.week, event)
        }
        return
    }

    // Normal behavior for edit mode
    if (!props.isLoading && !props.isPastYear) {
        if (props.canEdit) {
            emit("click", props.week, event)
        } else if (props.selectable) {
            // Selection mode when not editing
            if (event?.shiftKey) {
                emit("shift-click", props.week)
            } else {
                emit("click", props.week, event)
            }
        }
    }
}

// Touch event handlers for mobile long-press
function handleTouchStart() {
    if (props.selectable && !props.isPastYear) {
        startLongPress()
    }
}

function handleTouchEnd() {
    if (isLongPressing.value) {
        stopLongPress()
        // Short tap - handle as normal click
        handleClick()
    }
}

function handleTouchMove() {
    // Cancel long press if user moves finger
    stopLongPress()
}

// Helper function to normalize additional classes
function normalizeAdditionalClasses(classes: Props["additionalClasses"]): string {
    if (!classes) return ""
    if (typeof classes === "string") return classes
    if (Array.isArray(classes)) return classes.join(" ")

    // Handle object format
    return Object.entries(classes)
        .filter(([, value]) => value)
        .map(([key]) => key)
        .join(" ")
}

// Computed property for card classes - simplified
const cardClasses = computed(() => {
    const classes = [
        // Base responsive classes
        "col-xs-12",
        "col-sm-6",
        "col-md-4",
        "col-lg-3",
        "col-xl-2",
        "cursor-pointer",
        "week-schedule-card",

        // Conditional classes
        requiresPrimary.value && !hasPrimary.value && "requires-primary-card",
        props.selectable && "week-selectable",
        props.selected && "week-selected",

        // Additional classes from props
        normalizeAdditionalClasses(props.additionalClasses),
    ]

    // Filter out falsy values and join
    return classes.filter(Boolean).join(" ")
})
</script>

<style scoped>
@import url("@/styles/colors.css");

.week-schedule-card {
    max-width: 280px;
    min-width: 200px;
}

.week-schedule-card .q-card {
    height: 100%;
}

/* Style for weeks requiring primary evaluator - using UC Davis gold for warning */
.requires-primary-card {
    border: 2px solid var(--ucdavis-gold-70) !important;
    background-color: var(--ucdavis-gold-10);
}

/* Assignment item styling */
.week-cell__assignment-list {
    display: flex;
    flex-direction: column;
    gap: 4px;
}

@media screen and (prefers-reduced-motion: reduce) {
    .week-cell__assignment-item {
        display: flex;
        align-items: center;
        transition: none;
    }
}

.week-cell__assignment-item {
    display: flex;
    align-items: center;
    transition: background-color 0.3s ease-out;
}

/* Newly added item highlighting - using UC Davis gold for confirmation */
@media screen and (prefers-reduced-motion: reduce) {
    .week-cell__assignment-item--newly-added {
        background-color: var(--ucdavis-gold-20);
        border-radius: 4px;
        padding: 2px 4px;
        animation: none;
    }
}

.week-cell__assignment-item--newly-added {
    background-color: var(--ucdavis-gold-20);
    border-radius: 4px;
    padding: 2px 4px;
    animation: fadeToBackground var(--highlight-duration) ease-out forwards; /* Duration from ANIMATIONS.HIGHLIGHT_DURATION_MS */
}

@keyframes fadeToBackground {
    0% {
        background-color: var(--ucdavis-gold-30);
    }

    100% {
        background-color: transparent;
    }
}

/* Move transition for reordering */
@media screen and (prefers-reduced-motion: reduce) {
    .assignment-list-move {
        transition: none;
    }
}

.assignment-list-move {
    transition: transform 0.3s ease;
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
    color: var(--ucdavis-black-80);
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
    color: var(--ucdavis-black-40);
    font-style: italic;
}

.cursor-pointer {
    cursor: pointer;
}

/* Selection states - using UC Davis blue */
@media screen and (prefers-reduced-motion: reduce) {
    .week-selectable {
        transition: none;
        position: relative;
    }
}

.week-selectable {
    transition: all 0.2s ease-in-out;
    position: relative;
}

@media screen and (prefers-reduced-motion: reduce) {
    .week-selectable::before {
        content: "";
        position: absolute;
        inset: 0;
        border: 2px solid transparent;
        border-radius: 4px;
        pointer-events: none;
        transition: none;
    }
}

.week-selectable::before {
    content: "";
    position: absolute;
    inset: 0;
    border: 2px solid transparent;
    border-radius: 4px;
    pointer-events: none;
    transition: all 0.2s ease-in-out;
}

.week-selectable:hover::before,
.week-selectable:focus::before {
    border-color: var(--ucdavis-blue-60);
    opacity: 0.3;
}

.week-selected {
    background-color: var(--ucdavis-blue-10) !important;
    border: 2px solid var(--ucdavis-blue-60) !important;
}

.week-selected::before {
    border-color: var(--ucdavis-blue-60) !important;
    opacity: 1 !important;
}

/* Override requires-primary style when selected - combination of gold and blue */
.week-selected.requires-primary-card {
    background-color: var(--ucdavis-gold-10) !important;
    border: 2px solid var(--ucdavis-gold-80) !important;
}
</style>
