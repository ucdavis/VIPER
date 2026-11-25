<template>
    <section
        aria-labelledby="report-admin-permissions"
        class="q-mb-lg"
    >
        <h2 id="report-admin-permissions">Who can view it</h2>
        <p class="text-grey">
            Anyone with one of these RAPS permissions can view the report. With none, only Report Builder admins can.
        </p>
        <q-select
            v-model="permissions"
            label="RAPS permissions"
            :options="options"
            option-value="permission"
            option-label="permission"
            emit-value
            map-options
            multiple
            use-chips
            use-input
            input-debounce="300"
            outlined
            dense
            options-dense
            @filter="search"
        >
            <template #option="scope">
                <q-item v-bind="scope.itemProps">
                    <q-item-section>
                        <q-item-label>{{ scope.opt.permission }}</q-item-label>
                        <q-item-label
                            v-if="scope.opt.description"
                            caption
                        >
                            {{ scope.opt.description }}
                        </q-item-label>
                    </q-item-section>
                </q-item>
            </template>
            <template #no-option>
                <q-item>
                    <q-item-section class="text-grey">No matching RAPS permissions</q-item-section>
                </q-item>
            </template>
        </q-select>
        <StatusBanner
            v-if="stillMissing.length > 0"
            type="warning"
            class="q-mt-md"
        >
            RAPS no longer has {{ stillMissing.join(", ") }}, so nobody holds
            {{ stillMissing.length === 1 ? "it" : "them" }}. Remove
            {{ stillMissing.length === 1 ? "it" : "them" }} before saving.
        </StatusBanner>
    </section>
</template>

<script setup lang="ts">
import { computed, ref } from "vue"
import StatusBanner from "@/components/StatusBanner.vue"
import { reportAdminService } from "../admin/report-admin-service"
import type { ReportPermissionOption } from "../admin/report-admin-types"

/** The RAPS permissions that let someone view the report, picked from RAPS. */
const { missing = [] } = defineProps<{
    /** Saved permissions RAPS no longer has. */
    missing?: string[]
}>()

const permissions = defineModel<string[]>({ required: true })

const options = ref<ReportPermissionOption[]>([])

const stillMissing = computed(() => missing.filter((name) => permissions.value.includes(name)))

/** Asks RAPS for permissions matching the typed text. */
async function search(text: string, update: (callback: () => void) => void): Promise<void> {
    const found = await reportAdminService.searchPermissions(text)
    update(() => {
        options.value = found
    })
}
</script>
