<template>
  <q-tabs
    v-model="currentTab"
    class="text-grey q-mb-md"
    active-color="primary"
    indicator-color="primary"
    align="left"
    no-caps
  >
    <q-tab
      name="home"
      label="Home"
      @click="navigateTo('/ClinicalScheduler/')"
    />
    <q-tab
      name="rotation"
      label="Schedule by Rotation"
      @click="navigateTo('/ClinicalScheduler/rotation')"
    />
    <q-tab
      name="clinician"
      label="Schedule by Clinician"
      @click="navigateTo('/ClinicalScheduler/clinician')"
    />
  </q-tabs>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const route = useRoute()
const router = useRouter()

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

const navigateTo = (path: string) => {
  router.push(path)
}
</script>