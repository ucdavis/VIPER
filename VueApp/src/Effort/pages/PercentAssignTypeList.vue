<template>
    <div class="q-pa-md">
        <h2>Percent Assignment Types</h2>

        <div
            v-if="isLoading"
            class="text-grey q-my-md"
        >
            Loading percent assignment types...
        </div>

        <template v-else>
            <!-- Grouped by Class -->
            <div
                v-for="classGroup in groupedTypes"
                :key="classGroup.className"
                class="q-mb-lg"
            >
                <div class="text-h6 q-mb-sm">{{ classGroup.className }}</div>
                <q-table
                    :rows="classGroup.types"
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
                            <router-link
                                :to="getInstructorsLink(props.row.id)"
                                class="text-primary"
                            >
                                {{ props.row.name }}
                            </router-link>
                        </q-td>
                    </template>
                    <template #body-cell-showOnTemplate="props">
                        <q-td :props="props">
                            <q-icon
                                :name="props.row.showOnTemplate ? 'check_circle' : 'cancel'"
                                :color="props.row.showOnTemplate ? 'positive' : 'grey'"
                                size="sm"
                            />
                        </q-td>
                    </template>
                    <template #body-cell-instructorCount="props">
                        <q-td :props="props">
                            <router-link
                                :to="getInstructorsLink(props.row.id)"
                                class="text-primary"
                            >
                                {{ props.row.instructorCount }}
                            </router-link>
                        </q-td>
                    </template>
                </q-table>
            </div>

            <div
                v-if="groupedTypes.length === 0"
                class="text-grey q-my-md"
            >
                No percent assignment types found.
            </div>
        </template>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue"
import { useRoute } from "vue-router"
import { useQuasar } from "quasar"
import { percentAssignTypeService } from "../services/percent-assign-type-service"
import type { PercentAssignTypeDto } from "../types"
import type { QTableColumn } from "quasar"

const route = useRoute()
const $q = useQuasar()

const percentAssignTypes = ref<PercentAssignTypeDto[]>([])
const isLoading = ref(false)

const columns = computed<QTableColumn[]>(() => {
    const isMobile = $q.screen.lt.sm
    const cols: QTableColumn[] = [{ name: "name", label: "Name", field: "name", align: "left" }]
    if (!isMobile) {
        cols.push({
            name: "instructorCount",
            label: "# Instructors",
            field: "instructorCount",
            align: "center",
            style: "width: 120px",
        })
    }
    cols.push({
        name: "showOnTemplate",
        label: isMobile ? "Template" : "Show on Template",
        field: "showOnTemplate",
        align: "center",
        style: isMobile ? "width: 70px" : "width: 150px",
    })
    return cols
})

const currentTermCode = computed(() => {
    const parsed = route.params.termCode || route.query.termCode
    return parsed ? parseInt(parsed as string, 10) : null
})

function getInstructorsLink(typeId: number) {
    return currentTermCode.value
        ? { name: "PercentAssignTypeInstructorsWithTerm", params: { termCode: currentTermCode.value, typeId } }
        : { name: "PercentAssignTypeInstructors", params: { typeId } }
}

// Group types by class
const groupedTypes = computed(() => {
    const groups: { className: string; types: PercentAssignTypeDto[] }[] = []
    const classMap = new Map<string, PercentAssignTypeDto[]>()

    for (const type of percentAssignTypes.value) {
        const existing = classMap.get(type.class)
        if (existing) {
            existing.push(type)
        } else {
            classMap.set(type.class, [type])
        }
    }

    // Sort by class name
    const sortedClasses = Array.from(classMap.keys()).sort()
    for (const className of sortedClasses) {
        groups.push({ className, types: classMap.get(className) || [] })
    }

    return groups
})

async function loadTypes() {
    isLoading.value = true
    try {
        percentAssignTypes.value = await percentAssignTypeService.getPercentAssignTypes(true)
    } finally {
        isLoading.value = false
    }
}

onMounted(loadTypes)
</script>
