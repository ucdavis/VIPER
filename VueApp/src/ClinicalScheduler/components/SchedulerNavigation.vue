<template>
  <q-tabs
    v-model="currentTab"
    class="text-grey q-mb-md"
    active-color="primary"
    indicator-color="primary"
    align="left"
    no-caps
  >
    <q-route-tab
      name="home"
      label="Home"
      to="/ClinicalScheduler/"
      exact
    />
    <q-route-tab
      name="rotation"
      label="Schedule by Rotation"
      to="/ClinicalScheduler/rotation"
      exact
    />
    <q-route-tab
      name="clinician"
      label="Schedule by Clinician"
      to="/ClinicalScheduler/clinician"
      exact
    />
  </q-tabs>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()

// Determine current tab based on route
const currentTab = computed(() => {
  const path = route.path
  if (path === '/ClinicalScheduler/' || path === '/ClinicalScheduler') {
    return 'home'
  } else if (path.startsWith('/ClinicalScheduler/rotation')) {
    return 'rotation'
  } else if (path.startsWith('/ClinicalScheduler/clinician')) {
    return 'clinician'
  }
  return 'home'
})
</script>