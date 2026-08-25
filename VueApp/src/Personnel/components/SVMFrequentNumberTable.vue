<template>
    <q-table
        :rows="frequentNumbers"
        :columns="cols"
        row-key="entryId"
        dense
        :hide-pagination="true"
        v-model:pagination="pagination"
        :filter="search"
        :loading="loading"
    >
        <template
            v-if="editRecords"
            #top-left
        >
            <div class="row items-center q-gutter-sm">
                <div class="q-table__title">
                    Frequently Called Numbers
                    <q-btn
                        type="button"
                        color="primary"
                        dense
                        no-caps
                        @click="$emit('addFrequentNumber')"
                        icon="add"
                        size="xs"
                    />
                </div>
            </div>
        </template>
        <template #body-cell-edit="editProps">
            <q-td :props="editProps">
                <RecordActionButton
                    action="edit"
                    @action="$emit('editFrequentNumber', editProps.row)"
                />
            </q-td>
        </template>
        <template #body-cell-delete="deleteProps">
            <q-td :props="deleteProps">
                <RecordActionButton
                    action="delete"
                    @action="$emit('deleteFrequentNumber', deleteProps.row)"
                />
            </q-td>
        </template>
    </q-table>
</template>

<script setup lang="ts">
import { ref } from "vue"
import RecordActionButton from "./RecordActionButton.vue"
import type { Ref } from "vue"
import type { QTableProps } from "quasar"
import type { SVMFrequentNumberRecord } from "../types/svm-phone-types"

const props = defineProps<{
    frequentNumbers: SVMFrequentNumberRecord[]
    loading: boolean
    editRecords: boolean
    search: string
}>()
defineEmits(["addFrequentNumber", "editFrequentNumber", "deleteFrequentNumber"])
const pagination = ref({ rowsPerPage: 0 }) as Ref<QTableProps["pagination"]>
const cols: QTableProps["columns"] = [
    { name: "label", label: "Location", field: "label", align: "left", sortable: true },
    { name: "phone", label: "Phone", field: "phone", align: "left" },
]
if (props.editRecords) {
    cols.push({ name: "edit", label: "Edit", field: "edit", align: "left" })
    cols.push({ name: "delete", label: "Delete", field: "delete", align: "left" })
}
</script>
