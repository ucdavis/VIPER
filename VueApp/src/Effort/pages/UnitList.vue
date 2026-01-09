<template>
    <div class="q-pa-md">
        <div class="row items-center q-mb-md">
            <h2 class="q-ma-none">Manage Units</h2>
            <q-space />
            <q-btn
                label="Add Unit"
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
            Loading units...
        </div>

        <template v-else>
            <!-- Units Table -->
            <q-table
                :rows="units"
                :columns="columns"
                row-key="id"
                dense
                flat
                bordered
                :pagination="{ rowsPerPage: 0 }"
                hide-pagination
            >
                <template #body-cell-name="props">
                    <q-td :props="props">
                        <template v-if="editingId === props.row.id">
                            <div class="row no-wrap q-gutter-sm">
                                <q-input
                                    v-model="editName"
                                    dense
                                    outlined
                                    maxlength="20"
                                    hide-bottom-space
                                    style="min-width: 150px"
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
                                    aria-label="Save unit name"
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
                                <span :class="{ 'text-grey': !props.row.isActive }">{{ props.row.name }}</span>
                                <q-btn
                                    icon="edit"
                                    color="primary"
                                    dense
                                    flat
                                    round
                                    size="sm"
                                    class="q-ml-sm"
                                    aria-label="Edit unit name"
                                    @click="startEdit(props.row)"
                                >
                                    <q-tooltip>Edit name</q-tooltip>
                                </q-btn>
                            </div>
                        </template>
                    </q-td>
                </template>
                <template #body-cell-isActive="props">
                    <q-td :props="props">
                        <q-toggle
                            :model-value="props.row.isActive"
                            dense
                            :disable="isTogglingId === props.row.id"
                            @update:model-value="toggleActive(props.row)"
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
                            aria-label="Delete unit"
                            :disable="!props.row.canDelete"
                            :loading="isDeletingId === props.row.id"
                            @click="confirmDelete(props.row)"
                        >
                            <q-tooltip>{{
                                props.row.canDelete
                                    ? "Delete unit"
                                    : `Cannot delete: unit is in use (${props.row.usageCount} references)`
                            }}</q-tooltip>
                        </q-btn>
                    </q-td>
                </template>
            </q-table>

            <div
                v-if="units.length === 0"
                class="text-grey q-my-md"
            >
                No units found.
            </div>
        </template>

        <!-- Add Unit Dialog -->
        <q-dialog
            v-model="showAddDialog"
            persistent
        >
            <q-card style="min-width: 350px">
                <q-card-section class="row items-center q-pb-none">
                    <div class="text-h6">Add New Unit</div>
                    <q-space />
                    <q-btn
                        v-close-popup
                        icon="close"
                        flat
                        round
                        dense
                        aria-label="Close add unit dialog"
                    />
                </q-card-section>

                <q-card-section class="q-pt-none">
                    <q-input
                        ref="newUnitInput"
                        v-model="newUnitName"
                        label="Unit Name *"
                        dense
                        outlined
                        maxlength="20"
                        :error="!!newUnitError"
                        :error-message="newUnitError"
                        @keyup.enter="addUnit"
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
                        @click="addUnit"
                    />
                </q-card-actions>
            </q-card>
        </q-dialog>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, nextTick } from "vue"
import { useQuasar } from "quasar"
import { unitService } from "../services/unit-service"
import type { UnitDto } from "../types"
import type { QTableColumn, QInput } from "quasar"

const $q = useQuasar()

// State
const units = ref<UnitDto[]>([])
const isLoading = ref(false)

// Add new unit
const showAddDialog = ref(false)
const newUnitName = ref("")
const newUnitError = ref("")
const isAdding = ref(false)
const newUnitInput = ref<QInput | null>(null)

// Focus the input when the add dialog opens
watch(showAddDialog, (isOpen) => {
    if (isOpen) {
        nextTick(() => newUnitInput.value?.focus())
    }
})

// Edit
const editingId = ref<number | null>(null)
const editName = ref("")
const editError = ref("")
const isSaving = ref(false)

// Toggle active
const isTogglingId = ref<number | null>(null)

// Delete
const isDeletingId = ref<number | null>(null)

const columns: QTableColumn[] = [
    { name: "name", label: "Name", field: "name", align: "left", sortable: true },
    { name: "isActive", label: "Active", field: "isActive", align: "center", style: "width: 100px" },
    { name: "usageCount", label: "Usage", field: "usageCount", align: "right", sortable: true, style: "width: 80px" },
    { name: "actions", label: "Actions", field: "actions", align: "center", style: "width: 80px" },
]

async function loadUnits() {
    isLoading.value = true
    try {
        units.value = await unitService.getUnits()
    } finally {
        isLoading.value = false
    }
}

function closeAddDialog() {
    showAddDialog.value = false
    newUnitName.value = ""
    newUnitError.value = ""
}

async function addUnit() {
    newUnitError.value = ""

    // Validate required field
    if (!newUnitName.value.trim()) {
        newUnitError.value = "Unit name is required"
        return
    }

    isAdding.value = true

    try {
        const result = await unitService.createUnit({ name: newUnitName.value.trim() })
        if (result.success && result.unit) {
            $q.notify({ type: "positive", message: "Unit added successfully" })
            closeAddDialog()
            await loadUnits()
        } else {
            newUnitError.value = result.error || "Failed to add unit"
        }
    } finally {
        isAdding.value = false
    }
}

function updateUnitInList(updatedUnit: UnitDto) {
    const index = units.value.findIndex((u) => u.id === updatedUnit.id)
    if (index !== -1) {
        units.value[index] = updatedUnit
    }
}

function startEdit(unit: UnitDto) {
    editingId.value = unit.id
    editName.value = unit.name
    editError.value = ""
}

function cancelEdit() {
    editingId.value = null
    editName.value = ""
    editError.value = ""
}

async function saveEdit(unit: UnitDto) {
    if (!editName.value.trim()) {
        editError.value = "Name is required"
        return
    }

    editError.value = ""
    isSaving.value = true

    try {
        const result = await unitService.updateUnit(unit.id, {
            name: editName.value.trim(),
            isActive: unit.isActive,
        })
        if (result.success && result.unit) {
            updateUnitInList(result.unit)
            $q.notify({ type: "positive", message: "Unit updated successfully" })
            cancelEdit()
        } else {
            editError.value = result.error || "Failed to update unit"
        }
    } finally {
        isSaving.value = false
    }
}

async function toggleActive(unit: UnitDto) {
    isTogglingId.value = unit.id

    try {
        const result = await unitService.updateUnit(unit.id, {
            name: unit.name,
            isActive: !unit.isActive,
        })
        if (result.success && result.unit) {
            updateUnitInList(result.unit)
            $q.notify({
                type: "positive",
                message: `Unit ${result.unit.isActive ? "activated" : "deactivated"} successfully`,
            })
        } else {
            $q.notify({ type: "negative", message: result.error || "Failed to update unit" })
        }
    } finally {
        isTogglingId.value = null
    }
}

function confirmDelete(unit: UnitDto) {
    $q.dialog({
        title: "Delete Unit",
        message: `Are you sure you want to delete "${unit.name}"? This cannot be undone.`,
        cancel: true,
        persistent: true,
    }).onOk(async () => {
        await deleteUnit(unit)
    })
}

async function deleteUnit(unit: UnitDto) {
    isDeletingId.value = unit.id

    try {
        const success = await unitService.deleteUnit(unit.id)
        if (success) {
            $q.notify({ type: "positive", message: "Unit deleted successfully" })
            await loadUnits()
        } else {
            $q.notify({ type: "negative", message: "Failed to delete unit" })
        }
    } finally {
        isDeletingId.value = null
    }
}

onMounted(loadUnits)
</script>
