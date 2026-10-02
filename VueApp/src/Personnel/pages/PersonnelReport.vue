<template>
    <ReportRunner
        :report-key="reportKey"
        parent-label="Personnel"
        :parent-to="{ name: 'PersonnelHome' }"
    />
</template>

<script setup lang="ts">
import { computed } from "vue"
import { useRoute } from "vue-router"
import ReportRunner from "@/shared/reports/ReportRunner.vue"

const route = useRoute()

const reportKey = computed(() => String(route.params.key))
</script>
