<template>
    <q-page padding>
        <q-card class="no-print">
            <q-card-section>
                <div class="row items-center">
                    <div class="col">
                        <div class="text-h5">Student Photo Gallery</div>
                    </div>
                    <div class="col-auto">
                        <q-btn-group flat>
                            <q-btn
                                flat
                                icon="grid_view"
                                :color="galleryStore.galleryView === 'grid' ? 'primary' : 'grey'"
                                @click="setView('grid')"
                            >
                                <q-tooltip>Grid View</q-tooltip>
                            </q-btn>
                            <q-btn
                                flat
                                icon="list"
                                :color="galleryStore.galleryView === 'list' ? 'primary' : 'grey'"
                                @click="setView('list')"
                            >
                                <q-tooltip>List View</q-tooltip>
                            </q-btn>
                            <q-btn
                                flat
                                icon="print"
                                :color="galleryStore.galleryView === 'sheet' ? 'primary' : 'grey'"
                                @click="setView('sheet')"
                            >
                                <q-tooltip>Print Sheet</q-tooltip>
                            </q-btn>
                        </q-btn-group>
                    </div>
                </div>
            </q-card-section>

            <q-separator />

            <q-card-section>
                <div class="row q-col-gutter-md">
                    <!-- Class Level Selection -->
                    <div class="col-12 col-md-6">
                        <q-select
                            v-model="selectedClassLevel"
                            :options="classLevelOptions"
                            label="Select Class Year"
                            outlined
                            dense
                            options-dense
                            emit-value
                            map-options
                            @update:model-value="onClassLevelChange"
                        >
                            <template #prepend>
                                <q-icon name="school" />
                            </template>
                        </q-select>
                    </div>

                    <!-- Group Type Selection -->
                    <div class="col-12 col-md-6">
                        <q-select
                            v-model="selectedGroupType"
                            :options="groupTypeOptions"
                            label="Select Group Type"
                            outlined
                            dense
                            options-dense
                            emit-value
                            map-options
                            :disable="!selectedClassLevel"
                            @update:model-value="onGroupTypeChange"
                        >
                            <template #prepend>
                                <q-icon name="group" />
                            </template>
                        </q-select>
                    </div>

                    <!-- Group Selection -->
                    <div
                        v-if="selectedGroupType"
                        class="col-12"
                    >
                        <q-select
                            v-model="selectedGroup"
                            :options="groupOptions"
                            label="Select Specific Group"
                            outlined
                            dense
                            options-dense
                            emit-value
                            map-options
                            clearable
                            @update:model-value="onGroupChange"
                        >
                            <template #prepend>
                                <q-icon name="people" />
                            </template>
                        </q-select>
                    </div>

                    <!-- Ross Students Toggle -->
                    <div class="col-12">
                        <q-checkbox
                            v-model="galleryStore.includeRossStudents"
                            label="Include Ross Students"
                            @update:model-value="galleryStore.toggleRossStudents"
                        />
                    </div>
                </div>

                <!-- Export Controls -->
                <div
                    v-if="galleryStore.hasStudents && galleryStore.galleryView !== 'sheet'"
                    class="row q-mt-md q-gutter-sm"
                >
                    <q-btn
                        color="primary"
                        icon="description"
                        label="Export to Word"
                        :loading="galleryStore.exportInProgress"
                        @click="handleExportToWord"
                    />
                    <q-btn
                        color="primary"
                        icon="picture_as_pdf"
                        label="Export to PDF"
                        :loading="galleryStore.exportInProgress"
                        @click="handleExportToPDF"
                    />
                </div>

                <!-- Print Button for Sheet View -->
                <div
                    v-if="galleryStore.hasStudents && galleryStore.galleryView === 'sheet'"
                    class="row q-mt-md"
                >
                    <q-btn
                        color="primary"
                        icon="print"
                        label="Print"
                        @click="printSheet"
                    />
                </div>
            </q-card-section>
        </q-card>

        <!-- Photo Display Area -->
        <div
            v-if="galleryStore.loading"
            class="q-mt-lg text-center"
        >
            <q-spinner
                size="50px"
                color="primary"
            />
            <div class="q-mt-md">Loading photos...</div>
        </div>

        <div
            v-else-if="galleryStore.error"
            class="q-mt-lg"
        >
            <q-banner class="bg-negative text-white">
                <template #avatar>
                    <q-icon name="warning" />
                </template>
                {{ galleryStore.error }}
            </q-banner>
        </div>

        <div
            v-else-if="galleryStore.hasStudents"
            class="q-mt-lg"
        >
            <q-card>
                <q-card-section>
                    <div
                        v-if="galleryStore.galleryView !== 'sheet'"
                        class="row items-center q-mb-md no-print"
                    >
                        <div class="col">
                            <div class="text-subtitle1">{{ pageTitle }}</div>
                        </div>
                    </div>

                    <!-- Photo Grid Component -->
                    <PhotoGrid
                        v-if="galleryStore.galleryView === 'grid'"
                        :students="galleryStore.students"
                    />

                    <!-- Photo List Component -->
                    <PhotoList
                        v-else-if="galleryStore.galleryView === 'list'"
                        :students="galleryStore.students"
                    />

                    <!-- Photo Sheet Component -->
                    <PhotoSheet
                        v-else-if="galleryStore.galleryView === 'sheet'"
                        :students="galleryStore.students"
                        :title="pageTitle"
                    />
                </q-card-section>
            </q-card>
        </div>

        <div
            v-else
            class="q-mt-lg text-center text-grey"
        >
            <q-icon
                name="photo_library"
                size="100px"
                color="grey-5"
            />
            <div class="q-mt-md text-h6">No photos to display</div>
            <div class="text-subtitle2">
                {{
                    hasActiveFilters
                        ? "No students found for the selected filters"
                        : "Select a class year or group to view student photos"
                }}
            </div>
        </div>
    </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch, nextTick } from "vue"
import { useQuasar } from "quasar"
import { useRoute, useRouter } from "vue-router"
import { usePhotoGalleryStore } from "../stores/photo-gallery-store"
import PhotoGrid from "../components/PhotoGallery/PhotoGrid.vue"
import PhotoList from "../components/PhotoGallery/PhotoList.vue"
import PhotoSheet from "../components/PhotoGallery/PhotoSheet.vue"

const $q = useQuasar()
const route = useRoute()
const router = useRouter()
const galleryStore = usePhotoGalleryStore()

const selectedClassLevel = ref<string | null>(null)
const selectedGroupType = ref<string | null>(null)
const selectedGroup = ref<string | null>(null)

const classYears = ref<Array<{ year: number; classLevel: string }>>([])

const classLevelOptions = computed(() => [
    { label: "Select Class Year", value: null },
    ...classYears.value.map((cy) => ({
        label: `${cy.year} (${cy.classLevel})`,
        value: cy.classLevel,
    })),
])

const groupTypeOptions = computed(() => [
    { label: "All Students", value: null },
    { label: "Eighths", value: "eighths" },
    { label: "Twentieths", value: "twentieths" },
    ...(selectedClassLevel.value === "V3"
        ? [
              { label: "Teams", value: "teams" },
              { label: "Streams", value: "v3specialty" },
          ]
        : []),
])

const groupOptions = computed(() => {
    if (!selectedGroupType.value) return []

    let groups: string[] = []
    let countKey: "eighths" | "twentieths" | "teams" | "v3specialty" = "eighths"

    if (selectedGroupType.value === "eighths") {
        groups = galleryStore.groupTypes.eighths
        countKey = "eighths"
    } else if (selectedGroupType.value === "twentieths") {
        groups = galleryStore.groupTypes.twentieths
        countKey = "twentieths"
    } else if (selectedGroupType.value === "teams") {
        groups = galleryStore.groupTypes.teams
        countKey = "teams"
    } else if (selectedGroupType.value === "v3specialty") {
        groups = galleryStore.groupTypes.v3specialty
        countKey = "v3specialty"
    }

    // Use cached counts from the store (populated when class is loaded)
    const counts = galleryStore.groupCounts[countKey] || {}

    return groups.map((group) => {
        const count = counts[group] || 0
        return {
            label: `${group} (${count})`,
            value: group,
        }
    })
})

const hasActiveFilters = computed(() => {
    return !!selectedClassLevel.value || !!selectedGroupType.value || !!selectedGroup.value
})

const selectedClassYearDisplay = computed(() => {
    if (!selectedClassLevel.value) return ""
    const classYear = classYears.value.find((cy) => cy.classLevel === selectedClassLevel.value)
    return classYear ? `${classYear.year} (${classYear.classLevel})` : selectedClassLevel.value
})

const groupTypeLabel = computed(() => {
    if (!selectedGroupType.value) return ""
    const labels: Record<string, string> = {
        eighths: "Eighths",
        twentieths: "Twentieths",
        teams: "Team",
        v3specialty: "Stream",
    }
    return labels[selectedGroupType.value] || selectedGroupType.value
})

const pageTitle = computed(() => {
    const parts: string[] = []

    if (selectedClassYearDisplay.value) {
        parts.push(`Class of ${selectedClassYearDisplay.value}`)
    }

    if (selectedGroup.value && groupTypeLabel.value) {
        parts.push(`${groupTypeLabel.value} ${selectedGroup.value}`)
    }

    let title = parts.length > 0 ? parts.join(" - ") : "Student Photos"

    if (parts.length > 0 && galleryStore.studentCount > 0) {
        title += ` (${galleryStore.studentCount} Students)`
    } else if (galleryStore.studentCount > 0) {
        title = `${galleryStore.studentCount} Students`
    }

    if (galleryStore.includeRossStudents) {
        title = title.replace(" Students)", " Students including Ross)")
    }

    return title
})

async function onClassLevelChange(classLevel: string | null) {
    selectedGroupType.value = null
    selectedGroup.value = null
    updateUrlParams()

    if (classLevel) {
        await galleryStore.fetchClassPhotos(classLevel)
    } else {
        galleryStore.clearSelection()
    }
}

function onGroupTypeChange(groupType: string | null) {
    selectedGroup.value = null
    updateUrlParams()

    if (!groupType && selectedClassLevel.value) {
        galleryStore.fetchClassPhotos(selectedClassLevel.value)
    }
}

async function onGroupChange(group: string | null) {
    updateUrlParams()

    if (group && selectedGroupType.value) {
        await galleryStore.fetchGroupPhotos(selectedGroupType.value, group, selectedClassLevel.value)
    } else if (selectedClassLevel.value) {
        await galleryStore.fetchClassPhotos(selectedClassLevel.value)
    }
}

function updateUrlParams() {
    const query: Record<string, string> = {}
    if (selectedClassLevel.value) query.classLevel = selectedClassLevel.value
    if (selectedGroupType.value) query.groupType = selectedGroupType.value
    if (selectedGroup.value) query.group = selectedGroup.value
    if (galleryStore.includeRossStudents) query.includeRoss = "true"
    if (galleryStore.galleryView !== "grid") query.view = galleryStore.galleryView

    router.replace({ query })
}

function loadFromUrlParams() {
    const { classLevel, groupType, group, includeRoss, view } = route.query

    if (includeRoss === "true") {
        galleryStore.includeRossStudents = true
    }

    if (typeof view === "string" && (view === "grid" || view === "list" || view === "sheet")) {
        galleryStore.galleryView = view
    }

    if (typeof classLevel === "string") {
        selectedClassLevel.value = classLevel
    }
    if (typeof groupType === "string") {
        selectedGroupType.value = groupType
    }
    if (typeof group === "string") {
        selectedGroup.value = group
    }
}

async function fetchClassYears() {
    try {
        const response = await fetch("/api/students/photos/metadata/classyears")
        if (response.ok) {
            classYears.value = await response.json()
        }
    } catch {
        $q.notify({
            type: "negative",
            message: "Failed to load class years",
        })
    }
}

function setView(view: "grid" | "list" | "sheet") {
    galleryStore.setGalleryView(view)
    updateUrlParams()
}

async function handleExportToWord() {
    try {
        await galleryStore.exportToWord()
        $q.notify({
            type: "positive",
            message: "Export to Word completed successfully",
        })
    } catch (error: any) {
        $q.notify({
            type: "negative",
            message: error.message || "Failed to export to Word",
        })
    }
}

async function handleExportToPDF() {
    try {
        await galleryStore.exportToPDF()
        $q.notify({
            type: "positive",
            message: "Export to PDF completed successfully",
        })
    } catch (error: any) {
        $q.notify({
            type: "negative",
            message: error.message || "Failed to export to PDF",
        })
    }
}

function printSheet() {
    window.print()
}

onMounted(async () => {
    await galleryStore.fetchGalleryMenu()
    await fetchClassYears()

    loadFromUrlParams()

    if (selectedClassLevel.value) {
        // Always fetch class photos first to populate the group counts cache
        await galleryStore.fetchClassPhotos(selectedClassLevel.value)

        // Then fetch specific group if one is selected
        if (selectedGroup.value && selectedGroupType.value) {
            await galleryStore.fetchGroupPhotos(selectedGroupType.value, selectedGroup.value, selectedClassLevel.value)
        }
    }
})

watch(
    () => galleryStore.includeRossStudents,
    () => {
        updateUrlParams()
    },
)

watch(
    () => galleryStore.galleryView,
    (newView, oldView) => {
        // Auto-trigger print dialog when switching to sheet view
        if (newView === "sheet" && oldView !== "sheet" && galleryStore.hasStudents) {
            // Use nextTick + setTimeout to ensure images are loaded before printing
            nextTick(() => {
                setTimeout(() => {
                    window.print()
                }, 1000)
            })
        }
    },
)
</script>

<style>
@media print {
    @page {
        size: landscape;
    }

    /* Hide all navigation and UI elements */
    .no-print {
        display: none !important;
        height: 0 !important;
        width: 0 !important;
        margin: 0 !important;
        padding: 0 !important;
        overflow: hidden !important;
    }

    /* Collapse the Quasar layout structure */
    .q-header,
    .q-drawer,
    .q-footer {
        display: none !important;
        height: 0 !important;
    }

    /* Completely remove drawer space */
    .q-drawer-container {
        display: none !important;
        width: 0 !important;
        min-width: 0 !important;
    }

    /* Reset layout and page to use full width */
    .q-layout,
    .q-page-container,
    .q-page {
        margin: 0 !important;
        padding: 0 !important;
        padding-left: 0 !important;
        padding-right: 0 !important;
        max-width: 100% !important;
        width: 100% !important;
        min-height: auto !important;
    }

    /* Remove card styling for cleaner print */
    .q-card {
        box-shadow: none !important;
        border: none !important;
        margin: 0 !important;
        padding: 0 !important;
    }

    /* Ensure photo sheet takes full width */
    .photo-sheet {
        width: 100% !important;
        margin: 0 !important;
        padding: 10px !important;
    }

    /* Force images to display */
    img {
        display: block !important;
        visibility: visible !important;
        opacity: 1 !important;
        -webkit-print-color-adjust: exact !important;
        print-color-adjust: exact !important;
    }

    /* Ensure table and cells are visible */
    table,
    tr,
    td {
        display: table !important;
        visibility: visible !important;
    }

    /* Remove any transforms or transitions that might interfere */
    * {
        transform: none !important;
        transition: none !important;
        animation: none !important;
    }
}
</style>
