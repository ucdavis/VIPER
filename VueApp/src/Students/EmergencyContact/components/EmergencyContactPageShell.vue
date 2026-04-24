<script setup lang="ts">
import StatusBanner from "@/components/StatusBanner.vue"

defineProps<{
    loading: boolean
    detail: { fullName: string; isAdmin?: boolean } | null
}>()
</script>

<template>
    <div class="q-pa-md">
        <q-breadcrumbs class="q-mb-sm">
            <q-breadcrumbs-el
                label="Emergency Contacts"
                :to="detail?.isAdmin ? { name: 'EmergencyContactList' } : undefined"
            />
            <q-breadcrumbs-el :label="detail?.fullName ?? 'Loading...'" />
        </q-breadcrumbs>

        <q-spinner
            v-if="loading"
            color="primary"
            size="2rem"
            class="q-ma-lg"
            aria-label="Loading contact information"
        />

        <template v-else-if="detail">
            <slot />
        </template>

        <div
            v-else
            class="q-mt-md"
        >
            <StatusBanner type="warning">Student contact record not found.</StatusBanner>
        </div>
    </div>
</template>
