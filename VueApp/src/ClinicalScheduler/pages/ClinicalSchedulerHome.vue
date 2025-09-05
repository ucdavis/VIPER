<template>
    <div
        class="q-pa-md"
        style="max-width: 800px"
    >
        <div class="row items-center q-mb-md">
            <h1 class="text-h4 text-primary q-my-none">Clinical Scheduler</h1>
        </div>

        <!-- Loading state -->
        <div
            v-if="permissionsStore.isLoading"
            class="text-center q-my-lg"
        >
            <q-spinner-dots
                :size="UI_CONFIG.LOADING_SPINNER_SIZE"
                color="primary"
            />
            <div class="q-mt-md text-body1">Loading permissions...</div>
        </div>

        <!-- Access denied for users without any edit permissions -->
        <AccessDeniedCard
            v-else-if="!permissionsStore.hasAnyEditPermission"
            :message="ACCESS_DENIED_MESSAGES.CLINICAL_SCHEDULER"
            :subtitle="ACCESS_DENIED_SUBTITLES.CLINICAL_SCHEDULER"
        />

        <!-- Main content for users with permissions -->
        <div v-else>
            <!-- Permission info banner for limited access users -->
            <PermissionInfoBanner />

            <div>
                <h2 class="text-h5 text-grey-8 q-mb-md">Select Scheduling View:</h2>

                <div class="row q-gutter-md">
                    <!-- Schedule by Rotation - available to rotation-specific and full access users -->
                    <div
                        v-if="canAccessRotationView"
                        class="col-12 col-sm-auto"
                        style="min-width: 300px; max-width: 400px; flex: 1"
                    >
                        <q-card
                            flat
                            bordered
                            class="cursor-pointer view-card-custom"
                            :class="{
                                'view-card-hover': rotationHovered,
                                'shadow-2': !rotationHovered,
                                'shadow-4': rotationHovered,
                            }"
                            tabindex="0"
                            @click="navigateToRotationView"
                            @keydown.enter="navigateToRotationView"
                            @keydown.space="navigateToRotationView"
                            @mouseenter="rotationHovered = true"
                            @mouseleave="rotationHovered = false"
                            @focus="rotationHovered = true"
                            @blur="rotationHovered = false"
                        >
                            <q-card-section>
                                <div class="row items-center justify-between q-mb-sm">
                                    <h3 class="text-h6 text-primary q-my-none">Schedule by Rotation</h3>
                                    <q-icon
                                        name="assignment"
                                        size="24px"
                                        color="primary"
                                        class="opacity-70"
                                    />
                                </div>
                                <p class="text-grey-7 q-mb-sm">Schedule clinicians for a specific rotation</p>
                                <q-banner
                                    v-if="permissionsStore.hasOnlyServiceSpecificPermissions"
                                    dense
                                    inline-actions
                                    class="text-primary bg-primary-1 rounded-borders"
                                >
                                    <template #avatar>
                                        <q-icon
                                            name="info"
                                            size="14px"
                                        />
                                    </template>
                                    <span class="text-caption">Limited to your authorized rotations</span>
                                </q-banner>
                            </q-card-section>
                        </q-card>
                    </div>

                    <!-- Schedule by Clinician - not available to rotation-specific only users -->
                    <div
                        v-if="canAccessClinicianView"
                        class="col-12 col-sm-auto"
                        style="min-width: 300px; max-width: 400px; flex: 1"
                    >
                        <q-card
                            flat
                            bordered
                            class="cursor-pointer view-card-custom"
                            :class="{
                                'view-card-hover': clinicianHovered,
                                'shadow-2': !clinicianHovered,
                                'shadow-4': clinicianHovered,
                            }"
                            tabindex="0"
                            @click="navigateToClinicianView"
                            @keydown.enter="navigateToClinicianView"
                            @keydown.space="navigateToClinicianView"
                            @mouseenter="clinicianHovered = true"
                            @mouseleave="clinicianHovered = false"
                            @focus="clinicianHovered = true"
                            @blur="clinicianHovered = false"
                        >
                            <q-card-section>
                                <div class="row items-center justify-between q-mb-sm">
                                    <h3 class="text-h6 text-primary q-my-none">Schedule by Clinician</h3>
                                    <q-icon
                                        name="person"
                                        size="24px"
                                        color="primary"
                                        class="opacity-70"
                                    />
                                </div>
                                <p class="text-grey-7 q-mb-sm">Schedule rotations for a specific clinician</p>
                                <q-banner
                                    v-if="permissionsStore.hasOnlyOwnSchedulePermission"
                                    dense
                                    inline-actions
                                    class="text-primary bg-primary-1 rounded-borders"
                                >
                                    <template #avatar>
                                        <q-icon
                                            name="info"
                                            size="14px"
                                        />
                                    </template>
                                    <span class="text-caption">Your schedule only</span>
                                </q-banner>
                            </q-card-section>
                        </q-card>
                    </div>

                    <!-- Disabled card explanation for rotation-specific users -->
                    <div
                        v-if="permissionsStore.hasOnlyServiceSpecificPermissions"
                        class="col-12 col-sm-auto"
                        style="min-width: 300px; max-width: 400px; flex: 1"
                    >
                        <q-card
                            flat
                            bordered
                            disable
                            class="cursor-not-allowed bg-grey-1"
                            tabindex="0"
                        >
                            <q-card-section class="opacity-60">
                                <div class="row items-center justify-between q-mb-sm">
                                    <h3 class="text-h6 text-grey-6 q-my-none">Schedule by Clinician</h3>
                                    <q-icon
                                        name="person_off"
                                        size="24px"
                                        color="grey-6"
                                        class="opacity-70"
                                    />
                                </div>
                                <p class="text-grey-6 q-mb-sm">Not available with rotation-specific permissions</p>
                                <q-banner
                                    dense
                                    inline-actions
                                    class="text-grey-7 bg-grey-3 rounded-borders"
                                >
                                    <template #avatar>
                                        <q-icon
                                            name="lock"
                                            size="14px"
                                        />
                                    </template>
                                    <span class="text-caption">Contact admin for full access</span>
                                </q-banner>
                            </q-card-section>
                        </q-card>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue"
import { useRouter } from "vue-router"
import { usePermissionsStore } from "../stores/permissions"
import PermissionInfoBanner from "../components/PermissionInfoBanner.vue"
import AccessDeniedCard from "../components/AccessDeniedCard.vue"
import { ACCESS_DENIED_MESSAGES, ACCESS_DENIED_SUBTITLES } from "../constants/permission-messages"
import { UI_CONFIG } from "../constants/app-constants"

// Composables
const router = useRouter()
const permissionsStore = usePermissionsStore()

// Reactive hover states for cards
const rotationHovered = ref(false)
const clinicianHovered = ref(false)

// Computed
const canAccessRotationView = computed(() => {
    // Use the store's canAccessRotationView for consistency
    return permissionsStore.canAccessRotationView
})

const canAccessClinicianView = computed(() => {
    // Available to full access users and own schedule users, but NOT rotation-specific only users
    return permissionsStore.canAccessClinicianView
})

// Methods
const navigateToRotationView = () => {
    if (canAccessRotationView.value) {
        void router.push("/ClinicalScheduler/rotation")
    }
}

const navigateToClinicianView = () => {
    if (canAccessClinicianView.value) {
        void router.push("/ClinicalScheduler/clinician")
    }
}

// Lifecycle
onMounted(async () => {
    // Set page title
    document.title = "VIPER - Clinical Scheduler"

    // Initialize permissions if not already loaded
    if (!permissionsStore.userPermissions) {
        try {
            await permissionsStore.initialize()
        } catch {
            // Handle initialization errors gracefully - component will fall back to default state
        }
    }
})
</script>

<style scoped>
/* Custom card styling to work with Quasar */
.view-card-custom {
    background-color: #f9f9f9;
    transition: all 0.3s ease;
}

.view-card-hover {
    transform: translateY(-2px);
}

.opacity-70 {
    opacity: 0.7;
}

.opacity-60 {
    opacity: 0.6;
}

/* Reduced motion support */
@media screen and (prefers-reduced-motion: reduce) {
    .view-card-custom {
        transition: none;
    }

    .view-card-hover {
        transform: none;
    }
}
</style>
