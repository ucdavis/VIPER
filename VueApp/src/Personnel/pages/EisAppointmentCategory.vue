<template>
    <h2>Appointment category</h2>
    <div
        v-if="loading"
        role="status"
    >
        <q-spinner-dots
            size="2rem"
            color="primary"
            aria-hidden="true"
        />
        <span class="sr-only">Loading appointment categories</span>
    </div>
    <StatusBanner
        v-else-if="failed || data === null"
        type="error"
    >
        The appointment categories could not be loaded.
    </StatusBanner>
    <template v-else>
        <p>
            Categories are worked out from the employee's current UCPath jobs. The categories with a checkbox are set by
            hand for academic year {{ data.academicYear }}.
            <template v-if="!data.canEditFlags">Only EIS administrators can change them.</template>
        </p>
        <StatusBanner
            v-if="saveFailed"
            type="error"
        >
            The category could not be changed. Try again.
        </StatusBanner>
        <ul class="eis-categories">
            <li
                v-for="category in data.categories"
                :key="category.label"
                class="eis-category"
            >
                <q-checkbox
                    v-if="isManual(category)"
                    :model-value="category.applies"
                    :label="category.label"
                    :disable="!data.canEditFlags || saving"
                    dense
                    @update:model-value="(value: boolean) => changeFlag(category.flagCode, value)"
                />
                <template v-else>
                    <q-icon
                        :name="category.applies ? 'check_box' : 'check_box_outline_blank'"
                        :color="category.applies ? 'positive' : 'grey-5'"
                        size="sm"
                        aria-hidden="true"
                    />
                    <span :class="{ 'text-weight-medium': category.applies }">{{ category.label }}</span>
                    <span class="sr-only">{{ category.applies ? "(applies)" : "(does not apply)" }}</span>
                </template>
            </li>
        </ul>
    </template>
</template>

<script setup lang="ts">
import { computed, ref } from "vue"
import { useRoute } from "vue-router"
import StatusBanner from "@/components/StatusBanner.vue"
import { useEisSection } from "../composables/use-eis-section"
import { eisService } from "../services/eis-service"
import type { EisCategory } from "../types/eis-types"

/**
 * The legacy Appointment Category page: each category, ticked when it applies. Most come from
 * the employee's jobs; the manual ones (Branch Chief, Director, Service Chief and so on) are
 * checkboxes that EIS administrators set for the current academic year.
 */
const route = useRoute()
const employeeId = computed(() => String(route.params.employeeId ?? ""))
const { data, loading, failed } = useEisSection((id) => eisService.getCategories(id))
const saving = ref(false)
const saveFailed = ref(false)

/** A category EIS administrators set by hand, which has a dvtFlags code. */
type ManualCategory = EisCategory & { flagCode: number }

function isManual(category: EisCategory): category is ManualCategory {
    return category.flagCode !== null
}

async function changeFlag(code: number, value: boolean) {
    saving.value = true
    saveFailed.value = false
    const updated = await eisService.setFlag(employeeId.value, code, value)
    if (updated === null) {
        saveFailed.value = true
    } else {
        data.value = updated
    }
    saving.value = false
}
</script>

<style scoped>
.eis-categories {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(18rem, 1fr));
    gap: 0.25rem 1.5rem;
    list-style: none;
    padding: 0;
}

.eis-category {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    min-height: 1.75rem;
}
</style>
