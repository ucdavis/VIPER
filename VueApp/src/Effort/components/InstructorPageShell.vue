<script setup lang="ts">
import StatusBanner from "@/components/StatusBanner.vue"

defineProps<{
    termCode: number
    isLoading: boolean
    loadError?: string
}>()
</script>

<template>
    <div class="q-pa-md">
        <!-- Breadcrumb -->
        <q-breadcrumbs class="q-mb-md">
            <q-breadcrumbs-el
                label="Instructors"
                :to="{ name: 'InstructorList', params: { termCode } }"
            />
            <slot name="breadcrumbs" />
        </q-breadcrumbs>

        <!-- Loading state -->
        <div
            v-if="isLoading"
            class="text-center q-my-lg"
        >
            <q-spinner-dots
                size="3rem"
                color="primary"
            />
            <div class="q-mt-md text-body1">Loading instructor...</div>
        </div>

        <!-- Error state -->
        <StatusBanner
            v-else-if="loadError"
            type="error"
        >
            {{ loadError }}
            <template #action>
                <q-btn
                    flat
                    label="Go Back"
                    :to="{ name: 'InstructorList', params: { termCode } }"
                />
            </template>
        </StatusBanner>

        <!-- Content -->
        <template v-else>
            <slot />
        </template>
    </div>
</template>
