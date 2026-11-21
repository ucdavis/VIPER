<template>
    <BreadcrumbHeading
        label="Report problems"
        parent-label="Manage reports"
        :parent-to="{ name: 'ReportAdmin' }"
    />
    <ReportProblemsCheck />
    <ReportFailedRuns />
</template>

<script setup lang="ts">
import BreadcrumbHeading from "@/components/BreadcrumbHeading.vue"
import ReportFailedRuns from "../components/ReportFailedRuns.vue"
import ReportProblemsCheck from "../components/ReportProblemsCheck.vue"
</script>
