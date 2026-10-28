<script setup lang="ts">
import StatusBanner from "@/components/StatusBanner.vue"

/**
 * Frame for a page showing one student's record in a self-service app: breadcrumbs back to the
 * app's student list, a spinner while loading, and a notice when the record is not found.
 */
defineProps<{
    loading: boolean
    detail: { fullName: string } | null
    appLabel: string // Breadcrumb text for the app, e.g. "Career Selection".
    listRouteName: string
    canViewList: boolean // Whether the breadcrumb links to the student list; only list viewers may open it.
    recordLabel: string // What the record holds, e.g. "contact", as read in "Student contact record not found."
}>()
</script>

<template>
    <div class="q-pa-md">
        <q-breadcrumbs class="q-mb-sm">
            <q-breadcrumbs-el
                :label="appLabel"
                :to="canViewList ? { name: listRouteName } : undefined"
            />
            <q-breadcrumbs-el :label="detail?.fullName ?? 'Loading...'" />
        </q-breadcrumbs>

        <q-spinner
            v-if="loading"
            color="primary"
            size="2rem"
            class="q-ma-lg"
            :aria-label="`Loading ${recordLabel} information`"
        />

        <template v-else-if="detail">
            <slot />
        </template>

        <div
            v-else
            class="q-mt-md"
        >
            <StatusBanner type="warning">Student {{ recordLabel }} record not found.</StatusBanner>
        </div>
    </div>
</template>
