<template>
    <q-form
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
            :columns-error="columnsError"
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
import StatusBanner from "@/components/StatusBanner.vue"
import ReportAdminBasics from "./ReportAdminBasics.vue"
import ReportAdminColumns from "./ReportAdminColumns.vue"
import ReportAdminDisplay from "./ReportAdminDisplay.vue"
import ReportAdminHistory from "./ReportAdminHistory.vue"
import ReportAdminParameters from "./ReportAdminParameters.vue"
import ReportAdminPermissions from "./ReportAdminPermissions.vue"
import ReportAdminPreview from "./ReportAdminPreview.vue"
import ReportAdminStatus from "./ReportAdminStatus.vue"
import type { ReportAdminForm } from "../admin/report-admin-form"
import type { ReportAdminDefinition, ReportEditRequest, ReportSourceSummary } from "../admin/report-admin-types"

/**
 * The report edit form: basics, columns, parameters, display, permissions, save and history.
 * Split out of ReportAdminEdit so each template stays under the complexity threshold.
 */
defineProps<{
    definition: ReportAdminDefinition | null
    sources: ReportSourceSummary[]
    sourcesFailed: boolean
    columnsError: string | null
    sourceColumns: string[]
    canAddMissing: boolean
    targets: string[]
    isView: boolean
    views: string[]
    errors: string[]
    conflict: boolean
    saving: boolean
    isDirty: boolean
    request: ReportEditRequest
    changeSource: (name: string) => Promise<void>
    addMissingColumns: () => void
    save: () => Promise<void>
    setDeleted: (deleted: boolean) => Promise<void>
    reload: () => Promise<void>
    applyVersion: (report: ReportEditRequest) => Promise<void>
}>()

const form = defineModel<ReportAdminForm>({ required: true })
</script>
