<template>
    <section
        aria-labelledby="report-problems-check"
        class="q-mb-lg"
    >
        <h2 id="report-problems-check">Check every report</h2>
        <p>
            Checks each live report the way saving it would: its data source is still in the [report] schema, its
            columns and parameters are still in the source, and RAPS still has its permissions. It doesn't run the
            reports.
        </p>
        <q-btn
            color="primary"
            no-caps
            icon="fact_check"
            label="Check reports"
            :loading="loading"
            @click="check"
        />
        <StatusBanner
            v-if="failed"
            type="error"
            class="q-mt-md"
        >
            The reports couldn't be checked. Please try again later.
        </StatusBanner>
        <template v-else-if="result">
            <StatusBanner
                v-if="result.failing.length === 0"
                type="success"
                class="q-mt-md"
            >
                All {{ result.checked }} reports passed ({{ checkedAt }}).
            </StatusBanner>
            <template v-else>
                <StatusBanner
                    type="warning"
                    class="q-mt-md"
                >
                    {{ result.failing.length }} of {{ result.checked }} reports have problems ({{ checkedAt }}).
                </StatusBanner>
                <q-list
                    bordered
                    separator
                    dense
                    class="q-mt-md"
                >
                    <q-item
                        v-for="report in result.failing"
                        :key="report.id"
                    >
                        <q-item-section>
                            <q-item-label>
                                <router-link :to="{ name: 'ReportAdminEdit', params: { id: report.id } }">{{
                                    report.title
                                }}</router-link>
                            </q-item-label>
                            <q-item-label caption>{{
                                [report.area, report.source].filter(Boolean).join(" · ")
                            }}</q-item-label>
                            <ul class="q-my-xs q-pl-md">
                                <li
                                    v-for="problem in report.problems"
                                    :key="problem"
                                >
                                    {{ problem }}
                                </li>
                            </ul>
                        </q-item-section>
                    </q-item>
                </q-list>
            </template>
        </template>
    </section>
</template>

<script setup lang="ts">
import { computed, ref } from "vue"
import StatusBanner from "@/components/StatusBanner.vue"
import { formatCell } from "@/shared/report-builder/report-builder-format"
import { reportAdminService } from "../admin/report-admin-service"
import type { ReportCheckResult } from "../admin/report-admin-types"

/** Checks every live report on demand; it reads each source's shape, so it isn't run on load. */
const result = ref<ReportCheckResult | null>(null)
const loading = ref(false)
const failed = ref(false)

const checkedAt = computed(() => `checked ${formatCell(result.value?.checkedOn ?? null, "DateTime")}`)

async function check(): Promise<void> {
    loading.value = true
    result.value = await reportAdminService.check()
    failed.value = result.value === null
    loading.value = false
}
</script>
