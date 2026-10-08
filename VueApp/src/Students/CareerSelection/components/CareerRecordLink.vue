<script setup lang="ts">
import StudentRecordLink from "@/Students/components/StudentRecordLink.vue"

// A student's name on the career selection roster and report, linking to their record.
defineProps<{
    student: { personId: number; fullName: string; hasDetailRoute: boolean }
    canEdit: boolean
    emphasized?: boolean
}>()
</script>

<template>
    <StudentRecordLink
        :student="student"
        edit-route="CareerSelectionEdit"
        view-route="CareerSelectionView"
        :can-edit="canEdit"
        :emphasized="emphasized"
    />
</template>
