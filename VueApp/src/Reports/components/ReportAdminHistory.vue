<template>
    <section
        aria-labelledby="report-admin-history"
        class="q-mb-lg"
    >
        <h2 id="report-admin-history">History</h2>
        <q-btn
            v-if="versions === null"
            flat
            no-caps
            color="primary"
            icon="history"
            label="Show earlier versions"
            :loading="loading"
            @click="load"
        />
        <StatusBanner
            v-else-if="failed"
            type="error"
        >
            The history couldn't be loaded.
        </StatusBanner>
        <q-list
            v-else
            bordered
            separator
            dense
        >
            <q-item
                v-for="version in versions"
                :key="version.id"
            >
                <q-item-section>
                    <q-item-label>{{ version.action ?? "Saved" }}</q-item-label>
                    <q-item-label caption>{{ describe(version) }}</q-item-label>
                </q-item-section>
                <q-item-section side>
                    <q-btn
                        flat
                        dense
                        no-caps
                        color="primary"
                        label="Load into form"
                        :aria-label="`Load the version ${describe(version)} into the form`"
                        @click="loadVersion(version.id)"
                    />
                </q-item-section>
            </q-item>
        </q-list>
        <StatusBanner
            v-if="unreadable"
            type="warning"
            class="q-mt-md"
        >
            That version can't be read.
        </StatusBanner>
    </section>
</template>

<script setup lang="ts">
import { ref } from "vue"
import StatusBanner from "@/components/StatusBanner.vue"
import { formatCell } from "@/shared/report-builder/report-builder-format"
import { reportAdminService } from "../admin/report-admin-service"
import type { ReportEditRequest, ReportHistoryItem } from "../admin/report-admin-types"

/**
 * The report's saved versions. Loading one puts its fields in the form, where it can be checked
 * and saved as the current version.
 */
const { reportId } = defineProps<{ reportId: number }>()

const emit = defineEmits<{ (e: "load", report: ReportEditRequest): void }>()

const versions = ref<ReportHistoryItem[] | null>(null)
const loading = ref(false)
const failed = ref(false)
const unreadable = ref(false)

function describe(version: ReportHistoryItem): string {
    const when = formatCell(version.changedOn, "DateTime")
    return version.changedBy ? `${when} by ${version.changedBy}` : when
}

async function load(): Promise<void> {
    loading.value = true
    const found = await reportAdminService.getHistory(reportId)
    loading.value = false
    failed.value = found === null
    versions.value = found ?? []
}

async function loadVersion(historyId: number): Promise<void> {
    const version = await reportAdminService.getVersion(reportId, historyId)
    unreadable.value = !version?.report
    if (version?.report) {
        emit("load", version.report)
    }
}
</script>
