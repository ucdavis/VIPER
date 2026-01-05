<template>
    <div class="q-pa-md">
        <!-- Breadcrumb -->
        <q-breadcrumbs class="q-mb-md">
            <q-breadcrumbs-el
                label="Percent Admin Types"
                :to="backLink"
            />
            <q-breadcrumbs-el :label="typeData?.typeName ?? 'Type'" />
        </q-breadcrumbs>

        <!-- Loading state -->
        <div
            v-if="isLoading"
            class="text-grey q-my-md"
        >
            Loading instructors...
        </div>

        <template v-else-if="typeData">
            <h2>Instructors tagged with {{ typeData.typeName }} ({{ typeData.typeClass }})</h2>

            <q-table
                :rows="typeData.instructors"
                :columns="columns"
                :row-key="rowKey"
                dense
                flat
                bordered
                :pagination="{ rowsPerPage: 25 }"
            >
                <template #body-cell-fullName="props">
                    <q-td :props="props">
                        <router-link
                            v-if="currentTermCode"
                            :to="{
                                name: 'InstructorDetail',
                                params: { termCode: currentTermCode, personId: props.row.personId },
                            }"
                            class="text-primary"
                        >
                            {{ props.row.fullName }}
                        </router-link>
                        <span v-else>{{ props.row.fullName }}</span>
                    </q-td>
                </template>
            </q-table>

            <div
                v-if="typeData.instructors.length === 0"
                class="text-grey q-my-md"
            >
                No instructors found with this percent admin type.
            </div>
        </template>

        <!-- Error state -->
        <template v-else>
            <q-banner class="bg-negative text-white q-mb-md">
                Percent admin type not found.
                <template #action>
                    <q-btn
                        flat
                        label="Go Back"
                        :to="backLink"
                    />
                </template>
            </q-banner>
        </template>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue"
import { useRoute } from "vue-router"
import { effortService } from "../services/effort-service"
import type { InstructorsByTypeResponseDto, InstructorByTypeDto } from "../types"
import type { QTableColumn } from "quasar"

const route = useRoute()

const typeData = ref<InstructorsByTypeResponseDto | null>(null)
const isLoading = ref(false)

const rowKey = (row: InstructorByTypeDto) => `${row.personId}-${row.academicYear}`

const columns: QTableColumn[] = [
    { name: "academicYear", label: "Academic Year", field: "academicYear", align: "center", sortable: true },
    { name: "fullName", label: "Instructor", field: "fullName", align: "left", sortable: true },
]

const typeId = computed(() => {
    const id = route.params.typeId
    return id ? parseInt(id as string, 10) : null
})

const currentTermCode = computed(() => {
    const parsed = route.params.termCode || route.query.termCode
    return parsed ? parseInt(parsed as string, 10) : null
})

const backLink = computed(() =>
    currentTermCode.value
        ? { name: "EffortTypeListWithTerm", params: { termCode: currentTermCode.value } }
        : { name: "EffortTypeList" },
)

async function loadInstructors() {
    if (!typeId.value) return

    isLoading.value = true
    try {
        typeData.value = await effortService.getInstructorsByType(typeId.value)
    } finally {
        isLoading.value = false
    }
}

watch(typeId, loadInstructors, { immediate: true })
</script>
