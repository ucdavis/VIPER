<script setup lang="ts">
import { computed } from "vue"

/**
 * A student's name in a roster, linking to their record. A student with no AAUD mapping has no
 * PersonId to route on, so their name is shown unlinked with a warning instead.
 */
const props = defineProps<{
    student: { personId: number; fullName: string; hasDetailRoute: boolean }
    editRoute: string
    viewRoute: string
    // Links to the edit page rather than the read-only view.
    canEdit: boolean
    // Bolder name, for mobile cards where it heads the card.
    emphasized?: boolean
}>()

const to = computed(() => ({
    name: props.canEdit ? props.editRoute : props.viewRoute,
    params: { pidm: props.student.personId },
}))
</script>

<template>
    <router-link
        v-if="student.hasDetailRoute"
        :to="to"
        class="text-primary"
        :class="{ 'text-weight-medium': emphasized }"
        :aria-label="`${canEdit ? 'Edit' : 'View'} ${student.fullName}`"
    >
        {{ student.fullName }}
        <q-icon
            :name="canEdit ? 'edit' : 'visibility'"
            size="0.875rem"
            class="q-ml-xs"
            aria-hidden="true"
        />
    </router-link>
    <span
        v-else
        class="text-grey-7"
        :class="{ 'text-weight-medium': emphasized }"
    >
        {{ student.fullName }}
        <!-- The tooltip only opens on hover, and Quasar's own aria-hidden on q-icon would
             swallow an aria-label here, so the reason is given as sr-only text alongside. -->
        <q-icon
            name="warning"
            size="0.875rem"
            color="warning"
            class="q-ml-xs"
            aria-hidden="true"
        >
            <q-tooltip>No AAUD mapping, record cannot be opened</q-tooltip>
        </q-icon>
        <span class="sr-only">No AAUD mapping, record cannot be opened</span>
    </span>
</template>
