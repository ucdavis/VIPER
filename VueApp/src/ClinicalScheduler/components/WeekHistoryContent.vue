<template>
    <section
        :class="['week-history', { 'week-history--fluid': fluid }]"
        :aria-labelledby="titleId"
        :aria-busy="isLoading"
    >
        <header class="week-history__header">
            <div class="week-history__heading">
                <div
                    :id="titleId"
                    class="week-history__title"
                >
                    Audit Trail
                </div>
                <div class="week-history__subtitle">
                    {{ contextLabel }} &middot; Week {{ weekNumber }} ({{ formatDate(weekDateStart) }})
                </div>
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
                <q-tooltip :delay="500">Close</q-tooltip>
            </q-btn>
        </header>

        <q-separator />

        <!-- Loading: skeleton rows, not a spinner -->
        <div
            v-if="isLoading"
            class="week-history__body"
        >
            <div
                v-for="n in 3"
                :key="n"
                class="week-history__skeleton-row"
            >
                <q-skeleton
                    type="QBadge"
                    width="96px"
                    height="18px"
                />
                <q-skeleton
                    type="text"
                    width="70%"
                />
                <q-skeleton
                    type="text"
                    width="50%"
                />
            </div>
        </div>

        <!-- Error -->
        <div
            v-else-if="error"
            class="week-history__body week-history__state"
            role="alert"
        >
            <q-icon
                name="error_outline"
                color="negative"
                size="sm"
            />
            <span class="week-history__state-text">{{ error }}</span>
            <q-btn
                flat
                dense
                no-caps
                size="sm"
                color="primary"
                label="Retry"
                @click="$emit('retry')"
            />
        </div>

        <!-- Empty: teach what this surface is -->
        <div
            v-else-if="entries.length === 0"
            class="week-history__body week-history__state"
        >
            <q-icon
                name="history_toggle_off"
                color="grey-7"
                size="sm"
            />
            <span class="week-history__state-text text-grey-7">No audit entries for this week.</span>
        </div>

        <!-- Entries -->
        <ul
            v-else
            class="week-history__list"
        >
            <li
                v-for="entry in entries"
                :key="entry.scheduleAuditId"
                class="week-history__item"
            >
                <StatusBadge :color="getAuditActionColor(entry.action)">{{ entry.action }}</StatusBadge>
                <div class="week-history__subject">{{ subjectFor(entry) }}</div>
                <div class="week-history__meta">
                    by {{ entry.modifiedByName }} &middot;
                    {{ formatDateTime(entry.timeStamp, { dateStyle: "short", timeStyle: "short" }) }}
                </div>
            </li>
        </ul>
    </section>
</template>

<script setup lang="ts">
import { useDateFunctions } from "@/composables/DateFunctions"
import StatusBadge from "@/components/StatusBadge.vue"
import { getAuditActionColor } from "../utils/audit-actions"
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
}>()

defineEmits<{ retry: [] }>()

const { formatDate, formatDateTime } = useDateFunctions()

// Show the dimension that varies: the clinician (rotation view) or the rotation (clinician view).
function subjectFor(entry: AuditLogEntry): string {
    return props.viewMode === "rotation" ? entry.personName : entry.rotationName
}
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
    align-items: flex-start;
    gap: 0.5rem;
    padding: 0.5rem 0.375rem 0.5rem 0.875rem;
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

.week-history__body {
    padding: 0.75rem 0.875rem;
}

.week-history__skeleton-row {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
    padding: 0.375rem 0;
}

.week-history__state {
    display: flex;
    align-items: center;
    gap: 0.5rem;
}

.week-history__state-text {
    font-size: 0.8rem;
}

.week-history__list {
    list-style: none;
    margin: 0;
    padding: 0.375rem 0;
    max-height: 20rem;
    overflow-y: auto;
}

.week-history--fluid .week-history__list {
    max-height: 55vh;
}

.week-history__item {
    padding: 0.5rem 0.875rem;
    border-top: 1px solid var(--ucdavis-black-10);
}

.week-history__item:first-child {
    border-top: none;
}

.week-history__subject {
    margin-top: 0.3125rem;
    font-size: 0.85rem;
    font-weight: 600;
    color: var(--ucdavis-black-90);
    line-height: 1.25;
}

.week-history__meta {
    margin-top: 0.0625rem;
    font-size: 0.72rem;
    color: var(--ucdavis-black-80);
}
</style>
