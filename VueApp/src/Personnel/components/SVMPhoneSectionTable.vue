<template>
    <q-table
        :rows="section.rows"
        :columns="section.cols"
        row-key="entryId"
        dense
        :hide-pagination="true"
        v-model:pagination="pagination"
        :filter="search"
        :loading="loading"
    >
        <template
            #top-left
            v-if="isModify"
        >
            <div class="row items-center q-gutter-sm">
                <div class="q-table__title">
                    {{ section.title }}
                    <q-btn
                        type="button"
                        color="primary"
                        dense
                        no-caps
                        @click="$emit('addRecord', section.title, section.id)"
                        icon="add"
                        size="xs"
                    />
                </div>
            </div>
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
import type { SVMPhoneSection } from "../types/svm-phone-types"

defineProps<{ section: SVMPhoneSection; loading: boolean; isModify: boolean; search: string }>()
defineEmits(["addRecord", "editRecord", "deleteRecord"])
const pagination = ref({ rowsPerPage: 0 }) as Ref<QTableProps["pagination"]>
</script>
