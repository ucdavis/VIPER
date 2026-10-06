<template>
    <div class="row q-col-gutter-md items-start q-mb-md">
        <div class="col-auto">
            <img
                :src="eisService.photoUrl(header.employeeId)"
                :alt="`Photo of ${header.name}`"
                width="87"
                height="111"
            />
        </div>
        <div class="col">
            <EisFactList :facts="facts" />
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed } from "vue"
import EisFactList from "./EisFactList.vue"
import { eisService } from "../services/eis-service"
import type { EisPersonHeader } from "../types/eis-types"

/**
 * The short header shown above every EIS page: photo, ID, title and departments, plus the
 * staff or faculty program for people who hold one.
 */
const { header } = defineProps<{ header: EisPersonHeader }>()

const facts = computed(() => [
    { label: "Employee ID", value: header.employeeId },
    { label: "Primary affiliation", value: header.primaryAffiliation ?? "" },
    { label: "Primary title", value: header.primaryTitle ?? "" },
    { label: "Job group ID", value: header.jobGroup ?? "" },
    { label: "Home department", value: header.homeDepartment ?? "" },
    { label: "Alternate department", value: header.alternateDepartment ?? "" },
    ...(header.isStaff
        ? [
              { label: "Staff program", value: header.staffProgram ?? "" },
              { label: "Staff type", value: header.staffStatus ?? "" },
          ]
        : []),
    ...(header.isFaculty
        ? [
              { label: "Faculty program", value: header.facultyProgram ?? "" },
              { label: "Ladder rank", value: header.ladderRank ?? "" },
          ]
        : []),
])
</script>
