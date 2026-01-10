<template>
    <div class="q-pa-md">
        <div class="row items-center q-mb-md">
            <h2 class="q-ma-none">Manage Effort Types</h2>
            <q-space />
            <q-btn
                label="Add Effort Type"
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
            Loading effort types...
        </div>

        <template v-else>
            <!-- Effort Types Table - wrapped for horizontal scroll on tablets -->
            <div class="table-scroll-container">
                <q-table
                    :rows="effortTypes"
                    :columns="columns"
                    :visible-columns="visibleColumns"
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
                            <span class="has-tooltip">{{ props.col.label }}</span>
                            <q-tooltip>Effort is tracked in weeks instead of hours</q-tooltip>
                        </q-th>
                    </template>
                    <template #header-cell-facultyCanEnter="props">
                        <q-th :props="props">
                            <span class="has-tooltip">{{ props.col.label }}</span>
                            <q-tooltip>Faculty can enter effort for this effort type</q-tooltip>
                        </q-th>
                    </template>
                    <template #header-cell-allowedOnDvm="props">
                        <q-th :props="props">
                            <span class="has-tooltip">{{ props.col.label }}</span>
                            <q-tooltip>Allowed on DVM courses</q-tooltip>
                        </q-th>
                    </template>
                    <template #header-cell-allowedOn199299="props">
                        <q-th :props="props">
                            <span class="has-tooltip">{{ props.col.label }}</span>
                            <q-tooltip>Allowed on 199/299 courses</q-tooltip>
                        </q-th>
                    </template>
                    <template #header-cell-allowedOnRCourses="props">
                        <q-th :props="props">
                            <span class="has-tooltip">{{ props.col.label }}</span>
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
                                aria-label="Delete effort type"
                                :disable="!props.row.canDelete"
                                :loading="isDeletingId === props.row.id"
                                @click="confirmDelete(props.row)"
                            >
                                <q-tooltip>{{
                                    props.row.canDelete
                                        ? "Delete effort type"
                                        : `Cannot delete: effort type is in use (${props.row.usageCount} references)`
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
                                                aria-label="Delete effort type"
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
                                        Used in {{ props.row.usageCount }} record(s)
                                    </div>
                                </q-card-section>
                            </q-card>
                        </div>
                    </template>
                </q-table>
            </div>

            <div
                v-if="effortTypes.length === 0"
                class="text-grey q-my-md"
            >
                No effort types found.
            </div>
        </template>

        <!-- Add Effort Type Dialog -->
        <q-dialog
            v-model="showAddDialog"
            persistent
        >
            <q-card style="min-width: min(450px, 90vw)">
                <q-card-section class="row items-center q-pb-none">
                    <div class="text-h6">Add New Effort Type</div>
                    <q-space />
                    <q-btn
                        v-close-popup
                        icon="close"
                        flat
                        round
                        dense
                        aria-label="Close add effort type dialog"
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
                        @keyup.enter="addEffortType"
                    />
                    <q-input
                        v-model="newDescription"
                        label="Description *"
                        dense
                        outlined
                        maxlength="50"
                        :error="!!newDescriptionError"
                        :error-message="newDescriptionError"
                        @keyup.enter="addEffortType"
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
                        @click="addEffortType"
                    />
                </q-card-actions>
            </q-card>
        </q-dialog>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, nextTick, computed } from "vue"
import { useQuasar } from "quasar"
import { effortTypeService } from "../services/effort-type-service"
import type { EffortTypeDto } from "../types"
import type { QTableColumn, QInput } from "quasar"

const $q = useQuasar()

// State
const effortTypes = ref<EffortTypeDto[]>([])
const isLoading = ref(false)

// Add new effort type
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
    { name: "isActive", label: "Active", field: "isActive", align: "center", style: "width: 80px" },
    { name: "usageCount", label: "Usage", field: "usageCount", align: "right", sortable: true, style: "width: 80px" },
    { name: "actions", label: "Actions", field: "actions", align: "center", style: "width: 80px" },
]

// Hide Usage column on smaller screens (iPad mini landscape is ~1024px)
const visibleColumns = computed(() => {
    const allColumns = columns.map((c) => c.name)
    if ($q.screen.lt.lg) {
        return allColumns.filter((name) => name !== "usageCount")
    }
    return allColumns
})

async function loadEffortTypes() {
    isLoading.value = true
    try {
        effortTypes.value = await effortTypeService.getEffortTypes()
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

async function addEffortType() {
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
        const result = await effortTypeService.createEffortType({
            id: newId.value.trim().toUpperCase(),
            description: newDescription.value.trim(),
            usesWeeks: newUsesWeeks.value,
            facultyCanEnter: newFacultyCanEnter.value,
            allowedOnDvm: newAllowedOnDvm.value,
            allowedOn199299: newAllowedOn199299.value,
            allowedOnRCourses: newAllowedOnRCourses.value,
        })
        if (result.success && result.effortType) {
            $q.notify({ type: "positive", message: "Effort type added successfully" })
            closeAddDialog()
            await loadEffortTypes()
        } else {
            newIdError.value = result.error || "Failed to add effort type"
        }
    } finally {
        isAdding.value = false
    }
}

function updateEffortTypeInList(updatedEffortType: EffortTypeDto) {
    const index = effortTypes.value.findIndex((s) => s.id === updatedEffortType.id)
    if (index !== -1) {
        effortTypes.value[index] = updatedEffortType
    }
}

function startEdit(effortType: EffortTypeDto) {
    editingId.value = effortType.id
    editDescription.value = effortType.description
    editError.value = ""
}

function cancelEdit() {
    editingId.value = null
    editDescription.value = ""
    editError.value = ""
}

async function saveEdit(effortType: EffortTypeDto) {
    if (!editDescription.value.trim()) {
        editError.value = "Description is required"
        return
    }

    editError.value = ""
    isSaving.value = true

    try {
        const result = await effortTypeService.updateEffortType(effortType.id, {
            description: editDescription.value.trim(),
            usesWeeks: effortType.usesWeeks,
            isActive: effortType.isActive,
            facultyCanEnter: effortType.facultyCanEnter,
            allowedOnDvm: effortType.allowedOnDvm,
            allowedOn199299: effortType.allowedOn199299,
            allowedOnRCourses: effortType.allowedOnRCourses,
        })
        if (result.success && result.effortType) {
            updateEffortTypeInList(result.effortType)
            $q.notify({ type: "positive", message: "Effort type updated successfully" })
            cancelEdit()
        } else {
            editError.value = result.error || "Failed to update effort type"
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

async function toggleProperty(effortType: EffortTypeDto, property: ToggleableProperty) {
    isTogglingId.value = effortType.id

    try {
        const result = await effortTypeService.updateEffortType(effortType.id, {
            description: effortType.description,
            usesWeeks: effortType.usesWeeks,
            isActive: effortType.isActive,
            facultyCanEnter: effortType.facultyCanEnter,
            allowedOnDvm: effortType.allowedOnDvm,
            allowedOn199299: effortType.allowedOn199299,
            allowedOnRCourses: effortType.allowedOnRCourses,
            [property]: !effortType[property],
        })
        if (result.success && result.effortType) {
            updateEffortTypeInList(result.effortType)
            $q.notify({ type: "positive", message: "Effort type updated successfully" })
        } else {
            $q.notify({ type: "negative", message: result.error || "Failed to update effort type" })
        }
    } finally {
        isTogglingId.value = null
    }
}

function confirmDelete(effortType: EffortTypeDto) {
    $q.dialog({
        title: "Delete Effort Type",
        message: `Are you sure you want to delete "${effortType.id} - ${effortType.description}"? This cannot be undone.`,
        cancel: true,
        persistent: true,
    }).onOk(async () => {
        await deleteEffortType(effortType)
    })
}

async function deleteEffortType(effortType: EffortTypeDto) {
    isDeletingId.value = effortType.id

    try {
        const success = await effortTypeService.deleteEffortType(effortType.id)
        if (success) {
            $q.notify({ type: "positive", message: "Effort type deleted successfully" })
            await loadEffortTypes()
        } else {
            $q.notify({ type: "negative", message: "Failed to delete effort type" })
        }
    } finally {
        isDeletingId.value = null
    }
}

onMounted(loadEffortTypes)
</script>

<style scoped>
.table-scroll-container {
    overflow-x: auto;
}

.has-tooltip {
    border-bottom: 1px dotted currentColor;
    cursor: help;
}
</style>
