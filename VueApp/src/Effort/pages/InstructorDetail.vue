<template>
    <div class="q-pa-md">
        <!-- Breadcrumb -->
        <q-breadcrumbs class="q-mb-md">
            <q-breadcrumbs-el
                label="Instructors"
                :to="{ name: 'InstructorList', params: { termCode } }"
            />
            <q-breadcrumbs-el :label="instructor?.fullName ?? 'Instructor'" />
        </q-breadcrumbs>

        <!-- Loading state -->
        <div
            v-if="isLoading"
            class="text-grey q-my-md"
        >
            Loading instructor...
        </div>

        <!-- Error state -->
        <q-banner
            v-else-if="loadError"
            class="bg-negative text-white q-mb-md"
        >
            {{ loadError }}
            <template #action>
                <q-btn
                    flat
                    label="Go Back"
                    :to="{ name: 'InstructorList', params: { termCode } }"
                />
            </template>
        </q-banner>

        <!-- Instructor content -->
        <template v-else-if="instructor">
            <!-- Instructor Header -->
            <div class="row items-center q-mb-md">
                <h2 class="q-my-none">
                    Effort for {{ instructor.firstName }} {{ instructor.lastName }} - {{ currentTermName }}
                </h2>
                <q-space />
                <div
                    v-if="instructor.isVerified"
                    class="text-positive"
                >
                    <q-icon
                        name="check_circle"
                        size="sm"
                    />
                    Verified on {{ formatDate(instructor.effortVerified) }}
                </div>
            </div>

            <!-- Clinical Effort Note -->
            <p
                v-if="hasClinicalEffort"
                class="text-grey-8"
            >
                Enter clinical effort as weeks. Enter all other effort as hours.
            </p>

            <!-- Effort Records Table -->
            <q-table
                :rows="effortRecords"
                :columns="columns"
                row-key="id"
                dense
                flat
                bordered
                hide-pagination
                wrap-cells
                :rows-per-page-options="[0]"
                class="effort-table q-mb-lg"
            >
                <template #body-cell-course="props">
                    <q-td :props="props">
                        <router-link
                            :to="{
                                name: 'CourseDetail',
                                params: { termCode, courseId: props.row.course.id },
                            }"
                            class="text-primary"
                        >
                            {{ props.row.course.subjCode }}
                            {{ props.row.course.crseNumb.trim() }}-{{ props.row.course.seqNumb }}
                        </router-link>
                    </q-td>
                </template>
                <template #body-cell-effort="props">
                    <q-td
                        :props="props"
                        :class="{ 'zero-effort': props.row.effortValue === 0 }"
                    >
                        {{ props.row.effortValue ?? 0 }}
                        {{ props.row.effortLabel === "weeks" ? "Weeks" : "Hours" }}
                    </q-td>
                </template>
                <template #no-data>
                    <div class="full-width row flex-center text-grey q-gutter-sm q-py-lg">
                        <q-icon
                            name="school"
                            size="2em"
                        />
                        <span>No effort records for this instructor</span>
                    </div>
                </template>
            </q-table>

            <!-- Zero Effort Warning -->
            <q-banner
                v-if="hasZeroEffort"
                class="bg-warning q-mb-md"
                rounded
            >
                <template #avatar>
                    <q-icon
                        name="warning"
                        color="dark"
                    />
                </template>
                NOTE: This instructor has one or more effort items documented as ZERO effort. Effort cannot be verified
                until these items have been updated or removed.
            </q-banner>
        </template>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue"
import { useRoute } from "vue-router"
import type { QTableColumn } from "quasar"
import { effortService } from "../services/effort-service"
import type { PersonDto, TermDto, InstructorEffortRecordDto } from "../types"

const route = useRoute()

// Route params
const termCode = computed(() => route.params.termCode as string)
const personId = computed(() => parseInt(route.params.personId as string, 10))

// State
const instructor = ref<PersonDto | null>(null)
const effortRecords = ref<InstructorEffortRecordDto[]>([])
const terms = ref<TermDto[]>([])
const isLoading = ref(true)
const loadError = ref<string | null>(null)

// Computed
const currentTermName = computed(() => {
    const code = parseInt(termCode.value, 10)
    const term = terms.value.find((t) => t.termCode === code)
    return term?.termName ?? ""
})

const hasClinicalEffort = computed(() => {
    return effortRecords.value.some((r) => r.sessionType === "CLI")
})

const hasZeroEffort = computed(() => {
    return effortRecords.value.some((r) => {
        if (r.sessionType === "CLI") {
            return !r.weeks || r.weeks <= 0
        }
        return !r.hours || r.hours <= 0
    })
})

const columns = computed<QTableColumn[]>(() => [
    {
        name: "course",
        label: "Course",
        field: (row: InstructorEffortRecordDto) =>
            `${row.course.subjCode} ${row.course.crseNumb.trim()}-${row.course.seqNumb}`,
        align: "left",
        sortable: true,
    },
    {
        name: "units",
        label: "Units",
        field: (row: InstructorEffortRecordDto) => row.course.units,
        align: "left",
    },
    {
        name: "enrollment",
        label: "Enroll",
        field: (row: InstructorEffortRecordDto) => row.course.enrollment,
        align: "left",
    },
    {
        name: "role",
        label: "Role",
        field: "roleDescription",
        align: "left",
    },
    {
        name: "sessionType",
        label: "Session Type",
        field: "sessionType",
        align: "left",
    },
    {
        name: "effort",
        label: "Effort",
        field: "effortValue",
        align: "left",
    },
])

// Methods
function formatDate(dateString: string | null): string {
    if (!dateString) return ""
    const date = new Date(dateString)
    return date.toLocaleDateString("en-US", {
        month: "numeric",
        day: "numeric",
        year: "numeric",
        hour: "numeric",
        minute: "2-digit",
    })
}

async function loadData() {
    isLoading.value = true
    loadError.value = null

    try {
        const termCodeNum = parseInt(termCode.value, 10)

        const [instructorResult, recordsResult, termsResult] = await Promise.all([
            effortService.getInstructor(personId.value, termCodeNum),
            effortService.getInstructorEffortRecords(personId.value, termCodeNum),
            effortService.getTerms(),
        ])

        if (!instructorResult) {
            loadError.value = "Instructor not found or you do not have permission to view them."
            return
        }

        instructor.value = instructorResult
        effortRecords.value = recordsResult
        terms.value = termsResult
    } catch {
        loadError.value = "Failed to load instructor. Please try again."
    } finally {
        isLoading.value = false
    }
}

onMounted(loadData)
</script>

<style scoped>
.effort-table {
    width: 100%;
}

.effort-table :deep(.zero-effort) {
    background-color: #fff3cd;
    color: #856404;
}
</style>
