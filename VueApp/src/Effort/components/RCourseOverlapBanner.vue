<template>
    <StatusBanner
        v-if="courses.length > 0"
        type="warning"
    >
        {{ mode === "self" ? "You have" : "This instructor has" }} effort on both RES 000R-001 and
        {{ courses.join(", ") }}. Check that the same teaching isn't counted twice.
    </StatusBanner>
</template>

<script setup lang="ts">
import { computed } from "vue"
import StatusBanner from "@/components/StatusBanner.vue"
import type { InstructorEffortRecordDto } from "../types"
import { overlappingRCourses } from "../utils/r-course-overlap"

const props = defineProps<{
    records: InstructorEffortRecordDto[]
    mode: "self" | "staff"
}>()

const courses = computed(() => overlappingRCourses(props.records))
</script>
