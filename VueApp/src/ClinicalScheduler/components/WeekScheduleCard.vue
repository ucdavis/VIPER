<template>
  <q-card
    :class="cardClasses"
    clickable
    @click="$emit('click', week)"
  >
    <q-card-section class="q-pa-sm">
      <!-- Week Header with Date -->
      <div class="text-center text-weight-medium text-grey-8 q-mb-sm row items-center justify-center q-gutter-xs">
        <slot
          name="week-icon"
          :week="week"
        />
        <span>Week {{ week.weekNumber }} ({{ formatDate(week.dateStart) }})</span>
      </div>
      
      <!-- Assignment Content - Slot for flexibility -->
      <slot
        name="assignments"
        :week="week"
      >
        <div class="text-center q-py-sm">
          <div class="text-grey-6 text-caption">
            {{ !isPastYear ? 'Click to add assignment' : 'No assignments' }}
          </div>
        </div>
      </slot>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useDateFunctions } from '@/composables/DateFunctions'

const { formatDate } = useDateFunctions()

// Interface for what this component actually needs from week objects
export interface WeekItem {
  weekId: number
  weekNumber: number
  dateStart: string
  [key: string]: any // Allow additional properties like rotation, isPrimaryEvaluator, etc.
}

interface Props {
  week: WeekItem
  isPastYear?: boolean
  additionalClasses?: string | string[] | Record<string, boolean>
}

const props = withDefaults(defineProps<Props>(), {
  isPastYear: false,
  additionalClasses: ''
})

defineEmits<{
  click: [week: WeekItem]
}>()

const cardClasses = computed(() => {
  const baseClasses = 'col-xs-12 col-sm-6 col-md-4 col-lg-3 col-xl-2 cursor-pointer week-schedule-card'
  
  let additional = ''
  if (Array.isArray(props.additionalClasses)) {
    additional = props.additionalClasses.join(' ')
  } else if (typeof props.additionalClasses === 'object') {
    // Handle object form like { 'requires-primary-card': true, 'col-lg-3': true }
    additional = Object.entries(props.additionalClasses)
      .filter(([, value]) => value)
      .map(([key]) => key)
      .join(' ')
  } else {
    additional = props.additionalClasses || ''
  }
  
  return `${baseClasses} ${additional}`
})
</script>

<style scoped>
.week-schedule-card {
  max-width: 280px;
  min-width: 200px;
}

.week-schedule-card .q-card {
  height: 100%;
}

/* Ensure long names wrap properly */
.week-schedule-card .text-body2 {
  word-wrap: break-word;
  overflow-wrap: break-word;
  hyphens: auto;
  line-height: 1.2;
}
</style>