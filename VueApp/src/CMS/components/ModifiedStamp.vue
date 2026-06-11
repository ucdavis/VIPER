<template>
    <q-td :props="cellProps">
        {{ formatted }}
        <span class="text-grey-7">{{ cellProps.row.modifiedBy }}</span>
    </q-td>
</template>

<script setup lang="ts">
import { computed } from "vue"

const props = defineProps<{
    cellProps: { row: { modifiedOn: string; modifiedBy: string } }
}>()

const formatted = computed(() =>
    new Date(props.cellProps.row.modifiedOn).toLocaleDateString("en-US", {
        year: "2-digit",
        month: "2-digit",
        day: "2-digit",
    }),
)
</script>
