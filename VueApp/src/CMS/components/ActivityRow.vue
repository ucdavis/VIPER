<template>
    <q-item class="q-px-none activity-row">
        <q-item-section side>
            <q-icon
                :name="item.icon"
                :aria-label="item.typeLabel"
                color="secondary"
                size="sm"
            />
        </q-item-section>
        <q-item-section>
            <q-item-label lines="1">
                <router-link
                    :to="item.to"
                    class="activity-link"
                    >{{ item.label }}</router-link
                >
            </q-item-label>
            <q-item-label
                caption
                :title="formatFullDate(item.modifiedOn)"
            >
                {{ item.typeLabel }} &middot; {{ item.verb ? item.verb + " " : ""
                }}{{ formatTimeAgo(new Date(item.modifiedOn)) }} by
                {{ item.modifiedBy }}
            </q-item-label>
        </q-item-section>
        <q-item-section
            v-if="item.actions?.length"
            side
            class="activity-actions"
        >
            <div class="row no-wrap">
                <q-btn
                    v-for="action in item.actions"
                    :key="action.label"
                    flat
                    dense
                    round
                    size="sm"
                    color="primary"
                    :icon="action.icon"
                    :aria-label="action.label"
                    :to="action.to"
                    @click="runAction(action)"
                >
                    <q-tooltip>{{ action.label }}</q-tooltip>
                </q-btn>
            </div>
        </q-item-section>
    </q-item>
</template>

<script setup lang="ts">
import { formatTimeAgo } from "@vueuse/core"
import type { ActivityItem, RailAction } from "@/CMS/types/"

defineProps<{
    item: ActivityItem
}>()

function formatFullDate(value: string): string {
    return new Date(value).toLocaleString()
}

// Actions with their own `to` navigate via QBtn; run-actions just run. The stretched
// title link sits below the action buttons (z-index), so a button click can't reach it.
function runAction(action: RailAction) {
    action.run?.()
}
</script>

<style scoped>
/* Whole-row clickability without nesting controls inside an anchor (invalid HTML, ambiguous
   for AT): the title is the only row link, stretched over the row via ::after, and the action
   buttons sit above the overlay. Screen readers announce one link named by the title. */
.activity-row {
    position: relative;
}

.activity-link {
    color: inherit;
    text-decoration: none;
}

.activity-link::after {
    content: "";
    position: absolute;
    inset: 0;
}

.activity-link:hover,
.activity-link:focus-visible {
    text-decoration: underline;
}

.activity-row:hover,
.activity-row:focus-within {
    background: var(--surface-tint);
}

/* Keep the two icon actions from inflating the dense rows; the buttons carry their own
   padding and get the standard 44px targets on coarse pointers via base.css. */
.activity-actions {
    padding-left: 0;
    position: relative;
    z-index: 1;
}

/* Quiet the rail: row actions fade in on row hover or keyboard focus. Opacity (not display)
   keeps the buttons in the layout and the tab order, so rows never shift and focus reveals
   them for keyboard users. */
.activity-actions .q-btn {
    opacity: 0;
    transition: opacity 0.15s ease-out;
}

.q-item:hover .activity-actions .q-btn,
.q-item:focus-within .activity-actions .q-btn {
    opacity: 1;
}

/* No hover on touch: keep the actions always visible so they don't need a discovery tap
   that would fight the row's own navigation. */
@media (pointer: coarse) {
    .activity-actions .q-btn {
        opacity: 1;
    }
}

@media (prefers-reduced-motion: reduce) {
    .activity-actions .q-btn {
        transition: none;
    }
}
</style>
