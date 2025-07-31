<script setup lang="ts">
import { inject, ref, onMounted } from 'vue'
import { useFetch } from '@/composables/ViperFetch'

// Type definitions for bundle and competency data structures
interface Bundle {
    bundleId: number
    name: string
    clinical: boolean
    assessment: boolean
    milestone: boolean
}

interface CompetencyBundleAssociation {
    competencyId: number
    domainId: number
    parentId: number | null
    number: string
    name: string
    description: string | null
    canLinkToStudent: boolean
    domainName: string | null
    domainOrder: number | null
    bundles: Bundle[]
}

// API setup
const { get } = useFetch()
const apiUrl = inject('apiURL') as string

// Component state
const competencies = ref<CompetencyBundleAssociation[]>([])
const loading = ref(false)
const error = ref('')

// Filter toggles for bundle types
const clinicalFilter = ref(false)
const assessmentFilter = ref(false)
const milestoneFilter = ref(false)


// Table column definitions with responsive widths
const columns = [
    {
        name: 'number',
        required: true,
        label: 'Number',
        align: 'left' as const,
        field: (row: CompetencyBundleAssociation) => row.number,
        sortable: true,
        style: 'width: 80px;'
    },
    {
        name: 'name',
        required: true,
        label: 'Competency Name',
        align: 'left' as const,
        field: (row: CompetencyBundleAssociation) => row.name,
        sortable: true,
        // Responsive width with ellipsis for long names
        style: 'min-width: 200px; max-width: 300px;',
        classes: 'col-competency-name'
    },
    {
        name: 'bundles',
        label: 'Associated Bundles',
        align: 'left' as const,
        field: (row: CompetencyBundleAssociation) => row.bundles,
        style: 'min-width: 150px;'
    },
    {
        name: 'flags',
        label: 'Bundle Flags',
        align: 'center' as const,
        field: (row: CompetencyBundleAssociation) => row.bundles,
        style: 'min-width: 120px;'
    }
]

// Static table configuration
const tableStaticProps = {
    rowKey: 'competencyId',
    flat: true,
    bordered: true,
    pagination: { rowsPerPage: 50 }
}

/**
 * Fetches competencies with their bundle associations from the API
 * Applies filters based on bundle type toggles (clinical, assessment, milestone)
 */
async function loadCompetencies() {
    loading.value = true
    error.value = ''
    
    try {
        const params = new URLSearchParams()
        
        // Add filter parameters only if true
        if (clinicalFilter.value) params.append('clinical', 'true')
        if (assessmentFilter.value) params.append('assessment', 'true')
        if (milestoneFilter.value) params.append('milestone', 'true')
        
        const url = `${apiUrl}cts/competency-bundle-associations${params.toString() ? '?' + params.toString() : ''}`
        const response = await get(url)
        
        if (response.success) {
            competencies.value = response.result
        } else {
            error.value = 'Failed to load competencies'
        }
    } catch (e) {
        error.value = 'An error occurred while loading competencies'
        console.error(e)
    } finally {
        loading.value = false
    }
}

/**
 * Extracts unique bundle flags from a list of bundles
 * @param bundles - Array of bundle objects
 * @returns Array of flag names (Clinical, Assessment, Milestone)
 */
function getBundleFlags(bundles: Bundle[]): string[] {
    const flags = new Set<string>()
    bundles.forEach(bundle => {
        if (bundle.clinical) flags.add('Clinical')
        if (bundle.assessment) flags.add('Assessment')
        if (bundle.milestone) flags.add('Milestone')
    })
    return Array.from(flags)
}

// Load competencies when component is mounted
onMounted(() => {
    loadCompetencies()
})
</script>

<template>
    <div class="q-pa-md">
        <div class="row q-mb-md">
            <div class="col">
                <h5 class="q-my-none">Competencies Bundle Report</h5>
            </div>
        </div>

        <q-card class="q-mb-md">
            <q-card-section>
                <div class="row q-gutter-md items-center">
                    <div class="col-auto">
                        <span class="text-weight-medium">Filter by Bundle Flags:</span>
                    </div>
                    <q-toggle
                        v-model="clinicalFilter"
                        label="Clinical"
                        color="primary"
                        @update:model-value="loadCompetencies"
                    />
                    <q-toggle
                        v-model="assessmentFilter"
                        label="Assessment"
                        color="primary"
                        @update:model-value="loadCompetencies"
                    />
                    <q-toggle
                        v-model="milestoneFilter"
                        label="Milestone"
                        color="primary"
                        @update:model-value="loadCompetencies"
                    />
                    <div class="col-auto q-ml-md">
                        <q-chip v-if="!clinicalFilter && !assessmentFilter && !milestoneFilter" color="warning">
                            Showing competencies not in any bundle
                        </q-chip>
                        <q-chip v-else color="info">
                            Showing competencies with selected flags
                        </q-chip>
                    </div>
                </div>
            </q-card-section>
        </q-card>

        <q-card>
            <div class="table-wrapper">
                <q-table
                    v-bind="tableStaticProps"
                    :rows="competencies"
                    :columns="columns" 
                    :loading="loading"
                    :grid="$q.screen.lt.sm"
                    :card-class="$q.screen.lt.sm ? 'bg-grey-1 text-grey-9' : ''"
                >
                <template v-slot:body-cell-bundles="props">
                    <q-td :props="props">
                        <div v-if="props.value.length === 0" class="text-grey-6">
                            None
                        </div>
                        <div v-else class="row q-gutter-xs bundle-chips">
                            <q-chip
                                v-for="bundle in props.value"
                                :key="bundle.bundleId"
                                color="info"
                                text-color="white"
                                size="sm"
                                dense
                                class="q-ma-xs"
                            >
                                {{ bundle.name }}
                            </q-chip>
                        </div>
                    </q-td>
                </template>
                
                <template v-slot:body-cell-flags="props">
                    <q-td :props="props">
                        <div class="row q-gutter-xs justify-center">
                            <q-chip
                                v-for="flag in getBundleFlags(props.value)"
                                :key="flag"
                                size="sm"
                                :color="flag === 'Clinical' ? 'teal' : flag === 'Assessment' ? 'orange' : 'purple'"
                                text-color="white"
                                dense
                            >
                                {{ flag }}
                            </q-chip>
                        </div>
                    </q-td>
                </template>
                
                <template v-slot:no-data>
                    <div class="full-width row flex-center text-grey q-gutter-sm">
                        <q-icon size="2em" name="warning" />
                        <span>
                            {{ error ? error : 'No competencies found matching the selected criteria' }}
                        </span>
                    </div>
                </template>
                
                <template v-slot:item="props">
                    <div class="q-pa-xs col-xs-12 col-sm-6 col-md-4">
                        <q-card>
                            <q-card-section>
                                <div class="text-h6">{{ props.row.number }} {{ props.row.name }}</div>
                            </q-card-section>
                            <q-separator />
                            <q-card-section>
                                <div class="text-subtitle2 q-mb-xs">Associated Bundles:</div>
                                <div v-if="props.row.bundles.length === 0" class="text-grey-6">
                                    None
                                </div>
                                <div v-else class="row q-gutter-xs">
                                    <q-chip
                                        v-for="bundle in props.row.bundles"
                                        :key="bundle.bundleId"
                                        color="info"
                                        text-color="white"
                                        size="sm"
                                        dense
                                    >
                                        {{ bundle.name }}
                                    </q-chip>
                                </div>
                            </q-card-section>
                            <q-card-section v-if="getBundleFlags(props.row.bundles).length > 0">
                                <div class="text-subtitle2 q-mb-xs">Flags:</div>
                                <div class="row q-gutter-xs">
                                    <q-chip
                                        v-for="flag in getBundleFlags(props.row.bundles)"
                                        :key="flag"
                                        size="sm"
                                        :color="flag === 'Clinical' ? 'teal' : flag === 'Assessment' ? 'orange' : 'purple'"
                                        text-color="white"
                                        dense
                                    >
                                        {{ flag }}
                                    </q-chip>
                                </div>
                            </q-card-section>
                        </q-card>
                    </div>
                </template>
                </q-table>
            </div>
        </q-card>
    </div>
</template>

<style scoped>
/* Table wrapper for horizontal scrolling on tablets */
.table-wrapper {
    overflow-x: auto;
    width: 100%;
}

/* Competency name cell styling for text overflow */
.col-competency-name {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

/* Bundle chips wrapper */
.bundle-chips {
    flex-wrap: wrap;
}
</style>
