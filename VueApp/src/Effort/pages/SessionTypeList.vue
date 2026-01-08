<template>
    <div class="q-pa-md">
        <div class="row items-center q-mb-md">
            <h2 class="q-ma-none">Manage Session Types</h2>
            <q-space />
            <q-btn
                label="Add Session Type"
                color="primary"
                icon="add"
                dense
                padding="xs sm"
                @click="showAddDialog = true"
            />
        </div>

        <div
            v-if="isLoading"
            class="text-grey q-my-md"
        >
            Loading session types...
        </div>

        <template v-else>
            <!-- Session Types Table - wrapped for horizontal scroll on tablets -->
            <div class="table-scroll-container">
                <q-table
                    :rows="sessionTypes"
                    :columns="columns"
                    row-key="id"
                    dense
                    flat
                    bordered
                    :pagination="{ rowsPerPage: 0 }"
                    hide-pagination
                    :grid="$q.screen.lt.md"
                >
                    <!-- Column header tooltips -->
                    <template #header-cell-usesWeeks="props">
                        <q-th :props="props">
                            {{ props.col.label }}
                            <q-tooltip>Uses weeks (for clinical rotations) instead of hours</q-tooltip>
                        </q-th>
                    </template>
                    <template #header-cell-facultyCanEnter="props">
                        <q-th :props="props">
                            {{ props.col.label }}
                            <q-tooltip>Faculty can enter effort for this session type</q-tooltip>
                        </q-th>
                    </template>
                    <template #header-cell-allowedOnDvm="props">
                        <q-th :props="props">
                            {{ props.col.label }}
                            <q-tooltip>Allowed on DVM courses</q-tooltip>
                        </q-th>
                    </template>
                    <template #header-cell-allowedOn199299="props">
                        <q-th :props="props">
                            {{ props.col.label }}
                            <q-tooltip>Allowed on 199/299 courses</q-tooltip>
                        </q-th>
                    </template>
                    <template #header-cell-allowedOnRCourses="props">
                        <q-th :props="props">
                            {{ props.col.label }}
                            <q-tooltip>Allowed on R courses</q-tooltip>
                        </q-th>
                    </template>
                    <template #body-cell-description="props">
                        <q-td :props="props">
                            <template v-if="editingId === props.row.id">
                                <div class="row no-wrap q-gutter-sm">
                                    <q-input
                                        v-model="editDescription"
                                        dense
                                        outlined
                                        maxlength="50"
                                        hide-bottom-space
                                        style="min-width: 200px"
                                        :error="!!editError"
                                        :error-message="editError"
                                        @keyup.enter="saveEdit(props.row)"
                                        @keyup.escape="cancelEdit"
                                    />
                                    <q-btn
                                        icon="check"
                                        color="positive"
                                        dense
                                        flat
                                        round
                                        size="sm"
                                        aria-label="Save description"
                                        :loading="isSaving"
                                        @click="saveEdit(props.row)"
                                    >
                                        <q-tooltip>Save</q-tooltip>
                                    </q-btn>
                                    <q-btn
                                        icon="close"
                                        color="grey"
                                        dense
                                        flat
                                        round
                                        size="sm"
                                        aria-label="Cancel edit"
                                        @click="cancelEdit"
                                    >
                                        <q-tooltip>Cancel</q-tooltip>
                                    </q-btn>
                                </div>
                            </template>
                            <template v-else>
                                <div class="row items-center no-wrap">
                                    <span :class="{ 'text-grey': !props.row.isActive }">{{
                                        props.row.description
                                    }}</span>
                                    <q-btn
                                        icon="edit"
                                        color="primary"
                                        dense
                                        flat
                                        round
                                        size="sm"
                                        class="q-ml-sm"
                                        aria-label="Edit description"
                                        @click="startEdit(props.row)"
                                    >
                                        <q-tooltip>Edit description</q-tooltip>
                                    </q-btn>
                                </div>
                            </template>
                        </q-td>
                    </template>
                    <template #body-cell-usesWeeks="props">
                        <q-td
                            :props="props"
                            class="text-center"
                        >
                            <q-toggle
                                :model-value="props.row.usesWeeks"
                                dense
                                :disable="isTogglingId === props.row.id"
                                @update:model-value="toggleProperty(props.row, 'usesWeeks')"
                            >
                                <q-tooltip>{{
                                    props.row.usesWeeks ? "Uses weeks (clinical)" : "Uses hours"
                                }}</q-tooltip>
                            </q-toggle>
                        </q-td>
                    </template>
                    <template #body-cell-isActive="props">
                        <q-td
                            :props="props"
                            class="text-center"
                        >
                            <q-toggle
                                :model-value="props.row.isActive"
                                dense
                                :disable="isTogglingId === props.row.id"
                                @update:model-value="toggleProperty(props.row, 'isActive')"
                            />
                        </q-td>
                    </template>
                    <template #body-cell-facultyCanEnter="props">
                        <q-td
                            :props="props"
                            class="text-center"
                        >
                            <q-toggle
                                :model-value="props.row.facultyCanEnter"
                                dense
                                :disable="isTogglingId === props.row.id"
                                @update:model-value="toggleProperty(props.row, 'facultyCanEnter')"
                            />
                        </q-td>
                    </template>
                    <template #body-cell-allowedOnDvm="props">
                        <q-td
                            :props="props"
                            class="text-center"
                        >
                            <q-toggle
                                :model-value="props.row.allowedOnDvm"
                                dense
                                :disable="isTogglingId === props.row.id"
                                @update:model-value="toggleProperty(props.row, 'allowedOnDvm')"
                            />
                        </q-td>
                    </template>
                    <template #body-cell-allowedOn199299="props">
                        <q-td
                            :props="props"
                            class="text-center"
                        >
                            <q-toggle
                                :model-value="props.row.allowedOn199299"
                                dense
                                :disable="isTogglingId === props.row.id"
                                @update:model-value="toggleProperty(props.row, 'allowedOn199299')"
                            />
                        </q-td>
                    </template>
                    <template #body-cell-allowedOnRCourses="props">
                        <q-td
                            :props="props"
                            class="text-center"
                        >
                            <q-toggle
                                :model-value="props.row.allowedOnRCourses"
                                dense
                                :disable="isTogglingId === props.row.id"
                                @update:model-value="toggleProperty(props.row, 'allowedOnRCourses')"
                            />
                        </q-td>
                    </template>
                    <template #body-cell-actions="props">
                        <q-td :props="props">
                            <q-btn
                                icon="delete"
                                :color="props.row.canDelete ? 'negative' : 'grey'"
                                dense
                                flat
                                round
                                size="sm"
                                aria-label="Delete session type"
                                :disable="!props.row.canDelete"
                                :loading="isDeletingId === props.row.id"
                                @click="confirmDelete(props.row)"
                            >
                                <q-tooltip>{{
                                    props.row.canDelete
                                        ? "Delete session type"
                                        : `Cannot delete: session type is in use (${props.row.usageCount} references)`
                                }}</q-tooltip>
                            </q-btn>
                        </q-td>
                    </template>

                    <!-- Mobile card view -->
                    <template #item="props">
                        <div class="q-pa-xs col-12">
                            <q-card
                                flat
                                bordered
                                :class="{ 'bg-grey-2': !props.row.isActive }"
                            >
                                <q-card-section class="q-pa-sm">
                                    <!-- Header: ID, Description, Actions -->
                                    <div class="row items-center q-mb-sm">
                                        <div class="text-weight-bold q-mr-sm">{{ props.row.id }}</div>
                                        <!-- Edit mode for mobile -->
                                        <template v-if="editingId === props.row.id">
                                            <div class="col row no-wrap q-gutter-sm">
                                                <q-input
                                                    v-model="editDescription"
                                                    dense
                                                    outlined
                                                    maxlength="50"
                                                    hide-bottom-space
                                                    class="col"
                                                    :error="!!editError"
                                                    :error-message="editError"
                                                    @keyup.enter="saveEdit(props.row)"
                                                    @keyup.escape="cancelEdit"
                                                />
                                                <q-btn
                                                    icon="check"
                                                    color="positive"
                                                    dense
                                                    flat
                                                    round
                                                    size="sm"
                                                    aria-label="Save description"
                                                    :loading="isSaving"
                                                    @click="saveEdit(props.row)"
                                                />
                                                <q-btn
                                                    icon="close"
                                                    color="grey"
                                                    dense
                                                    flat
                                                    round
                                                    size="sm"
                                                    aria-label="Cancel edit"
                                                    @click="cancelEdit"
                                                />
                                            </div>
                                        </template>
                                        <!-- Display mode -->
                                        <template v-else>
                                            <div
                                                class="col text-grey-8"
                                                :class="{ 'text-grey': !props.row.isActive }"
                                            >
                                                {{ props.row.description }}
                                            </div>
                                            <q-btn
                                                icon="edit"
                                                color="primary"
                                                dense
                                                flat
                                                round
                                                size="sm"
                                                aria-label="Edit description"
                                                @click="startEdit(props.row)"
                                            />
                                            <q-btn
                                                icon="delete"
                                                :color="props.row.canDelete ? 'negative' : 'grey'"
                                                dense
                                                flat
                                                round
                                                size="sm"
                                                aria-label="Delete session type"
                                                :disable="!props.row.canDelete"
                                                :loading="isDeletingId === props.row.id"
                                                @click="confirmDelete(props.row)"
                                            />
                                        </template>
                                    </div>

                                    <!-- Toggles - flex wrap for responsive layout -->
                                    <div class="flex wrap items-center q-gutter-x-md q-gutter-y-xs">
                                        <q-toggle
                                            :model-value="props.row.isActive"
                                            label="Active"
                                            dense
                                            :disable="isTogglingId === props.row.id"
                                            @update:model-value="toggleProperty(props.row, 'isActive')"
                                        />
                                        <q-toggle
                                            :model-value="props.row.usesWeeks"
                                            label="Weeks"
                                            dense
                                            :disable="isTogglingId === props.row.id"
                                            @update:model-value="toggleProperty(props.row, 'usesWeeks')"
                                        />
                                        <q-toggle
                                            :model-value="props.row.facultyCanEnter"
                                            label="Faculty"
                                            dense
                                            :disable="isTogglingId === props.row.id"
                                            @update:model-value="toggleProperty(props.row, 'facultyCanEnter')"
                                        />
                                        <q-toggle
                                            :model-value="props.row.allowedOnDvm"
                                            label="DVM"
                                            dense
                                            :disable="isTogglingId === props.row.id"
                                            @update:model-value="toggleProperty(props.row, 'allowedOnDvm')"
                                        />
                                        <q-toggle
                                            :model-value="props.row.allowedOn199299"
                                            label="199/299"
                                            dense
                                            :disable="isTogglingId === props.row.id"
                                            @update:model-value="toggleProperty(props.row, 'allowedOn199299')"
                                        />
                                        <q-toggle
                                            :model-value="props.row.allowedOnRCourses"
                                            label="R Courses"
                                            dense
                                            :disable="isTogglingId === props.row.id"
                                            @update:model-value="toggleProperty(props.row, 'allowedOnRCourses')"
                                        />
                                    </div>

                                    <!-- Usage count -->
                                    <div
                                        v-if="props.row.usageCount > 0"
                                        class="text-caption text-grey-6 q-mt-xs"
                                    >
                                        Used in {{ props.row.usageCount }} session(s)
                                    </div>
                                </q-card-section>
                            </q-card>
                        </div>
                    </template>
                </q-table>
            </div>

            <div
                v-if="sessionTypes.length === 0"
                class="text-grey q-my-md"
            >
                No session types found.
            </div>
        </template>

        <!-- Add Session Type Dialog -->
        <q-dialog
            v-model="showAddDialog"
            persistent
        >
            <q-card style="min-width: min(450px, 90vw)">
                <q-card-section class="row items-center q-pb-none">
                    <div class="text-h6">Add New Session Type</div>
                    <q-space />
                    <q-btn
                        v-close-popup
                        icon="close"
                        flat
                        round
                        dense
                        aria-label="Close add session type dialog"
                    />
                </q-card-section>

                <q-card-section class="q-pt-none q-gutter-sm">
                    <q-input
                        ref="newIdInput"
                        v-model="newId"
                        label="ID (e.g., LEC, LAB, CLI) *"
                        dense
                        outlined
                        maxlength="3"
                        :error="!!newIdError"
                        :error-message="newIdError"
                        hint="1-3 uppercase characters"
                        @keyup.enter="addSessionType"
                    />
                    <q-input
                        v-model="newDescription"
                        label="Description *"
                        dense
                        outlined
                        maxlength="50"
                        :error="!!newDescriptionError"
                        :error-message="newDescriptionError"
                        @keyup.enter="addSessionType"
                    />
                    <q-toggle
                        v-model="newUsesWeeks"
                        label="Uses weeks (for clinical)"
                        dense
                    />
                    <div class="text-subtitle2 q-mt-md">Permissions</div>
                    <q-toggle
                        v-model="newFacultyCanEnter"
                        label="Faculty can enter"
                        dense
                    />
                    <div class="text-subtitle2 q-mt-md">Allowed on course types</div>
                    <q-toggle
                        v-model="newAllowedOnDvm"
                        label="DVM courses"
                        dense
                    />
                    <q-toggle
                        v-model="newAllowedOn199299"
                        label="199/299 courses"
                        dense
                    />
                    <q-toggle
                        v-model="newAllowedOnRCourses"
                        label="R courses"
                        dense
                    />
                </q-card-section>

                <q-card-actions align="right">
                    <q-btn
                        label="Cancel"
                        flat
                        dense
                        @click="closeAddDialog"
                    />
                    <q-btn
                        label="Add"
                        color="primary"
                        dense
                        :loading="isAdding"
                        @click="addSessionType"
                    />
                </q-card-actions>
            </q-card>
        </q-dialog>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, nextTick } from "vue"
import { useQuasar } from "quasar"
import { sessionTypeService } from "../services/session-type-service"
import type { SessionTypeDto } from "../types"
import type { QTableColumn, QInput } from "quasar"

const $q = useQuasar()

// State
const sessionTypes = ref<SessionTypeDto[]>([])
const isLoading = ref(false)

// Add new session type
const showAddDialog = ref(false)
const newId = ref("")
const newDescription = ref("")
const newUsesWeeks = ref(false)
const newFacultyCanEnter = ref(true)
const newAllowedOnDvm = ref(true)
const newAllowedOn199299 = ref(true)
const newAllowedOnRCourses = ref(true)
const newIdError = ref("")
const newDescriptionError = ref("")
const isAdding = ref(false)
const newIdInput = ref<QInput | null>(null)

// Focus the input when the add dialog opens
watch(showAddDialog, (isOpen) => {
    if (isOpen) {
        nextTick(() => newIdInput.value?.focus())
    }
})

// Edit
const editingId = ref<string | null>(null)
const editDescription = ref("")
const editError = ref("")
const isSaving = ref(false)

// Toggle active/flags
const isTogglingId = ref<string | null>(null)

// Delete
const isDeletingId = ref<string | null>(null)

const columns: QTableColumn[] = [
    { name: "id", label: "ID", field: "id", align: "left", sortable: true, style: "width: 70px" },
    { name: "description", label: "Description", field: "description", align: "left", sortable: true },
    { name: "usesWeeks", label: "Weeks", field: "usesWeeks", align: "center", style: "width: 80px" },
    { name: "isActive", label: "Active", field: "isActive", align: "center", style: "width: 80px" },
    { name: "facultyCanEnter", label: "Faculty", field: "facultyCanEnter", align: "center", style: "width: 80px" },
    { name: "allowedOnDvm", label: "DVM", field: "allowedOnDvm", align: "center", style: "width: 80px" },
    { name: "allowedOn199299", label: "199/299", field: "allowedOn199299", align: "center", style: "width: 80px" },
    {
        name: "allowedOnRCourses",
        label: "R Courses",
        field: "allowedOnRCourses",
        align: "center",
        style: "width: 90px",
    },
    { name: "actions", label: "Actions", field: "actions", align: "center", style: "width: 80px" },
]

async function loadSessionTypes() {
    isLoading.value = true
    try {
        sessionTypes.value = await sessionTypeService.getSessionTypes()
    } finally {
        isLoading.value = false
    }
}

function resetAddForm() {
    newId.value = ""
    newDescription.value = ""
    newUsesWeeks.value = false
    newFacultyCanEnter.value = true
    newAllowedOnDvm.value = true
    newAllowedOn199299.value = true
    newAllowedOnRCourses.value = true
    newIdError.value = ""
    newDescriptionError.value = ""
}

function closeAddDialog() {
    showAddDialog.value = false
    resetAddForm()
}

async function addSessionType() {
    newIdError.value = ""
    newDescriptionError.value = ""

    // Validate required fields
    let hasError = false
    if (!newId.value.trim()) {
        newIdError.value = "ID is required"
        hasError = true
    }
    if (!newDescription.value.trim()) {
        newDescriptionError.value = "Description is required"
        hasError = true
    }
    if (hasError) return

    isAdding.value = true

    try {
        const result = await sessionTypeService.createSessionType({
            id: newId.value.trim().toUpperCase(),
            description: newDescription.value.trim(),
            usesWeeks: newUsesWeeks.value,
            facultyCanEnter: newFacultyCanEnter.value,
            allowedOnDvm: newAllowedOnDvm.value,
            allowedOn199299: newAllowedOn199299.value,
            allowedOnRCourses: newAllowedOnRCourses.value,
        })
        if (result.success && result.sessionType) {
            $q.notify({ type: "positive", message: "Session type added successfully" })
            closeAddDialog()
            await loadSessionTypes()
        } else {
            newIdError.value = result.error || "Failed to add session type"
        }
    } finally {
        isAdding.value = false
    }
}

function updateSessionTypeInList(updatedSessionType: SessionTypeDto) {
    const index = sessionTypes.value.findIndex((s) => s.id === updatedSessionType.id)
    if (index !== -1) {
        sessionTypes.value[index] = updatedSessionType
    }
}

function startEdit(sessionType: SessionTypeDto) {
    editingId.value = sessionType.id
    editDescription.value = sessionType.description
    editError.value = ""
}

function cancelEdit() {
    editingId.value = null
    editDescription.value = ""
    editError.value = ""
}

async function saveEdit(sessionType: SessionTypeDto) {
    if (!editDescription.value.trim()) {
        editError.value = "Description is required"
        return
    }

    editError.value = ""
    isSaving.value = true

    try {
        const result = await sessionTypeService.updateSessionType(sessionType.id, {
            description: editDescription.value.trim(),
            usesWeeks: sessionType.usesWeeks,
            isActive: sessionType.isActive,
            facultyCanEnter: sessionType.facultyCanEnter,
            allowedOnDvm: sessionType.allowedOnDvm,
            allowedOn199299: sessionType.allowedOn199299,
            allowedOnRCourses: sessionType.allowedOnRCourses,
        })
        if (result.success && result.sessionType) {
            updateSessionTypeInList(result.sessionType)
            $q.notify({ type: "positive", message: "Session type updated successfully" })
            cancelEdit()
        } else {
            editError.value = result.error || "Failed to update session type"
        }
    } finally {
        isSaving.value = false
    }
}

type ToggleableProperty =
    | "usesWeeks"
    | "isActive"
    | "facultyCanEnter"
    | "allowedOnDvm"
    | "allowedOn199299"
    | "allowedOnRCourses"

async function toggleProperty(sessionType: SessionTypeDto, property: ToggleableProperty) {
    isTogglingId.value = sessionType.id

    try {
        const result = await sessionTypeService.updateSessionType(sessionType.id, {
            description: sessionType.description,
            usesWeeks: sessionType.usesWeeks,
            isActive: sessionType.isActive,
            facultyCanEnter: sessionType.facultyCanEnter,
            allowedOnDvm: sessionType.allowedOnDvm,
            allowedOn199299: sessionType.allowedOn199299,
            allowedOnRCourses: sessionType.allowedOnRCourses,
            [property]: !sessionType[property],
        })
        if (result.success && result.sessionType) {
            updateSessionTypeInList(result.sessionType)
            $q.notify({ type: "positive", message: "Session type updated successfully" })
        } else {
            $q.notify({ type: "negative", message: result.error || "Failed to update session type" })
        }
    } finally {
        isTogglingId.value = null
    }
}

function confirmDelete(sessionType: SessionTypeDto) {
    $q.dialog({
        title: "Delete Session Type",
        message: `Are you sure you want to delete "${sessionType.id} - ${sessionType.description}"? This cannot be undone.`,
        cancel: true,
        persistent: true,
    }).onOk(async () => {
        await deleteSessionType(sessionType)
    })
}

async function deleteSessionType(sessionType: SessionTypeDto) {
    isDeletingId.value = sessionType.id

    try {
        const success = await sessionTypeService.deleteSessionType(sessionType.id)
        if (success) {
            $q.notify({ type: "positive", message: "Session type deleted successfully" })
            await loadSessionTypes()
        } else {
            $q.notify({ type: "negative", message: "Failed to delete session type" })
        }
    } finally {
        isDeletingId.value = null
    }
}

onMounted(loadSessionTypes)
</script>

<style scoped>
.table-scroll-container {
    overflow-x: auto;
}
</style>
