<template>
    <BreadcrumbHeading
        :label="heading"
        parent-label="Manage reports"
        :parent-to="{ name: 'ReportAdmin' }"
    />
    <div
        v-if="loading"
        role="status"
    >
        <q-spinner-dots
            size="2rem"
            color="primary"
            aria-hidden="true"
        />
        <span class="sr-only">Loading report</span>
    </div>
    <StatusBanner
        v-else-if="notFound"
        type="warning"
    >
        There's no report with that number.
    </StatusBanner>
    <ReportAdminEditForm
        v-else
        v-model="form"
        :definition="definition"
        :sources="sources"
        :sources-failed="sourcesFailed"
        :columns-error="source?.columnsError ?? null"
        :source-columns="sourceColumns"
        :can-add-missing="canAddMissing"
        :targets="targets"
        :is-view="isView"
        :views="views"
        :errors="errors"
        :conflict="conflict"
        :saving="saving"
        :is-dirty="isDirty"
        :request="request"
        :change-source="changeSource"
        :add-missing-columns="addMissingColumns"
        :save="save"
        :set-deleted="setDeleted"
        :reload="reload"
        :apply-version="applyVersion"
    />
</template>

<script setup lang="ts">
import { computed } from "vue"
import { useRoute } from "vue-router"
import BreadcrumbHeading from "@/components/BreadcrumbHeading.vue"
import StatusBanner from "@/components/StatusBanner.vue"
import { useConfirmLeave } from "@/composables/use-confirm-leave"
import ReportAdminEditForm from "../components/ReportAdminEditForm.vue"
import { toRequest } from "../admin/report-admin-form"
import { useReportAdminEdit } from "../admin/use-report-admin-edit"

/** Creates a report, or edits, previews, deletes and restores one. */
const route = useRoute()

const reportId = computed(() => (route.params.id === undefined ? null : Number(route.params.id)))

const {
    form,
    definition,
    sources,
    source,
    loading,
    notFound,
    sourcesFailed,
    saving,
    errors,
    conflict,
    isDirty,
    isView,
    views,
    targets,
    sourceColumns,
    canAddMissing,
    changeSource,
    addMissingColumns,
    save,
    setDeleted,
    applyVersion,
    reload,
} = useReportAdminEdit(reportId)

useConfirmLeave(isDirty)

const heading = computed(() => (definition.value === null ? "New report" : definition.value.report.title))
const request = computed(() => toRequest(form.value))
</script>
