<template>
    <div class="q-pa-md">
        <h2>Instructor List for {{ currentTermName }}</h2>

        <!-- Term selector and filters -->
        <div class="row q-col-gutter-sm q-mb-md items-center">
            <div class="col-12 col-sm-4 col-md-3">
                <q-select
                    v-model="selectedTermCode"
                    :options="terms"
                    option-label="termName"
                    option-value="termCode"
                    emit-value
                    map-options
                    label="Term"
                    dense
                    options-dense
                    outlined
                />
            </div>
            <div class="col-6 col-sm-3 col-md-2">
                <q-select
                    v-model="selectedDept"
                    :options="deptOptions"
                    label="Department"
                    dense
                    options-dense
                    outlined
                    clearable
                />
            </div>
            <div class="col-6 col-sm-3 col-md-2">
                <q-input
                    v-model="searchText"
                    label="Search"
                    dense
                    outlined
                    clearable
                >
                    <template #append>
                        <q-icon name="search" />
                    </template>
                </q-input>
            </div>

            <div class="col-12 col-sm-auto">
                <q-btn
                    v-if="hasImportInstructor"
                    color="primary"
                    label="Add Instructor"
                    icon="person_add"
                    dense
                    class="full-width-xs"
                    @click="showAddDialog = true"
                />
            </div>
        </div>

        <!-- Loading state -->
        <div
            v-if="isLoading"
            class="text-grey q-my-md"
        >
            Loading instructors...
        </div>

        <!-- Grouped instructors by department -->
        <template v-else-if="groupedInstructors.length > 0">
            <div
                v-for="deptGroup in groupedInstructors"
                :key="deptGroup.dept"
                class="q-mb-sm"
            >
                <!-- Department header -->
                <div class="dept-header text-white q-pa-sm">
                    {{ deptGroup.dept }}
                </div>

                <!-- Instructors table for this department -->
                <q-table
                    :rows="deptGroup.instructors"
                    :columns="columns"
                    row-key="personId"
                    dense
                    flat
                    bordered
                    hide-pagination
                    :rows-per-page-options="[0]"
                    class="dept-table"
                >
                    <template #body-cell-isVerified="props">
                        <q-td :props="props">
                            <q-icon
                                v-if="props.row.isVerified"
                                name="check"
                                color="positive"
                                size="sm"
                            >
                                <q-tooltip>Effort verified</q-tooltip>
                            </q-icon>
                        </q-td>
                    </template>
                    <template #body-cell-fullName="props">
                        <q-td :props="props">
                            <router-link
                                :to="{
                                    name: 'InstructorDetail',
                                    params: {
                                        termCode: selectedTermCode,
                                        personId: props.row.personId,
                                    },
                                }"
                                class="text-weight-medium instructor-name"
                            >
                                {{ props.row.fullName }}
                            </router-link>
                            <div
                                v-if="props.row.title"
                                class="text-caption title-code"
                            >
                                {{ props.row.title }}
                            </div>
                        </q-td>
                    </template>
                    <template #body-cell-actions="props">
                        <q-td :props="props">
                            <q-btn
                                v-if="hasEditInstructor"
                                icon="edit"
                                color="primary"
                                dense
                                flat
                                round
                                size="sm"
                                @click="openEditDialog(props.row)"
                            >
                                <q-tooltip>Edit instructor</q-tooltip>
                            </q-btn>
                            <q-btn
                                v-if="hasDeleteInstructor"
                                icon="delete"
                                color="negative"
                                dense
                                flat
                                round
                                size="sm"
                                @click="confirmDeleteInstructor(props.row)"
                            >
                                <q-tooltip>Delete instructor</q-tooltip>
                            </q-btn>
                        </q-td>
                    </template>
                </q-table>
            </div>
        </template>

        <!-- No data state -->
        <div
            v-else
            class="full-width row flex-center text-grey q-gutter-sm q-py-lg"
        >
            <q-icon
                name="person"
                size="2em"
            />
            <span>No instructors found for this term</span>
        </div>

        <!-- Add Dialog -->
        <InstructorAddDialog
            v-model="showAddDialog"
            :term-code="selectedTermCode"
            @created="onInstructorCreated"
        />

        <!-- Edit Dialog -->
        <InstructorEditDialog
            v-model="showEditDialog"
            :instructor="selectedInstructor"
            :term-code="selectedTermCode"
            @updated="onInstructorUpdated"
        />
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from "vue"
import { useQuasar } from "quasar"
import { useRoute, useRouter } from "vue-router"
import { effortService } from "../services/effort-service"
import { termService } from "../services/term-service"
import { useEffortPermissions } from "../composables/use-effort-permissions"
import type { PersonDto, TermDto } from "../types"
import type { QTableColumn } from "quasar"
import InstructorAddDialog from "../components/InstructorAddDialog.vue"
import InstructorEditDialog from "../components/InstructorEditDialog.vue"

const $q = useQuasar()
const route = useRoute()
const router = useRouter()
const { hasImportInstructor, hasEditInstructor, hasDeleteInstructor } = useEffortPermissions()

// State
const terms = ref<TermDto[]>([])
const selectedTermCode = ref<number | null>(null)
const instructors = ref<PersonDto[]>([])
const selectedDept = ref<string | null>(null)
const searchText = ref("")
const isLoading = ref(false)

// Dialogs
const showAddDialog = ref(false)
const showEditDialog = ref(false)
const selectedInstructor = ref<PersonDto | null>(null)

// Computed
const currentTermName = computed(() => {
    if (!selectedTermCode.value) return ""
    const term = terms.value.find((t) => t.termCode === selectedTermCode.value)
    return term?.termName ?? ""
})

const deptOptions = computed(() => {
    const uniqueDepts = [...new Set(instructors.value.map((i) => i.effortDept))].sort()
    return ["", ...uniqueDepts]
})

const filteredInstructors = computed(() => {
    let result = instructors.value

    if (selectedDept.value) {
        result = result.filter((i) => i.effortDept === selectedDept.value)
    }

    if (searchText.value) {
        const search = searchText.value.toLowerCase()
        result = result.filter(
            (i) =>
                i.fullName.toLowerCase().includes(search) ||
                i.firstName.toLowerCase().includes(search) ||
                i.lastName.toLowerCase().includes(search),
        )
    }

    return result
})

// Group instructors by department
const groupedInstructors = computed(() => {
    const groups: Record<string, PersonDto[]> = {}

    for (const instructor of filteredInstructors.value) {
        const dept = instructor.effortDept || "UNK"
        const groupArray = groups[dept] ?? (groups[dept] = [])
        groupArray.push(instructor)
    }

    // Sort departments and return as array with guaranteed instructor arrays
    return Object.keys(groups)
        .sort()
        .map((dept) => ({
            dept,
            instructors: groups[dept] ?? [],
        }))
})

const columns = computed<QTableColumn[]>(() => [
    {
        name: "isVerified",
        label: "Verified",
        field: "isVerified",
        align: "center",
        sortable: true,
        style: "width: 80px",
        headerStyle: "width: 80px",
    },
    {
        name: "fullName",
        label: "Instructor",
        field: "fullName",
        align: "left",
        sortable: true,
    },
    {
        name: "actions",
        label: "Actions",
        field: "actions",
        align: "center",
        style: "width: 100px",
        headerStyle: "width: 100px",
    },
])

// Methods
async function loadTerms() {
    const paramTermCode = route.params.termCode ? parseInt(route.params.termCode as string, 10) : null

    terms.value = await termService.getTerms()

    if (paramTermCode && terms.value.some((t) => t.termCode === paramTermCode)) {
        selectedTermCode.value = paramTermCode
        await loadInstructors()
    } else {
        router.replace({ name: "EffortHome" })
    }
}

watch(selectedTermCode, (newTermCode, oldTermCode) => {
    if (newTermCode && oldTermCode !== null) {
        router.replace({ name: "InstructorList", params: { termCode: newTermCode.toString() } })
        loadInstructors()
    }
})

watch(
    () => route.params.termCode,
    (newTermCode) => {
        const parsed = newTermCode ? parseInt(newTermCode as string, 10) : null
        if (parsed && parsed !== selectedTermCode.value && terms.value.some((t) => t.termCode === parsed)) {
            selectedTermCode.value = parsed
        }
    },
)

async function loadInstructors() {
    if (!selectedTermCode.value) return

    isLoading.value = true
    try {
        instructors.value = await effortService.getInstructors(selectedTermCode.value)
    } finally {
        isLoading.value = false
    }
}

function openEditDialog(instructor: PersonDto) {
    selectedInstructor.value = instructor
    showEditDialog.value = true
}

function confirmDeleteInstructor(instructor: PersonDto) {
    $q.dialog({
        title: "Delete Instructor",
        message: `Are you sure you want to delete ${instructor.fullName}? This will also delete all associated effort records for this term.`,
        cancel: true,
        persistent: true,
    }).onOk(async () => {
        if (!selectedTermCode.value) return

        const { recordCount } = await effortService.canDeleteInstructor(instructor.personId, selectedTermCode.value)
        if (recordCount > 0) {
            $q.dialog({
                title: "Confirm Delete",
                message: `This instructor has ${recordCount} effort record(s) that will also be deleted. Are you sure you want to proceed?`,
                cancel: true,
                persistent: true,
            }).onOk(() => deleteInstructor(instructor.personId))
        } else {
            await deleteInstructor(instructor.personId)
        }
    })
}

async function deleteInstructor(personId: number) {
    if (!selectedTermCode.value) return

    const success = await effortService.deleteInstructor(personId, selectedTermCode.value)
    if (success) {
        $q.notify({ type: "positive", message: "Instructor deleted successfully" })
        await loadInstructors()
    } else {
        $q.notify({ type: "negative", message: "Failed to delete instructor" })
    }
}

function onInstructorCreated() {
    $q.notify({ type: "positive", message: "Instructor added successfully" })
    loadInstructors()
}

function onInstructorUpdated() {
    $q.notify({ type: "positive", message: "Instructor updated successfully" })
    loadInstructors()
}

onMounted(loadTerms)
</script>

<style scoped>
@media (width <= 599px) {
    .full-width-xs {
        width: 100%;
    }
}

.dept-header {
    background-color: #5f6a6a;
    font-weight: bold;
}

.dept-table {
    margin-bottom: 0;
    table-layout: fixed;
    width: 100%;
}

.dept-table :deep(table) {
    table-layout: fixed;
    width: 100%;
}

.instructor-name {
    color: #800000;
}

.title-code {
    color: #666;
    font-size: 0.8em;
    text-transform: uppercase;
}
</style>
