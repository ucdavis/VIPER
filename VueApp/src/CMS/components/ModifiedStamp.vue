<template>
    <q-td
        v-if="cellProps"
        :props="cellProps"
    >
        {{ formatted }}
        <span class="text-grey-7">{{ stamp.modifiedBy }}</span>
    </q-td>
    <span v-else>
        {{ formatted }}
        <span class="text-grey-7">{{ stamp.modifiedBy }}</span>
    </span>
</template>

<script setup lang="ts">
import { computed } from "vue"

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
</script>
