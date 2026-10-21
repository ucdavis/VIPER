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
            <template v-if="!categories.canEditFlags">Only EIS administrators can change them.</template>
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
import { computed, ref } from "vue"
import { useRoute } from "vue-router"
import StatusBanner from "@/components/StatusBanner.vue"
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
const { data, loading } = useEisSection((id) => eisService.getCategories(id))
const saving = ref(false)
const saveFailed = ref(false)

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
