<template>
    <q-table
        :rows="unit.rows"
        :columns="unit.cols"
        row-key="unitPersonId"
        dense
        :hide-pagination="true"
        v-model:pagination="pagination"
        :filter="search"
        :title="unit.name"
        :loading="loading"
    >
        <template
            v-if="isMaintain"
            #top-left
        >
            <div class="row items-center q-gutter-sm">
                <div class="q-table__title">
                    {{ unit.name }}
                    <q-btn
                        type="button"
                        color="primary"
                        dense
                        no-caps
                        @click="$emit('addRecord', unit)"
                        icon="add"
                        size="xs"
                    />
                </div>
            </div>
        </template>
        <template
            #body-cell-name="props"
            v-if="!isMaintain"
        >
            <q-td
                :props="props"
                v-if="props.row.employeeMailId !== ''"
            >
                <a :href="`mailto:${props.row.employeeMailId}@ucdavis.edu`">{{ props.row.name }}</a>
            </q-td>
            <q-td
                :props="props"
                v-else
            >
                {{ props.row.name }}
            </q-td>
        </template>
        <template #body-cell-listFirst="props">
            <q-td :props="props">
                <q-icon
                    v-if="props.row.listFirst"
                    name="check"
                ></q-icon>
            </q-td>
        </template>
        <template #body-cell-edit="props">
            <q-td :props="props">
                <RecordActionButton
                    action="edit"
                    @action="$emit('editRecord', props.row)"
                />
            </q-td>
        </template>
        <template #body-cell-delete="props">
            <q-td :props="props">
                <RecordActionButton
                    action="delete"
                    @action="$emit('deleteRecord', props.row)"
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
import type { PhoneListUnit } from "../types/phone-list-phone-types"

defineProps<{ unit: PhoneListUnit; loading: boolean; isMaintain: boolean; search: string }>()
defineEmits(["addRecord", "editRecord", "deleteRecord"])
const pagination = ref({ rowsPerPage: 0 }) as Ref<QTableProps["pagination"]>
</script>
