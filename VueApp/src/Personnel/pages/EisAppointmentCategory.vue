<template>
    <h2>Appointment category</h2>
    <EisSectionState
        v-slot="{ data: categories }"
        :loading="loading"
        :data="data"
        what="appointment categories"
    >
        <p>
            Categories are worked out from the employee's current UCPath jobs. The categories with a checkbox are set by
            hand for academic year {{ categories.academicYear }}.
            <template v-if="categories.canEditFlags">Changes save as soon as you check or uncheck one.</template>
            <template v-else>Only EIS administrators can change them.</template>
        </p>
        <StatusBanner
            v-if="saveFailed"
            type="error"
        >
            The category could not be changed. Try again.
        </StatusBanner>
        <EisCategoryList
            :categories="categories.categories"
            :can-edit="categories.canEditFlags && !saving"
            @change="changeFlag"
        />
    </EisSectionState>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue"
import { useRoute } from "vue-router"
import StatusBanner from "@/components/StatusBanner.vue"
import { showStatusNotification } from "@/composables/status-notification"
import EisCategoryList from "../components/EisCategoryList.vue"
import EisSectionState from "../components/EisSectionState.vue"
import { useEisSection } from "../composables/use-eis-section"
import { eisService } from "../services/eis-service"

/**
 * The legacy Appointment Category page: each category, ticked when it applies. Most come from
 * the employee's jobs; the manual ones (Branch Chief, Director, Service Chief and so on) are
 * checkboxes that EIS administrators set for the current academic year.
 */
const route = useRoute()
const employeeId = computed(() => String(route.params.employeeId ?? ""))
const { data, loading, setData } = useEisSection((id) => eisService.getCategories(id))
const saving = ref(false)
const saveFailed = ref(false)

// A failed save belongs to the employee it was made for.
watch(employeeId, () => {
    saveFailed.value = false
})

async function changeFlag(code: number, value: boolean) {
    const savedFor = employeeId.value
    saving.value = true
    saveFailed.value = false
    try {
        const updated = await eisService.setFlag(savedFor, code, value)
        // The user may have moved to another employee while this saved; leave that page alone.
        if (savedFor !== employeeId.value) {
            return
        }
        if (updated === null) {
            saveFailed.value = true
        } else {
            setData(updated)
            showStatusNotification("Category updated.")
        }
    } finally {
        saving.value = false
    }
}
</script>
