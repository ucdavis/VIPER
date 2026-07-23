<template>
    <q-td
        v-if="cellProps"
        :props="cellProps"
    >
        {{ formatted }}
        <span class="text-grey-7">{{ stamp.modifiedBy }}</span>
        <span
            v-if="isRecent"
            class="recency-badge"
        >
            <StatusBadge
                color="info"
                label="Recently updated"
            />
        </span>
    </q-td>
    <span v-else>
        {{ formatted }}
        <span class="text-grey-7">{{ stamp.modifiedBy }}</span>
        <span
            v-if="isRecent"
            class="recency-badge"
        >
            <StatusBadge
                color="info"
                label="Recently updated"
            />
        </span>
    </span>
</template>

<script setup lang="ts">
import { computed } from "vue"
import StatusBadge from "@/components/StatusBadge.vue"

type ModifiedRow = { modifiedOn: string; modifiedBy: string }

// Renders a <q-td> when given table cell props, or a plain inline span when given a
// row directly (QTable card/grid mode on small screens), so both layouts share one
// date format.
const props = defineProps<{
    cellProps?: { row: ModifiedRow }
    row?: ModifiedRow
}>()

const stamp = computed<ModifiedRow>(() => props.cellProps?.row ?? props.row ?? { modifiedOn: "", modifiedBy: "" })

const formatted = computed(() =>
    stamp.value.modifiedOn
        ? new Date(stamp.value.modifiedOn).toLocaleDateString("en-US", {
              year: "2-digit",
              month: "2-digit",
              day: "2-digit",
          })
        : "",
)

// Flag rows edited within the last week so a listing surfaces freshly-changed content at a glance
// (matches the 7-day "recent" window CMS uses elsewhere, e.g. Files' purge-soon threshold).
const RECENT_DAYS = 7

const isRecent = computed(() => {
    if (!stamp.value.modifiedOn) return false
    const modified = new Date(stamp.value.modifiedOn).getTime()
    if (Number.isNaN(modified)) return false
    return Date.now() - modified <= RECENT_DAYS * 86_400_000
})
</script>

<style scoped>
/* Drop the badge onto its own line under the date so the Modified column keeps its narrow
   width instead of stretching to fit the label inline. */
.recency-badge {
    display: block;
    margin-top: 2px;
}
</style>
