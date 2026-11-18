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
    <q-form
        v-else
        class="compact-form"
        @submit="save"
    >
        <ReportAdminStatus
            :definition="definition"
            @delete="setDeleted(true)"
            @restore="setDeleted(false)"
        />
        <ReportAdminBasics
            v-model="form"
            :sources="sources"
            :sources-failed="sourcesFailed"
            :columns-error="source?.columnsError ?? null"
            @source="changeSource"
        />
        <ReportAdminColumns
            v-model="form.columns"
            :source-columns="sourceColumns"
            :can-add-missing="canAddMissing"
            @add-missing="addMissingColumns"
        />
        <ReportAdminParameters
            v-model="form.parameters"
            :targets="targets"
            :is-view="isView"
            :views="views"
        />
        <ReportAdminDisplay
            v-model="form"
            :source-columns="sourceColumns"
        />
        <ReportAdminPermissions
            v-model="form.permissions"
            :missing="definition?.missingPermissions ?? []"
        />
        <StatusBanner
            v-if="errors.length > 0"
            type="error"
        >
            <ul class="q-my-none q-pl-md">
                <li
                    v-for="error in errors"
                    :key="error"
                >
                    {{ error }}
                </li>
            </ul>
            <template
                v-if="conflict"
                #action
            >
                <q-btn
                    flat
                    no-caps
                    label="Reload the saved report"
                    @click="reload"
                />
            </template>
        </StatusBanner>
        <div class="q-mb-lg">
            <q-btn
                type="submit"
                color="primary"
                no-caps
                :label="definition === null ? 'Create report' : 'Save'"
                :loading="saving"
                :disable="Boolean(definition?.deletedOn)"
            >
                <template #loading>
                    <q-spinner
                        size="1em"
                        class="q-mr-sm"
                    />
                    Saving
                </template>
            </q-btn>
            <span
                v-if="isDirty"
                class="q-ml-md text-grey"
            >
                Unsaved changes
            </span>
        </div>
        <ReportAdminPreview :report="request" />
        <ReportAdminHistory
            v-if="definition !== null"
            :report-id="definition.id"
            @load="applyVersion"
        />
    </q-form>
</template>

<script setup lang="ts">
import { computed } from "vue"
import { useRoute } from "vue-router"
import BreadcrumbHeading from "@/components/BreadcrumbHeading.vue"
import StatusBanner from "@/components/StatusBanner.vue"
import { useConfirmLeave } from "@/composables/use-confirm-leave"
import ReportAdminBasics from "../components/ReportAdminBasics.vue"
import ReportAdminColumns from "../components/ReportAdminColumns.vue"
import ReportAdminDisplay from "../components/ReportAdminDisplay.vue"
import ReportAdminHistory from "../components/ReportAdminHistory.vue"
import ReportAdminParameters from "../components/ReportAdminParameters.vue"
import ReportAdminPermissions from "../components/ReportAdminPermissions.vue"
import ReportAdminPreview from "../components/ReportAdminPreview.vue"
import ReportAdminStatus from "../components/ReportAdminStatus.vue"
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
