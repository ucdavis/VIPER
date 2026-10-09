<template>
    <div
        v-if="loading"
        role="status"
    >
        <q-spinner-dots
            size="2rem"
            color="primary"
            aria-hidden="true"
        />
        <span class="sr-only">Loading {{ what }}</span>
    </div>
    <StatusBanner
        v-else-if="data === null"
        type="error"
    >
        The {{ what }} could not be loaded.
    </StatusBanner>
    <slot
        v-else
        :data="data"
    />
</template>

<script setup lang="ts" generic="T">
import StatusBanner from "@/components/StatusBanner.vue"

/**
 * The loading and error states every EIS section page shares. Once the data has loaded, the
 * default slot receives it, no longer null.
 */
defineProps<{
    loading: boolean
    data: T | null
    /** What the page shows, as in "Loading {what}" and "The {what} could not be loaded." */
    what: string
}>()

defineSlots<{ default(props: { data: T }): unknown }>()
</script>
