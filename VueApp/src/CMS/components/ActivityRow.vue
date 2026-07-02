<template>
    <q-item
        clickable
        :to="item.to"
        class="q-px-none"
    >
        <q-item-section side>
            <q-icon
                :name="item.icon"
                :aria-label="item.typeLabel"
                color="secondary"
                size="sm"
            />
        </q-item-section>
        <q-item-section>
            <q-item-label lines="1">{{ item.label }}</q-item-label>
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
                    :key="action.icon"
                    flat
                    dense
                    round
                    size="sm"
                    color="primary"
                    :icon="action.icon"
                    :aria-label="action.label"
                    :to="action.to"
                    @click.stop="runAction(action, $event)"
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

// Row-level navigation must not fire for action clicks (`.stop` in the template). Actions with
// their own `to` navigate via QBtn; only run-actions preventDefault, both to keep the row's
// anchor inert and because a prevented default makes QBtn cancel its router navigation.
function runAction(action: RailAction, event: Event) {
    if (!action.run) return
    event.preventDefault()
    action.run()
}
</script>

<style scoped>
/* Keep the two icon actions from inflating the dense rows; the buttons carry their own
   padding and get the standard 44px targets on coarse pointers via base.css. */
.activity-actions {
    padding-left: 0;
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
