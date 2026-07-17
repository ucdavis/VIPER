<template>
    <!-- Initial load only; while paging we keep the prior week's rows (below) -->
    <div
        v-if="showSkeleton"
        class="week-history__body"
    >
        <div
            v-for="n in 3"
            :key="n"
            class="week-history__skeleton-row"
        >
            <q-skeleton
                type="QBadge"
                width="6rem"
                height="1.125rem"
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

    <div
        v-else-if="error"
        class="week-history__body"
    >
        <StatusBanner type="error">
            {{ error }}
            <template #action>
                <q-btn
                    flat
                    dense
                    no-caps
                    size="sm"
                    color="primary"
                    label="Retry"
                    @click="$emit('retry')"
                />
            </template>
        </StatusBanner>
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

    <!-- While paging, the prior week's rows stay (dimmed) until new data arrives -->
    <ul
        v-else
        :class="listClass"
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
</template>

<script setup lang="ts">
import { computed } from "vue"
import { useDateFunctions } from "@/composables/DateFunctions"
import StatusBadge from "@/components/StatusBadge.vue"
import StatusBanner from "@/components/StatusBanner.vue"
import { getAuditActionColor } from "../utils/audit-actions"
import type { AuditLogEntry } from "../types/audit-types"

const props = defineProps<{
    /** "rotation" shows the clinician as the subject; "clinician" shows the rotation */
    viewMode: "rotation" | "clinician"
    entries: AuditLogEntry[]
    isLoading: boolean
    error: string | null
}>()

defineEmits<{ retry: [] }>()

const { formatDateTime } = useDateFunctions()

const showSkeleton = computed(() => props.isLoading && props.entries.length === 0)
const listClass = computed(() => ["week-history__list", { "week-history__list--loading": props.isLoading }])

// Show the dimension that varies: the clinician (rotation view) or the rotation (clinician view).
function subjectFor(entry: AuditLogEntry): string {
    return props.viewMode === "rotation" ? entry.personName : entry.rotationName
}
</script>

<style scoped>
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

    /* Fade back to full when fresh rows replace the dimmed ones */
    transition: opacity 0.2s ease;
}

.week-history__list--loading {
    /* Dim the outgoing week while the next loads */
    opacity: 0.45;
}

@media screen and (prefers-reduced-motion: reduce) {
    .week-history__list {
        transition: none;
    }
}

.week-history__item {
    padding: 0.5rem 0.875rem;
    border-top: 0.0625rem solid var(--ucdavis-black-10);
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
