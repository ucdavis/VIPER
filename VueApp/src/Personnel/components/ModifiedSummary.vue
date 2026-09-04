<template>
    <div>
        {{ label }} Modified
        {{ formattedDate || "Never" }}
        <span v-if="by">by {{ by }}</span>
    </div>
</template>

<script setup lang="ts">
import { computed } from "vue"
import { useDateFunctions } from "@/composables/DateFunctions.ts"

/**
 * "<label> Modified <date> by <person>" for the record dialogs. A record nobody has touched
 * reads as "Never", and one whose author was never recorded omits the "by" clause rather than
 * trailing off after the word.
 */
const props = defineProps<{
    label: string
    /** Typed Date by the API models, but JSON delivers the ISO string form. Both are accepted. */
    date: Date | string | null | undefined
    by: string | null | undefined
}>()

const { formatDate } = useDateFunctions()

/**
 * formatDate reads the date half of an ISO string, so a real Date has to be spelled that way
 * first. Built from local parts rather than toISOString(), which would shift the day for anyone
 * east of UTC.
 */
function toIsoDate(value: Date | string): string {
    if (!(value instanceof Date)) {
        return value
    }
    const month = String(value.getMonth() + 1).padStart(2, "0")
    const day = String(value.getDate()).padStart(2, "0")
    return `${value.getFullYear()}-${month}-${day}`
}

const formattedDate = computed(() => formatDate(toIsoDate(props.date ?? "")))
</script>
