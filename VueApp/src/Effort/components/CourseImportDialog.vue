<template>
    <q-dialog
        v-model="dialogOpen"
        persistent
        maximized-on-mobile
    >
        <q-card style="min-width: 600px; max-width: 900px">
            <q-card-section class="row items-center q-pb-none">
                <div class="text-h6">Import Course from Banner</div>
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
                <!-- Search Form -->
                <div class="row q-col-gutter-sm items-end q-mb-md">
                    <div class="col">
                        <q-input
                            v-model="searchSubjCode"
                            label="Subject Code"
                            dense
                            outlined
                            @keyup.enter="searchCourses"
                        />
                    </div>
                    <div class="col">
                        <q-input
                            v-model="searchCrseNumb"
                            label="Course Number"
                            dense
                            outlined
                            @keyup.enter="searchCourses"
                        />
                    </div>
                    <div class="col">
                        <q-input
                            v-model="searchCrn"
                            label="CRN"
                            dense
                            outlined
                            @keyup.enter="searchCourses"
                        />
                    </div>
                    <div class="col-auto">
                        <q-btn
                            label="Search"
                            color="primary"
                            :loading="isSearching"
                            :disable="!canSearch"
                            @click="searchCourses"
                        />
                    </div>
                </div>

                <div
                    v-if="searchError"
                    class="text-negative q-mb-md"
                >
                    {{ searchError }}
                </div>

                <!-- Search Results -->
                <q-table
                    v-if="searchResults.length > 0"
                    :rows="searchResults"
                    :columns="columns"
                    row-key="crn"
                    dense
                    flat
                    bordered
                    :pagination="{ rowsPerPage: 10 }"
                >
                    <template #body-cell-courseCode="slotProps">
                        <q-td :props="slotProps">
                            <span class="text-weight-medium">{{ slotProps.row.courseCode }}</span>
                            <span class="text-grey-7 q-ml-xs">-{{ slotProps.row.seqNumb }}</span>
                        </q-td>
                    </template>
                    <template #body-cell-units="slotProps">
                        <q-td :props="slotProps">
                            <template v-if="slotProps.row.isVariableUnits">
                                {{ slotProps.row.unitLow }} - {{ slotProps.row.unitHigh }}
                                <q-badge
                                    color="info"
                                    class="q-ml-xs"
                                    >Variable</q-badge
                                >
                            </template>
                            <template v-else>
                                {{ slotProps.row.unitLow }}
                            </template>
                        </q-td>
                    </template>
                    <template #body-cell-status="slotProps">
                        <q-td :props="slotProps">
                            <template v-if="slotProps.row.alreadyImported">
                                <q-badge color="grey">Already Imported</q-badge>
                            </template>
                            <template v-else-if="slotProps.row.importedUnitValues.length > 0">
                                <q-badge color="orange"
                                    >Imported: {{ slotProps.row.importedUnitValues.join(", ") }} units</q-badge
                                >
                            </template>
                            <template v-else>
                                <q-badge color="positive">Available</q-badge>
                            </template>
                        </q-td>
                    </template>
                    <template #body-cell-actions="slotProps">
                        <q-td :props="slotProps">
                            <q-btn
                                v-if="!slotProps.row.alreadyImported"
                                label="Import"
                                color="primary"
                                dense
                                size="sm"
                                :loading="importingCrn === slotProps.row.crn"
                                @click="startImport(slotProps.row)"
                            />
                        </q-td>
                    </template>
                </q-table>

                <div
                    v-else-if="hasSearched && !isSearching"
                    class="text-grey text-center q-py-lg"
                >
                    No courses found matching your search criteria.
                </div>
            </q-card-section>
        </q-card>
    </q-dialog>

    <!-- Import Options Dialog -->
    <q-dialog
        v-model="showImportOptionsDialog"
        persistent
    >
        <q-card style="min-width: 350px">
            <q-card-section>
                <div class="text-h6">Import Options</div>
                <div class="text-subtitle2 text-grey q-mt-sm">
                    {{ selectedBannerCourse?.courseCode }}-{{ selectedBannerCourse?.seqNumb }}
                </div>
            </q-card-section>

            <q-card-section>
                <!-- Variable units input -->
                <template v-if="selectedBannerCourse?.isVariableUnits">
                    <p class="text-body2">
                        This is a variable-unit course. Please enter the units for this import ({{
                            selectedBannerCourse?.unitLow
                        }}
                        - {{ selectedBannerCourse?.unitHigh }}).
                    </p>
                    <q-input
                        v-model.number="importUnits"
                        type="number"
                        label="Units"
                        dense
                        outlined
                        class="q-mb-md"
                        :min="selectedBannerCourse?.unitLow"
                        :max="selectedBannerCourse?.unitHigh"
                        step="0.5"
                        :rules="[
                            (v: number) =>
                                (v >= (selectedBannerCourse?.unitLow ?? 0) &&
                                    v <= (selectedBannerCourse?.unitHigh ?? 99)) ||
                                `Units must be between ${selectedBannerCourse?.unitLow} and ${selectedBannerCourse?.unitHigh}`,
                        ]"
                    />
                    <div
                        v-if="selectedBannerCourse?.importedUnitValues.length"
                        class="text-caption text-orange q-mb-md"
                    >
                        Already imported with: {{ selectedBannerCourse.importedUnitValues.join(", ") }} units
                    </div>
                </template>

                <!-- Import error display -->
                <q-banner
                    v-if="importError"
                    class="bg-negative text-white q-mt-md"
                    dense
                >
                    <template #avatar>
                        <q-icon name="error" />
                    </template>
                    {{ importError }}
                </q-banner>
            </q-card-section>

            <q-card-actions align="right">
                <q-btn
                    v-close-popup
                    label="Cancel"
                    flat
                />
                <q-btn
                    label="Import"
                    color="primary"
                    :loading="isImporting"
                    :disable="!canImport"
                    @click="doImport"
                />
            </q-card-actions>
        </q-card>
    </q-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue"
import { useQuasar } from "quasar"
import { effortService } from "../services/effort-service"
import type { BannerCourseDto } from "../types"
import type { QTableColumn } from "quasar"

const props = defineProps<{
    modelValue: boolean
    termCode: number | null
}>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    imported: []
}>()

const $q = useQuasar()

// Dialog state
const dialogOpen = computed({
    get: () => props.modelValue,
    set: (value) => emit("update:modelValue", value),
})

// Search state
const searchSubjCode = ref("")
const searchCrseNumb = ref("")
const searchCrn = ref("")
const searchResults = ref<BannerCourseDto[]>([])
const isSearching = ref(false)
const hasSearched = ref(false)
const searchError = ref("")

// Import state
const showImportOptionsDialog = ref(false)
const selectedBannerCourse = ref<BannerCourseDto | null>(null)
const importUnits = ref<number>(0)
const isImporting = ref(false)
const importingCrn = ref<string | null>(null)
const importError = ref<string | null>(null)

const canSearch = computed(
    () => props.termCode && (searchSubjCode.value.trim() || searchCrseNumb.value.trim() || searchCrn.value.trim()),
)

const canImport = computed(() => {
    if (!selectedBannerCourse.value) return false

    // For variable-unit courses, validate units
    if (selectedBannerCourse.value.isVariableUnits) {
        if (
            importUnits.value < selectedBannerCourse.value.unitLow ||
            importUnits.value > selectedBannerCourse.value.unitHigh
        ) {
            return false
        }
    }

    return true
})

const columns: QTableColumn[] = [
    { name: "courseCode", label: "Course", field: "courseCode", align: "left" },
    { name: "crn", label: "CRN", field: "crn", align: "left" },
    { name: "title", label: "Title", field: "title", align: "left" },
    { name: "enrollment", label: "Enrollment", field: "enrollment", align: "right" },
    { name: "units", label: "Units", field: "unitLow", align: "right" },
    { name: "status", label: "Status", field: "alreadyImported", align: "center" },
    { name: "actions", label: "", field: "actions", align: "center" },
]

// Reset search when dialog opens
watch(dialogOpen, (open) => {
    if (open) {
        searchSubjCode.value = ""
        searchCrseNumb.value = ""
        searchCrn.value = ""
        searchResults.value = []
        hasSearched.value = false
        searchError.value = ""
    }
})

async function searchCourses() {
    if (!canSearch.value || !props.termCode) return

    isSearching.value = true
    searchError.value = ""
    hasSearched.value = true

    try {
        searchResults.value = await effortService.searchBannerCourses(props.termCode, {
            subjCode: searchSubjCode.value.trim() || undefined,
            crseNumb: searchCrseNumb.value.trim() || undefined,
            crn: searchCrn.value.trim() || undefined,
        })
    } catch (err) {
        searchError.value = err instanceof Error ? err.message : "Error searching for courses"
        searchResults.value = []
    } finally {
        isSearching.value = false
    }
}

function startImport(course: BannerCourseDto) {
    selectedBannerCourse.value = course
    importUnits.value = course.unitLow
    importError.value = null
    showImportOptionsDialog.value = true
}

async function doImport() {
    if (!selectedBannerCourse.value || !props.termCode) return

    isImporting.value = true
    importingCrn.value = selectedBannerCourse.value.crn
    importError.value = null

    try {
        const request = {
            termCode: props.termCode,
            crn: selectedBannerCourse.value.crn,
            units: selectedBannerCourse.value.isVariableUnits ? importUnits.value : undefined,
        }

        const result = await effortService.importCourse(request)

        if (result.success) {
            showImportOptionsDialog.value = false
            dialogOpen.value = false
            emit("imported")
        } else {
            importError.value = result.error ?? "Failed to import course"
            $q.notify({ type: "negative", message: importError.value })
        }
    } catch (err) {
        importError.value = err instanceof Error ? err.message : "Failed to import course"
        $q.notify({ type: "negative", message: importError.value })
    } finally {
        isImporting.value = false
        importingCrn.value = null
    }
}
</script>
