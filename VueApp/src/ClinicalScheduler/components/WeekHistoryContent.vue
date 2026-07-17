<template>
    <section
        :class="['week-history', { 'week-history--fluid': fluid }]"
        :aria-labelledby="titleId"
        :aria-busy="isLoading"
    >
        <header class="week-history__header">
            <div class="week-history__bar">
                <div class="week-history__heading">
                    <div
                        :id="titleId"
                        class="week-history__title"
                    >
                        Audit Trail
                    </div>
                    <div class="week-history__subtitle">{{ contextLabel }}</div>
                </div>
                <q-btn
                    v-close-popup
                    flat
                    round
                    dense
                    size="sm"
                    icon="close"
                    color="grey-7"
                    class="week-history__close"
                    aria-label="Close audit trail"
                >
                    <!-- Hover devices only; see the trigger tooltip in WeekHistoryButton -->
                    <q-tooltip
                        v-if="supportsHover"
                        :delay="500"
                        >Close</q-tooltip
                    >
                </q-btn>
            </div>

            <!-- Page between weeks without closing the dialog. The week label is a
                 live region so screen readers announce each move. -->
            <nav
                class="week-history__nav"
                aria-label="Week navigation"
            >
                <q-btn
                    flat
                    round
                    dense
                    size="sm"
                    icon="chevron_left"
                    color="grey-8"
                    class="week-history__nav-btn"
                    :disable="!canPrev"
                    aria-label="Previous week"
                    @click="$emit('prev')"
                >
                    <q-tooltip
                        v-if="showPrevTooltip"
                        :delay="500"
                        >Previous week</q-tooltip
                    >
                </q-btn>
                <div
                    class="week-history__nav-label"
                    aria-live="polite"
                >
                    Week {{ weekNumber }} ({{ formatDate(weekDateStart) }})
                </div>
                <q-btn
                    flat
                    round
                    dense
                    size="sm"
                    icon="chevron_right"
                    color="grey-8"
                    class="week-history__nav-btn"
                    :disable="!canNext"
                    aria-label="Next week"
                    @click="$emit('next')"
                >
                    <q-tooltip
                        v-if="showNextTooltip"
                        :delay="500"
                        >Next week</q-tooltip
                    >
                </q-btn>
            </nav>

            <!-- Determinate + absolutely placed: even a fast load reads as a full sweep -->
            <Transition name="week-history-progress">
                <q-linear-progress
                    v-if="showProgress"
                    :value="progress"
                    size="0.125rem"
                    color="primary"
                    track-color="transparent"
                    class="week-history__progress"
                    aria-hidden="true"
                />
            </Transition>
        </header>

        <!-- Height tracks the measured content so the card animates between weeks, not snaps -->
        <div
            ref="viewport"
            class="week-history__viewport"
        >
            <div
                ref="measure"
                class="week-history__measure"
            >
                <WeekHistoryBody
                    :view-mode="viewMode"
                    :entries="entries"
                    :is-loading="isLoading"
                    :error="error"
                    @retry="$emit('retry')"
                />
            </div>
        </div>
    </section>
</template>

<script setup lang="ts">
import { computed, ref, useTemplateRef, watch } from "vue"
import { useIntervalFn, useMediaQuery, useResizeObserver, useTimeoutFn } from "@vueuse/core"
import { useDateFunctions } from "@/composables/DateFunctions"
import WeekHistoryBody from "./WeekHistoryBody.vue"
import type { AuditLogEntry } from "../types/audit-types"

const props = defineProps<{
    /** Id shared with the wrapper so it can name the surface via aria-labelledby */
    titleId: string
    /** "rotation" shows the clinician as the subject; "clinician" shows the rotation */
    viewMode: "rotation" | "clinician"
    weekNumber: number
    weekDateStart: string
    contextLabel: string
    entries: AuditLogEntry[]
    isLoading: boolean
    error: string | null
    /** Fill the wrapper (dialog) instead of the fixed popover width */
    fluid?: boolean
    /** Enable the "Previous week" control (false at the first week of the schedule) */
    canPrev?: boolean
    /** Enable the "Next week" control (false at the last week of the schedule) */
    canNext?: boolean
}>()

defineEmits<{ retry: []; prev: []; next: [] }>()

const { formatDate } = useDateFunctions()

// Tooltips are a hover affordance; render them only when the primary input can hover
const supportsHover = useMediaQuery("(hover: hover)")

// Precomputed template conditionals, so the template carries fewer inline branches
const showPrevTooltip = computed(() => supportsHover.value && props.canPrev)
const showNextTooltip = computed(() => supportsHover.value && props.canNext)

// Feed the measured content height to the viewport, which CSS transitions.
const viewport = useTemplateRef<HTMLElement>("viewport")
const measure = useTemplateRef<HTMLElement>("measure")
const bodyHeight = ref("auto")

useResizeObserver(measure, () => {
    const el = measure.value
    if (el) bodyHeight.value = `${el.offsetHeight}px`
})

// Reset scroll to the top on each new week.
watch(
    () => props.weekNumber,
    () => {
        if (viewport.value) viewport.value.scrollTop = 0
    },
)

// Creep toward ~90% while loading, then snap to 100%: a determinate bar that always
// completes reads better than an indeterminate pulse cut off by a quick load.
const progress = ref(0)
const showProgress = ref(false)

const { pause: pauseTrickle, resume: resumeTrickle } = useIntervalFn(
    () => {
        progress.value += (0.9 - progress.value) * 0.22
    },
    160,
    { immediate: false },
)

// Hold the completed bar briefly so the fill shows, then remove it.
const { start: scheduleHide, stop: cancelHide } = useTimeoutFn(
    () => {
        showProgress.value = false
        progress.value = 0
    },
    280,
    { immediate: false },
)

watch(
    () => props.isLoading,
    (loading) => {
        if (loading) {
            cancelHide()
            showProgress.value = true
            progress.value = 0.12
            resumeTrickle()
        } else {
            pauseTrickle()
            if (showProgress.value) {
                progress.value = 1
                scheduleHide()
            }
        }
    },
    { immediate: true },
)
</script>

<style scoped>
.week-history {
    width: 20rem;
    max-width: 88vw;
}

.week-history--fluid {
    width: 100%;
    max-width: 100%;
}

.week-history__header {
    display: flex;
    flex-direction: column;
    gap: 0.375rem;
    padding: 0.5rem 0.375rem 0.5rem 0.875rem;

    /* Positioned so the loading bar can anchor here; kept above the scrolling viewport */
    position: sticky;
    top: 0;
    z-index: 1;
    background: #fff;
    border-bottom: 0.0625rem solid var(--ucdavis-black-10);
}

.week-history__bar {
    display: flex;
    align-items: flex-start;
    gap: 0.5rem;
}

.week-history__nav {
    display: flex;
    align-items: center;
    gap: 0.25rem;
}

.week-history__nav-btn {
    flex-shrink: 0;
}

.week-history__nav-label {
    flex: 1;
    min-width: 0;
    text-align: center;
    font-size: 0.8rem;
    font-weight: 600;
    color: var(--ucdavis-black-90);
}

.week-history__progress {
    position: absolute;
    right: 0;
    bottom: -0.0625rem;
    left: 0;
}

/* Smooth the fill between trickle steps and on the final snap to 100% */
.week-history__progress :deep(.q-linear-progress__model) {
    transition: transform 0.35s cubic-bezier(0.22, 1, 0.36, 1);
}

/* Fade out on removal */
.week-history-progress-leave-active {
    transition: opacity 0.25s ease;
}

.week-history-progress-leave-to {
    opacity: 0;
}

@media screen and (prefers-reduced-motion: reduce) {
    .week-history__progress :deep(.q-linear-progress__model) {
        transition: none;
    }

    .week-history-progress-leave-active {
        transition: none;
    }
}

.week-history__viewport {
    overflow-y: auto;
    max-height: 55vh;

    /* Height comes from the measured content (script); animate size changes between weeks */
    height: v-bind("bodyHeight");
    transition: height 0.24s cubic-bezier(0.22, 1, 0.36, 1);
    will-change: height;
}

@media screen and (prefers-reduced-motion: reduce) {
    .week-history__viewport {
        transition: none;
    }
}

.week-history__heading {
    flex: 1;
    min-width: 0;
    padding-top: 0.125rem;
}

.week-history__close {
    flex-shrink: 0;
}

.week-history__title {
    font-size: 0.9rem;
    font-weight: 700;
    color: var(--ucdavis-blue-100);
    line-height: 1.2;
}

.week-history__subtitle {
    margin-top: 0.125rem;
    font-size: 0.75rem;
    color: var(--ucdavis-black-70);
}
</style>
