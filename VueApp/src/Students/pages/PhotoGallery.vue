<template>
    <q-page padding>
        <q-card>
            <q-tabs
                class="no-print"
                v-model="activeTab"
                dense
                active-color="primary"
                indicator-color="primary"
                align="left"
                @update:model-value="onTabChange"
            >
                <q-tab
                    name="photos"
                    label="Photo Gallery"
                    icon="photo_library"
                />
                <q-tab
                    name="list"
                    label="Student List"
                    icon="people"
                />
            </q-tabs>

            <q-separator class="no-print" />

            <q-card-section class="no-print">
                <div class="row items-center">
                    <div class="col">
                        <div class="text-h5">{{ pageMainTitle }}</div>
                    </div>
                    <div
                        v-if="activeTab === 'photos'"
                        class="col-auto"
                    >
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
                    <div
                        v-if="activeTab === 'list' && studentListData.length > 0"
                        class="col-auto"
                    >
                        <q-btn
                            flat
                            icon="print"
                            color="primary"
                            @click="handlePrint"
                        >
                            <q-tooltip>Print Student List</q-tooltip>
                        </q-btn>
                    </div>
                </div>
            </q-card-section>

            <q-separator class="no-print" />

            <q-tab-panels
                v-model="activeTab"
                animated
            >
                <!-- Photo Gallery Tab -->
                <q-tab-panel name="photos">
                    <div class="row q-col-gutter-md no-print">
                        <!-- Class Level or Course Selection -->
                        <div class="col-12 col-md-6">
                            <q-select
                                v-model="selectedClassOrCourse"
                                :options="classLevelOptions"
                                label="Select Class Year or Course"
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
                                <template #option="scope">
                                    <q-item
                                        v-bind="scope.itemProps"
                                        :disable="scope.opt.disable"
                                        :class="{ 'text-weight-bold': scope.opt.header }"
                                    >
                                        <q-item-section>
                                            <q-item-label>{{ scope.opt.label }}</q-item-label>
                                        </q-item-section>
                                    </q-item>
                                </template>
                                <template #selected>
                                    {{
                                        classLevelOptions.find((opt) => opt.value === selectedClassOrCourse)?.label ||
                                        "Select Class Year or Course"
                                    }}
                                </template>
                            </q-select>
                        </div>

                        <!-- Group Type Selection (only for class years, not courses) -->
                        <div
                            v-if="!selectedCourse"
                            class="col-12 col-md-6"
                        >
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

                        <!-- Ross Students Toggle (only for V3 and V4) -->
                        <div
                            v-if="showRossCheckbox"
                            class="col-12"
                        >
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
                        class="row q-mt-md q-gutter-sm no-print"
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
                        class="row q-mt-md no-print"
                    >
                        <q-btn
                            color="primary"
                            icon="print"
                            label="Print"
                            @click="handlePrint"
                        />
                    </div>

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
                        <!-- Q-Table for Grid and List views -->
                        <q-table
                            v-if="galleryStore.galleryView !== 'sheet'"
                            :rows="galleryStore.students"
                            :columns="photoColumns"
                            :grid="galleryStore.galleryView === 'grid'"
                            :filter="photoFilter"
                            row-key="mailId"
                            :pagination="{ rowsPerPage: 0 }"
                            hide-header
                            :loading="galleryStore.loading"
                            separator="cell"
                            bordered
                            class="photo-gallery-table"
                        >
                            <!-- Filter input in top-right corner -->
                            <template #top-right>
                                <q-input
                                    v-model="photoFilter"
                                    dense
                                    outlined
                                    debounce="300"
                                    placeholder="Filter students"
                                    clearable
                                    class="q-mr-sm"
                                >
                                    <template #append>
                                        <q-icon name="search" />
                                    </template>
                                </q-input>
                            </template>

                            <!-- Title in top-left -->
                            <template #top-left>
                                <div class="text-h5 text-weight-bold">{{ pageTitle }}</div>
                            </template>

                            <!-- Grid/List item rendering -->
                            <!-- Grid view: Use item slot for grid mode -->
                            <template
                                v-if="galleryStore.galleryView === 'grid'"
                                #item="props"
                            >
                                <div class="col-4 col-sm-3 col-md-2 col-lg-2 col-xl-1 q-pa-xs">
                                    <StudentPhotoCard
                                        :student="props.row"
                                        :current-index="props.rowIndex"
                                        @click="handleStudentClickByMailId(props.row.mailId)"
                                    />
                                </div>
                            </template>

                            <!-- List view: Use body slot for table mode -->
                            <template
                                v-if="galleryStore.galleryView === 'list'"
                                #body="props"
                            >
                                <q-tr
                                    :props="props"
                                    @click="handleStudentClickByMailId(props.row.mailId)"
                                    class="cursor-pointer"
                                >
                                    <q-td colspan="100%">
                                        <q-item class="q-pa-sm">
                                            <q-item-section avatar>
                                                <img
                                                    v-if="props.row.hasPhoto"
                                                    :src="getPhotoUrl(props.row)"
                                                    :alt="`${props.row.displayName} photo`"
                                                    loading="lazy"
                                                    style="
                                                        width: 42px;
                                                        height: 56px;
                                                        object-fit: cover;
                                                        border-radius: 4px;
                                                    "
                                                />
                                                <q-avatar
                                                    v-else
                                                    size="56px"
                                                >
                                                    <q-icon
                                                        name="person"
                                                        size="32px"
                                                    />
                                                </q-avatar>
                                            </q-item-section>

                                            <q-item-section>
                                                <q-item-label class="text-weight-medium">
                                                    {{ props.row.displayName }}
                                                </q-item-label>
                                                <q-item-label caption>
                                                    {{ props.row.mailId }}@ucdavis.edu
                                                </q-item-label>
                                                <q-item-label
                                                    v-if="props.row.secondaryTextLines.length > 0"
                                                    caption
                                                >
                                                    {{ props.row.secondaryTextLines.join(" • ") }}
                                                </q-item-label>
                                            </q-item-section>

                                            <q-item-section side>
                                                <q-badge
                                                    v-if="props.row.isRossStudent"
                                                    color="primary"
                                                    label="Ross"
                                                />
                                            </q-item-section>
                                        </q-item>
                                    </q-td>
                                </q-tr>
                            </template>

                            <!-- No results message -->
                            <template #no-data>
                                <div class="full-width row flex-center q-gutter-sm q-py-xl">
                                    <q-icon
                                        name="photo_library"
                                        size="100px"
                                        color="grey-5"
                                    />
                                    <div class="text-center">
                                        <div class="text-h6 text-grey">No photos to display</div>
                                        <div class="text-subtitle2 text-grey">
                                            {{
                                                photoFilter.trim()
                                                    ? `No students found matching "${photoFilter}"`
                                                    : hasActiveFilters
                                                      ? "No students found for the selected filters"
                                                      : "Select a class year or group to view student photos"
                                            }}
                                        </div>
                                    </div>
                                </div>
                            </template>
                        </q-table>

                        <!-- Keep PhotoSheet separate for print view (no filtering needed) -->
                        <PhotoSheet
                            v-else
                            :students="galleryStore.students"
                            :title="pageTitle"
                            :generated-date="galleryDateFormatted"
                        />
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
                </q-tab-panel>

                <!-- Student List Tab -->
                <q-tab-panel name="list">
                    <div class="row q-col-gutter-md no-print">
                        <!-- Class Year Selection -->
                        <div class="col-12 col-md-6">
                            <q-select
                                v-model="selectedStudentListYear"
                                :options="studentListYearOptions"
                                label="Select Class Year"
                                outlined
                                dense
                                options-dense
                                emit-value
                                map-options
                                @update:model-value="onStudentListYearChange"
                            >
                                <template #prepend>
                                    <q-icon name="school" />
                                </template>
                            </q-select>
                        </div>

                        <!-- Ross Students Toggle (only for V3 and V4) -->
                        <div
                            v-if="showRossCheckboxInList"
                            class="col-12"
                        >
                            <q-checkbox
                                v-model="includeRossStudentsInList"
                                label="Include Ross Students"
                                @update:model-value="onRossStudentsToggle"
                            />
                        </div>
                    </div>

                    <!-- Student List Table -->
                    <div
                        v-if="studentListLoading"
                        class="q-mt-lg text-center"
                    >
                        <q-spinner
                            size="50px"
                            color="primary"
                        />
                        <div class="q-mt-md">Loading students...</div>
                    </div>

                    <div
                        v-else-if="studentListError"
                        class="q-mt-lg"
                    >
                        <q-banner class="bg-negative text-white">
                            <template #avatar>
                                <q-icon name="warning" />
                            </template>
                            {{ studentListError }}
                        </q-banner>
                    </div>

                    <div
                        v-else-if="studentListData.length > 0"
                        class="q-mt-lg"
                    >
                        <!-- Print Button for Student List -->
                        <div class="row q-mb-md no-print">
                            <q-btn
                                color="primary"
                                icon="print"
                                label="Print"
                                @click="handlePrint"
                            />
                        </div>

                        <div class="row items-center q-mb-md no-print">
                            <div class="col">
                                <div class="text-subtitle1">
                                    {{ studentListTitle }}
                                </div>
                                <div class="text-caption text-grey-7">Generated: {{ currentDateFormatted }}</div>
                            </div>
                        </div>

                        <q-table
                            :rows="studentListData"
                            :columns="studentListColumns"
                            row-key="mailId"
                            dense
                            :pagination="{ rowsPerPage: 0 }"
                            :filter="studentListFilter"
                            class="student-list-table"
                        >
                            <template #top-right>
                                <div class="no-print">
                                    <q-input
                                        v-model="studentListFilter"
                                        dense
                                        outlined
                                        debounce="300"
                                        placeholder="Filter students"
                                    >
                                        <template #append>
                                            <q-icon name="search" />
                                        </template>
                                    </q-input>
                                </div>
                            </template>
                        </q-table>
                    </div>

                    <div
                        v-else
                        class="q-mt-lg text-center text-grey"
                    >
                        <q-icon
                            name="people"
                            size="100px"
                            color="grey-5"
                        />
                        <div class="q-mt-md text-h6">No students to display</div>
                        <div class="text-subtitle2">Select a class year to view the student list</div>
                    </div>
                </q-tab-panel>
            </q-tab-panels>
        </q-card>

        <!-- Centralized Student Dialog -->
        <StudentPhotoDialog
            v-model="showStudentDialog"
            :students="galleryStore.students"
            :initial-index="selectedStudentIndex"
            @update:index="selectedStudentIndex = $event"
        />
    </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch, nextTick } from "vue"
import { useQuasar } from "quasar"
import { useRoute, useRouter } from "vue-router"
import { usePhotoGalleryStore } from "../stores/photo-gallery-store"
import { photoGalleryService } from "../services/photo-gallery-service"
import type { ClassYear, CoursesByTerm } from "../services/photo-gallery-service"
import { usePhotoGalleryOptions } from "../composables/use-photo-gallery-options"
import { getPhotoUrl } from "../composables/use-photo-url"
import PhotoSheet from "../components/PhotoGallery/PhotoSheet.vue"
import StudentPhotoCard from "../components/PhotoGallery/StudentPhotoCard.vue"
import StudentPhotoDialog from "../components/PhotoGallery/StudentPhotoDialog.vue"
import type { QTableColumn } from "quasar"
import type { StudentPhoto } from "../services/photo-gallery-service"

// Constants
const PRINT_DELAY_MS = 1000 // Delay before triggering print dialog to ensure images are loaded

const $q = useQuasar()
const route = useRoute()
const router = useRouter()
const galleryStore = usePhotoGalleryStore()

// Photo Gallery Tab
const selectedClassLevel = ref<string | null>(null)
const selectedGroupType = ref<string | null>(null)
const selectedGroup = ref<string | null>(null)
const selectedCourse = ref<{ termCode: string; crn: string } | null>(null)
const classYears = ref<ClassYear[]>([])
const availableCourses = ref<CoursesByTerm[]>([])

// Photo Gallery Filter
const photoFilter = ref("")

// Column definitions for q-table filtering
const photoColumns: QTableColumn[] = [
    {
        name: "name",
        field: (row: StudentPhoto) => `${row.firstName} ${row.lastName} ${row.displayName}`,
        label: "Name",
        align: "left",
    },
    {
        name: "email",
        field: "mailId",
        label: "Email",
        align: "left",
    },
    {
        name: "groups",
        field: (row: StudentPhoto) => row.secondaryTextLines.join(" "),
        label: "Groups",
        align: "left",
    },
]

// Computed property for the select v-model (combines class level and course)
const selectedClassOrCourse = computed({
    get: () => {
        if (selectedCourse.value) {
            return `course:${selectedCourse.value.termCode}:${selectedCourse.value.crn}`
        }
        return selectedClassLevel.value
    },
    set: (_value: string | null) => {
        // This setter is handled by onClassLevelChange, but we need it for v-model binding
    },
})

// Show Ross checkbox only for V3 and V4 class levels (Photo Gallery)
const showRossCheckbox = computed(() => selectedClassLevel.value === "V3" || selectedClassLevel.value === "V4")

// Show Ross checkbox for Student List (only for V3 and V4 years)
const showRossCheckboxInList = computed(() => {
    if (!selectedStudentListYear.value) {
        return false
    }
    const selectedYear = classYearOptions.value.find((cy) => cy.year === selectedStudentListYear.value)
    return selectedYear?.classLevel === "V3" || selectedYear?.classLevel === "V4"
})

// Tab Management
const activeTab = ref<string>("photos")

// Student List Tab
const selectedStudentListYear = ref<number | null>(null)
const studentListData = ref<any[]>([])
const studentListLoading = ref(false)
const studentListError = ref<string | null>(null)
const studentListFilter = ref("")
const includeRossStudentsInList = ref(false)

// Independent timestamps for student list vs. photo gallery flows
const studentListLastLoadedAt = ref<Date | null>(null)
const galleryLastLoadedAt = ref<Date | null>(null)

// Student dialog management
const showStudentDialog = ref(false)
const selectedStudentIndex = ref(0)
const pendingStudentMailId = ref<string | null>(null)
const isUpdatingUrlProgrammatically = ref(false)

const studentListColumns = [
    { name: "number", label: "#", field: "number", align: "left" as const, sortable: false, style: "width: 50px" },
    { name: "name", label: "Name", field: "fullName", align: "left" as const, sortable: true },
    { name: "email", label: "Email", field: "email", align: "left" as const, sortable: true },
    { name: "priorName", label: "Prior Name", field: "priorName", align: "left" as const, sortable: true },
]

// Use the composable for building dropdown options
const { classYearOptions, classLevelOptions, studentListYearOptions } = usePhotoGalleryOptions(
    classYears,
    availableCourses,
)

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

const pageMainTitle = computed(() => {
    if (activeTab.value === "list") {
        return "Student List"
    }
    return "Student Photo Gallery"
})

const studentListTitle = computed(() => {
    if (!selectedStudentListYear.value) return "Student List"
    const classYearInfo = classYears.value.find((cy) => cy.year === selectedStudentListYear.value)
    const yearDisplay = classYearInfo
        ? `${classYearInfo.year} (${classYearInfo.classLevel})`
        : selectedStudentListYear.value
    let title = `Class of ${yearDisplay}`
    if (studentListData.value.length > 0) {
        title += ` - ${studentListData.value.length} Student${studentListData.value.length !== 1 ? "s" : ""}`
    }
    if (includeRossStudentsInList.value) {
        title += " (including Ross)"
    }
    return title
})

// Helper function to format dates - DRY principle
function formatTimestamp(date: Date | null): string {
    if (!date) {
        return ""
    }

    return date.toLocaleDateString("en-US", {
        year: "numeric",
        month: "long",
        day: "numeric",
        hour: "2-digit",
        minute: "2-digit",
    })
}

// Student List uses its own timestamp
const currentDateFormatted = computed(() => formatTimestamp(studentListLastLoadedAt.value))

// Photo Gallery uses its own timestamp for sheet view
const galleryDateFormatted = computed(() => formatTimestamp(galleryLastLoadedAt.value))

const pageTitle = computed(() => {
    const parts: string[] = []

    // Show course info if a course is selected
    if (galleryStore.selectedCourse) {
        const course = galleryStore.selectedCourse
        parts.push(`${course.subjectCode}${course.courseNumber} - ${course.title}`)
        if (course.termDescription) {
            parts.push(course.termDescription)
        }
    } else if (selectedClassYearDisplay.value) {
        // Otherwise show class year
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

async function onClassLevelChange(value: string | null) {
    selectedGroupType.value = null
    selectedGroup.value = null

    if (!value) {
        galleryStore.includeRossStudents = false
        selectedClassLevel.value = null
        selectedCourse.value = null
        galleryStore.clearSelection()
        updateUrlParams()
        return
    }

    // Ensure value is a string (q-select might emit objects in some cases)
    const stringValue = typeof value === "string" ? value : String(value)

    // Check if this is a course selection (format: "course:termCode:crn")
    if (stringValue.startsWith("course:")) {
        const [, termCode, crn] = stringValue.split(":")
        if (!termCode || !crn) {
            $q.notify({
                type: "negative",
                message: "Invalid course selection",
            })
            return
        }
        galleryStore.includeRossStudents = false
        selectedClassLevel.value = null
        selectedCourse.value = { termCode, crn }
        updateUrlParams()
        await galleryStore.fetchCoursePhotos(termCode, crn)
    } else {
        // This is a class level selection
        selectedClassLevel.value = stringValue
        selectedCourse.value = null
        if (stringValue !== "V3" && stringValue !== "V4") {
            galleryStore.includeRossStudents = false
        }
        updateUrlParams()
        await galleryStore.fetchClassPhotos(stringValue)
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

// Helper function to reload current Photo Gallery data
async function reloadPhotoGalleryData() {
    if (!selectedClassLevel.value) return

    if (selectedGroupType.value && selectedGroup.value) {
        await galleryStore.fetchGroupPhotos(selectedGroupType.value, selectedGroup.value, selectedClassLevel.value)
    } else {
        await galleryStore.fetchClassPhotos(selectedClassLevel.value)
    }
}

async function updateUrlParams() {
    isUpdatingUrlProgrammatically.value = true
    try {
        const query: Record<string, string> = {}
        if (selectedClassLevel.value) {
            query.classLevel = selectedClassLevel.value
        }
        if (selectedCourse.value) {
            query.termCode = selectedCourse.value.termCode
            query.crn = selectedCourse.value.crn
        }
        if (selectedGroupType.value) {
            query.groupType = selectedGroupType.value
        }
        if (selectedGroup.value) {
            query.group = selectedGroup.value
        }
        if (galleryStore.includeRossStudents) {
            query.includeRoss = "true"
        }
        if (galleryStore.galleryView !== "grid") {
            query.view = galleryStore.galleryView
        }
        if (activeTab.value !== "photos") {
            query.tab = activeTab.value
        }
        if (selectedStudentListYear.value) {
            query.studentListYear = selectedStudentListYear.value.toString()
        }
        if (includeRossStudentsInList.value) {
            query.includeRossInList = "true"
        }

        // Add student parameter if dialog is open
        if (showStudentDialog.value && galleryStore.students?.length > 0) {
            if (selectedStudentIndex.value >= 0 && selectedStudentIndex.value < galleryStore.students.length) {
                const selectedStudent = galleryStore.students[selectedStudentIndex.value]
                if (selectedStudent) {
                    query.student = selectedStudent.mailId
                }
            }
        }

        await router.replace({ query })

        // Reset flag after router operation completes
        await nextTick()
    } finally {
        isUpdatingUrlProgrammatically.value = false
    }
}

function onTabChange(_tab: string) {
    updateUrlParams()
}

async function onStudentListYearChange(year: number | null) {
    updateUrlParams()

    if (!year) {
        studentListData.value = []
        return
    }

    studentListLoading.value = true
    studentListError.value = null

    try {
        // Find the class level for this year
        const classYearInfo = classYears.value.find((cy) => cy.year === year)
        if (!classYearInfo) {
            throw new Error(`Could not find class level for year ${year}`)
        }
        if (classYearInfo.classLevel !== "V3" && classYearInfo.classLevel !== "V4") {
            includeRossStudentsInList.value = false
        }

        // Use the photo gallery service for consistent API calls with CSRF token handling
        const result = await photoGalleryService.getClassGallery(
            classYearInfo.classLevel,
            includeRossStudentsInList.value,
        )
        if (!result || !Array.isArray(result.students)) {
            throw new Error("Failed to load students")
        }

        // Map the data to include row number and email
        studentListData.value = result.students.map((s: any, index: number) => ({
            number: index + 1,
            personId: s.personId,
            fullName: s.displayName || `${s.lastName}, ${s.firstName}`,
            email: s.mailId ? `${s.mailId}@ucdavis.edu` : "",
            priorName: "", // Photo gallery doesn't have prior name data
        }))

        // Update timestamp to reflect when student list data was actually loaded
        studentListLastLoadedAt.value = new Date()
    } catch (error: any) {
        studentListError.value = error.message || "An error occurred while loading students"
        $q.notify({
            type: "negative",
            message: studentListError.value || "An error occurred while loading students",
        })
    } finally {
        studentListLoading.value = false
    }
}

async function onRossStudentsToggle() {
    updateUrlParams()

    // Reload data if a year is selected
    if (selectedStudentListYear.value) {
        await onStudentListYearChange(selectedStudentListYear.value)
    }
}

function loadFromUrlParams() {
    const {
        classLevel,
        termCode,
        crn,
        groupType,
        group,
        includeRoss,
        view,
        tab,
        studentListYear,
        includeRossInList,
        student,
    } = route.query

    if (typeof view === "string" && (view === "grid" || view === "list" || view === "sheet")) {
        galleryStore.galleryView = view
    }

    if (typeof tab === "string" && (tab === "photos" || tab === "list")) {
        activeTab.value = tab
    }

    // Handle course parameters
    if (typeof termCode === "string" && typeof crn === "string") {
        selectedCourse.value = { termCode, crn }
    } else if (typeof classLevel === "string") {
        // Only set classLevel if no course is selected
        selectedClassLevel.value = classLevel
    }

    if (typeof groupType === "string") {
        selectedGroupType.value = groupType
    }
    if (typeof group === "string") {
        selectedGroup.value = group
    }
    if (typeof studentListYear === "string") {
        const year = Number.parseInt(studentListYear, 10)
        if (!Number.isNaN(year)) {
            selectedStudentListYear.value = year
        }
    } else if (tab === "list" && typeof classLevel === "string" && !studentListYear) {
        // If Student List tab is active and we have classLevel but no studentListYear,
        // derive the year from the classLevel
        const classYearInfo = classYears.value.find((cy) => cy.classLevel === classLevel)
        if (classYearInfo) {
            selectedStudentListYear.value = classYearInfo.year
        }
    }

    // Handle Ross student inclusion - only allow for V3/V4 class levels
    // Photo Gallery Tab: Only set includeRoss if V3 or V4 class level is selected
    if (includeRoss === "true" && typeof classLevel === "string" && (classLevel === "V3" || classLevel === "V4")) {
        galleryStore.includeRossStudents = true
    }
    // If not V3/V4, the parameter is ignored (sanitized)

    // Student List Tab
    if (includeRossInList === "true" && typeof studentListYear === "string") {
        const year = Number.parseInt(studentListYear, 10)
        if (!Number.isNaN(year)) {
            // Find the class level for this year
            const yearOption = classYearOptions.value.find((cy) => cy.year === year)
            if (yearOption && (yearOption.classLevel === "V3" || yearOption.classLevel === "V4")) {
                includeRossStudentsInList.value = true
            }
            // If not V3/V4, the parameter is ignored (sanitized)
        }
    }

    // Handle student parameter - store for opening after data loads
    if (typeof student === "string") {
        pendingStudentMailId.value = student
    }
}

async function fetchClassYears() {
    try {
        const years = await photoGalleryService.getClassYears()
        if (!Array.isArray(years)) {
            throw new Error("Invalid class year response")
        }
        classYears.value = years
    } catch {
        $q.notify({
            type: "negative",
            message: "Failed to load class years",
        })
    }
}

async function fetchAvailableCourses() {
    try {
        const courses = await photoGalleryService.getAvailableCourses()
        if (!Array.isArray(courses)) {
            throw new TypeError("Invalid courses response")
        }
        availableCourses.value = courses
    } catch {
        $q.notify({
            type: "negative",
            message: "Failed to load available courses",
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

function handlePrint() {
    globalThis.print()
}

async function handleStudentClickByMailId(mailId: string) {
    if (!galleryStore.students || galleryStore.students.length === 0) {
        return
    }

    const index = galleryStore.students.findIndex((student) => student.mailId === mailId)
    if (index < 0) {
        return
    }

    selectedStudentIndex.value = index

    // Update URL BEFORE opening dialog to prevent router.replace() from closing it
    const selectedStudent = galleryStore.students[index]
    if (selectedStudent) {
        isUpdatingUrlProgrammatically.value = true
        try {
            const currentQuery = { ...route.query }
            currentQuery.student = selectedStudent.mailId
            await router.replace({ query: currentQuery })
            await nextTick()
        } finally {
            isUpdatingUrlProgrammatically.value = false
        }
    }

    // Use setTimeout to ensure dialog opens after the current click event fully completes
    // This prevents the click event from being interpreted as a backdrop click
    setTimeout(() => {
        showStudentDialog.value = true
    }, 0)
}

onMounted(async () => {
    await galleryStore.fetchGalleryMenu()
    await fetchClassYears()
    await fetchAvailableCourses()

    loadFromUrlParams()

    // Wait for reactivity to settle after loading URL params
    await nextTick()

    if (selectedCourse.value) {
        // Fetch course photos if a course is selected
        await galleryStore.fetchCoursePhotos(selectedCourse.value.termCode, selectedCourse.value.crn)
    } else if (selectedClassLevel.value) {
        // Always fetch class photos first to populate the group counts cache
        await galleryStore.fetchClassPhotos(selectedClassLevel.value)

        // Then fetch specific group if one is selected
        if (selectedGroup.value && selectedGroupType.value) {
            await galleryStore.fetchGroupPhotos(selectedGroupType.value, selectedGroup.value, selectedClassLevel.value)
        }
    }

    // Load student list data if a year is selected
    if (selectedStudentListYear.value) {
        await onStudentListYearChange(selectedStudentListYear.value)
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
    async (newView, oldView) => {
        // Reload data and auto-trigger print dialog when switching to sheet view
        if (newView === "sheet" && oldView !== "sheet") {
            // Reload Photo Gallery data to ensure it's fresh
            await reloadPhotoGalleryData()

            // Auto-trigger print dialog after data loads
            if (galleryStore.hasStudents) {
                // Use nextTick + setTimeout to ensure images are loaded before printing
                nextTick(() => {
                    setTimeout(() => {
                        window.print()
                    }, PRINT_DELAY_MS)
                })
            }
        }
    },
)

watch(
    () => galleryStore.students,
    (newStudents) => {
        // Update timestamp when Photo Gallery data loads
        if (newStudents && newStudents.length > 0) {
            galleryLastLoadedAt.value = new Date()

            // Handle pending student modal open from URL parameter
            if (pendingStudentMailId.value) {
                handleStudentClickByMailId(pendingStudentMailId.value)
                pendingStudentMailId.value = null
            }
        }
    },
)

// Reload data when switching to Student List tab
watch(activeTab, async (newTab, oldTab) => {
    if (newTab === "list" && oldTab === "photos" && selectedStudentListYear.value) {
        // Reload Student List data to ensure it's fresh
        await onStudentListYearChange(selectedStudentListYear.value)
    }
})

// Update URL when student dialog closes
watch(showStudentDialog, (newShowDialog, oldShowDialog) => {
    // Skip URL updates if we're already in the middle of updating programmatically
    // This prevents circular dependencies where router.replace() causes the dialog to close
    if (isUpdatingUrlProgrammatically.value) {
        return
    }

    // Only update URL when dialog is closing to remove the student parameter
    const isClosing = oldShowDialog && !newShowDialog

    if (isClosing) {
        updateUrlParams()
    }
})

// Update URL during arrow key navigation using History API
// This approach bypasses Vue Router to avoid triggering navigation cycles that close the dialog
watch(selectedStudentIndex, (newIndex) => {
    // Only update URL if dialog is open and we have students
    if (showStudentDialog.value && galleryStore.students && galleryStore.students.length > 0) {
        const student = galleryStore.students[newIndex]
        if (student) {
            // Use History API directly to avoid triggering Vue Router
            const url = new URL(globalThis.location.href)
            url.searchParams.set("student", student.mailId)
            globalThis.history.replaceState({}, "", url.toString())
        }
    }
})
</script>

<style>
/* Remove box-within-box appearance */
.photo-gallery-table .q-table__card {
    box-shadow: none !important;
    border: none !important;
}

.photo-gallery-table .q-table__top {
    background-color: transparent !important;
    padding: 16px !important;
}

.photo-gallery-table .q-table__bottom {
    background-color: transparent !important;
}

.photo-gallery-table .q-table__container {
    border: 1px solid rgb(0 0 0 / 12%) !important;
    box-shadow: none !important;
}

/* List view borders - add borders to each item */
.photo-gallery-table .q-table__grid-content .q-item {
    border-bottom: 1px solid rgb(0 0 0 / 12%);
}

.photo-gallery-table .q-table__grid-content .q-item:last-child {
    border-bottom: none;
}

@media print {
    /* Default to landscape for photo gallery */
    @page {
        size: landscape;
    }

    /* Portrait for student list */
    .print-table {
        page: portrait-page;
    }

    @page portrait-page {
        size: portrait;
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

    /* Hide only the tab navigation bar, not the tab panels */
    .q-tabs__content {
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

    /* Hide inactive tab panels */
    .q-tab-panel[aria-hidden="true"] {
        display: none !important;
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

    /* QTable print styling */

    /* Hide search box and pagination */
    .q-table__top,
    .q-table__bottom {
        display: none !important;
    }

    /* Remove top gap */
    .q-mt-lg,
    .q-table {
        margin-top: 0 !important;
    }

    /* Style table header */
    .q-table thead th {
        background-color: #e0e0e0 !important;
        font-weight: bold !important;
        -webkit-print-color-adjust: exact !important;
        print-color-adjust: exact !important;
    }
}
</style>
