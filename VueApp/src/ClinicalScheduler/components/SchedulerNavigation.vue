<template>
  <!-- Navigation tabs with proper active state for parameterized routes -->
  <q-tabs
    class="text-grey q-mb-md"
    active-color="primary"
    indicator-color="primary"
    align="left"
    no-caps
    role="tablist"
    aria-label="Clinical Scheduler Navigation"
  >
    <q-route-tab
      name="home"
      label="Home"
      to="/ClinicalScheduler/"
      :exact="true"
      :aria-controls="`home-panel`"
      :id="`home-tab`"
      role="tab"
    />
    <q-route-tab
      name="rotation"
      label="Schedule by Rotation"
      :to="{ name: 'RotationSchedule' }"
      :class="rotationTabClass"
      :aria-controls="`rotation-panel`"
      :id="`rotation-tab`"
      role="tab"
    />
    <q-route-tab
      name="clinician"
      label="Schedule by Clinician"
      :to="{ name: 'ClinicianSchedule' }"
      :class="clinicianTabClass"
      :aria-controls="`clinician-panel`"
      :id="`clinician-tab`"
      role="tab"
    />
  </q-tabs>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()

// Helper to check if a tab should be active based on route name
const isTabActive = (baseRouteName: string): string => {
  const routeName = route.name as string
  const isActive = routeName === baseRouteName || routeName === `${baseRouteName}WithId`
  return isActive ? 'q-tab--active' : ''
}

const rotationTabClass = computed(() => isTabActive('RotationSchedule'))
const clinicianTabClass = computed(() => isTabActive('ClinicianSchedule'))
</script>

<style scoped>
/* Fix text color when tab is manually marked as active but Quasar thinks it's inactive */
:deep(.q-tab--active.q-tab--inactive) {
  color: var(--q-primary) !important;
  opacity: 1 !important;
}
</style>
