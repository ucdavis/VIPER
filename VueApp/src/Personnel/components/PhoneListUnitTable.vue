<template>
    <div class="q-mb-md">
        <!-- Desktop: table -->
        <q-table
            class="gt-sm"
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
                            :aria-label="`Add to ${unit.name}`"
                            @click="$emit('addRecord', unit)"
                            icon="add"
                            size="xs"
                        />
                    </div>
                </div>
            </template>
            <template
                #body-cell-name="nameProps"
                v-if="!isMaintain"
            >
                <q-td
                    :props="nameProps"
                    v-if="nameProps.row.employeeMailId !== ''"
                >
                    <a :href="`mailto:${nameProps.row.employeeMailId}@ucdavis.edu`">{{ nameProps.row.name }}</a>
                </q-td>
                <q-td
                    :props="nameProps"
                    v-else
                >
                    {{ nameProps.row.name }}
                </q-td>
            </template>
            <template #body-cell-listFirst="listFirstProps">
                <q-td :props="listFirstProps">
                    <q-icon
                        v-if="listFirstProps.row.listFirst"
                        name="check"
                    ></q-icon>
                </q-td>
            </template>
            <template #body-cell-edit="cell">
                <RecordActionCell
                    action="edit"
                    :cell="cell"
                    @action="$emit('editRecord', cell.row)"
                />
            </template>
            <template #body-cell-delete="cell">
                <RecordActionCell
                    action="delete"
                    :cell="cell"
                    @action="$emit('deleteRecord', cell.row)"
                />
            </template>
        </q-table>

        <MobileCardList
            v-model:pagination="pagination"
            :title="unit.name"
            :columns="unit.cols"
            :rows="unit.rows"
            :search="search"
            :loading="loading"
            row-key="unitPersonId"
            :omit-columns="['name', 'edit', 'delete']"
            empty-message="No records to display."
        >
            <template #title-append>
                <q-btn
                    v-if="isMaintain"
                    type="button"
                    color="primary"
                    dense
                    no-caps
                    :aria-label="`Add to ${unit.name}`"
                    @click="$emit('addRecord', unit)"
                    icon="add"
                    size="xs"
                />
            </template>
            <!-- Mailed from the read-only list only, as on desktop: the maintain view is for
                 editing the record, not for contacting the person. -->
            <template #card-title="{ row }">
                <a
                    v-if="!isMaintain && row.employeeMailId !== ''"
                    :href="`mailto:${row.employeeMailId}@ucdavis.edu`"
                    >{{ row.name }}</a
                >
                <template v-else>{{ row.name }}</template>
            </template>
            <template
                v-if="isMaintain"
                #card-actions="{ row }"
            >
                <RecordActionButton
                    action="edit"
                    @action="$emit('editRecord', row)"
                />
                <RecordActionButton
                    action="delete"
                    @action="$emit('deleteRecord', row)"
                />
            </template>
        </MobileCardList>
    </div>
</template>

<script setup lang="ts">
import { ref } from "vue"
import MobileCardList from "./MobileCardList.vue"
import RecordActionButton from "./RecordActionButton.vue"
import RecordActionCell from "./RecordActionCell.vue"
import type { Ref } from "vue"
import type { QTableProps } from "quasar"
import type { PhoneListUnit } from "../types/phone-list-phone-types"

defineProps<{ unit: PhoneListUnit; loading: boolean; isMaintain: boolean; search: string }>()
defineEmits(["addRecord", "editRecord", "deleteRecord"])
// Bound to the table, and shared with the card list's sort control.
const pagination: Ref<QTableProps["pagination"]> = ref({ rowsPerPage: 0, sortBy: null, descending: false })
</script>
