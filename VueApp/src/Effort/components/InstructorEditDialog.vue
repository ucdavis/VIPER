<template>
    <q-dialog
        :model-value="modelValue"
        persistent
        @update:model-value="emit('update:modelValue', $event)"
    >
        <q-card style="min-width: 450px; max-width: 600px">
            <q-card-section class="row items-center q-pb-none">
                <div class="text-h6">Edit Instructor</div>
                <q-space />
                <q-btn
                    v-close-popup
                    icon="close"
                    flat
                    round
                    dense
                />
            </q-card-section>

            <q-card-section>
                <!-- Read-only instructor info -->
                <div class="q-mb-md q-pa-sm bg-grey-2 rounded-borders">
                    <div class="text-subtitle2">{{ instructor?.fullName }}</div>
                    <div class="text-caption text-grey-7">Person ID: {{ instructor?.personId }}</div>
                </div>

                <!-- Editable fields -->
                <q-select
                    v-model="form.effortDept"
                    :options="groupedDepartments"
                    label="Department"
                    dense
                    options-dense
                    outlined
                    emit-value
                    map-options
                    class="q-mb-sm"
                >
                    <template #option="scope">
                        <q-item v-bind="scope.itemProps">
                            <q-item-section>
                                <q-item-label>{{ scope.opt.label }}</q-item-label>
                            </q-item-section>
                        </q-item>
                    </template>
                </q-select>

                <q-input
                    v-model="form.effortTitleCode"
                    label="Title Code"
                    dense
                    outlined
                    maxlength="6"
                    class="q-mb-sm"
                />

                <q-input
                    v-model="form.jobGroupId"
                    label="Job Group ID"
                    dense
                    outlined
                    maxlength="3"
                    class="q-mb-sm"
                />

                <q-select
                    v-model="form.reportUnits"
                    :options="reportUnitOptions"
                    label="Report Units"
                    dense
                    options-dense
                    outlined
                    multiple
                    use-chips
                    emit-value
                    map-options
                    option-label="label"
                    option-value="value"
                    class="q-mb-sm"
                />

                <q-checkbox
                    v-model="form.volunteerWos"
                    label="Volunteer / WOS"
                    class="q-mb-sm"
                >
                    <q-tooltip>Excludes instructor from M&amp;P reports</q-tooltip>
                </q-checkbox>

                <div
                    v-if="errorMessage"
                    class="text-negative q-mt-sm"
                >
                    {{ errorMessage }}
                </div>
            </q-card-section>

            <q-card-actions align="right">
                <q-btn
                    v-close-popup
                    flat
                    label="Cancel"
                />
                <q-btn
                    color="primary"
                    label="Update"
                    :loading="isSaving"
                    @click="updateInstructor"
                />
            </q-card-actions>
        </q-card>
    </q-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from "vue"
import { effortService } from "../services/effort-service"
import type { PersonDto, DepartmentDto, ReportUnitDto } from "../types"

const props = defineProps<{
    modelValue: boolean
    instructor: PersonDto | null
    termCode: number | null
}>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    updated: []
}>()

// Form state
const form = ref({
    effortDept: "",
    effortTitleCode: "",
    jobGroupId: "",
    reportUnits: [] as string[],
    volunteerWos: false,
})

const departments = ref<DepartmentDto[]>([])
const reportUnits = ref<ReportUnitDto[]>([])
const isSaving = ref(false)
const errorMessage = ref("")

// Computed
const groupedDepartments = computed(() => {
    const groups: Record<string, { label: string; value: string }[]> = {}

    for (const dept of departments.value) {
        const groupArray = groups[dept.group] ?? (groups[dept.group] = [])
        groupArray.push({
            label: `${dept.code} - ${dept.name}`,
            value: dept.code,
        })
    }

    const result: { label: string; value: string; disable?: boolean }[] = []
    for (const [groupName, items] of Object.entries(groups)) {
        result.push({ label: groupName, value: "", disable: true })
        result.push(...items)
    }

    return result
})

const reportUnitOptions = computed(() => {
    return reportUnits.value.map((u) => ({
        label: `${u.abbrev} - ${u.unit}`,
        value: u.abbrev,
    }))
})

// Load lookup data
onMounted(async () => {
    const [depts, units] = await Promise.all([effortService.getInstructorDepartments(), effortService.getReportUnits()])
    departments.value = depts
    reportUnits.value = units
})

// Reset form when dialog opens with instructor
watch(
    () => [props.modelValue, props.instructor],
    ([isOpen, instructor]) => {
        if (isOpen && instructor) {
            const inst = instructor as PersonDto
            form.value = {
                effortDept: inst.effortDept || "",
                effortTitleCode: inst.effortTitleCode || "",
                jobGroupId: inst.jobGroupId || "",
                reportUnits: inst.reportUnit ? inst.reportUnit.split(",").map((s) => s.trim()) : [],
                volunteerWos: inst.volunteerWos ?? false,
            }
            errorMessage.value = ""
        }
    },
    { immediate: true },
)

async function updateInstructor() {
    if (!props.instructor || !props.termCode) return

    isSaving.value = true
    errorMessage.value = ""

    try {
        const result = await effortService.updateInstructor(props.instructor.personId, props.termCode, {
            effortDept: form.value.effortDept,
            effortTitleCode: form.value.effortTitleCode,
            jobGroupId: form.value.jobGroupId || null,
            reportUnits: form.value.reportUnits.length > 0 ? form.value.reportUnits : null,
            volunteerWos: form.value.volunteerWos,
        })

        if (result.success) {
            emit("update:modelValue", false)
            emit("updated")
        } else {
            errorMessage.value = result.error || "Failed to update instructor"
        }
    } catch {
        errorMessage.value = "An unexpected error occurred"
    } finally {
        isSaving.value = false
    }
}
</script>
