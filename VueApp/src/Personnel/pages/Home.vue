<script setup lang="ts">
import CmsContent from "@/components/CmsContent.vue"
import ReportCatalog from "@/shared/reports/ReportCatalog.vue"
</script>
<template>
    <CmsContent content-name="personnel-home"></CmsContent>
    <ReportCatalog
        area="Personnel"
        report-route="PersonnelReport"
    />
</template>
