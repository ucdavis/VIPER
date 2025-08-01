<script setup lang="ts">
import { inject, ref, onMounted } from 'vue'
import { useFetch } from '@/composables/ViperFetch'
import type { Bundle, CompetencyBundleAssociation } from '../types'

// API setup
const { get } = useFetch()
const apiUrl = inject('apiURL') as string

// Component state
const competencies = ref<CompetencyBundleAssociation[]>([])
const groupedCompetencies = ref<Map<string, CompetencyBundleAssociation[]>>(new Map())
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
        label: '#',
        align: 'left' as const,
        field: (row: CompetencyBundleAssociation) => row.number,
        sortable: true,
        style: 'width: 10%; min-width: 60px;'
    },
    {
        name: 'name',
        required: true,
        label: 'Competency Name',
        align: 'left' as const,
        field: (row: CompetencyBundleAssociation) => row.name,
        sortable: true,
        style: 'width: 45%; min-width: 200px;',
        classes: 'competency-name-column'
    },
    {
        name: 'bundles',
        label: 'Associated Bundles',
        align: 'left' as const,
        field: (row: CompetencyBundleAssociation) => row.bundles,
        style: 'width: 30%; min-width: 150px;'
    },
    {
        name: 'flags',
        label: 'Bundle Flags',
        align: 'center' as const,
        field: (row: CompetencyBundleAssociation) => row.bundles,
        style: 'width: 15%; min-width: 100px;'
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
 * Groups competencies by their parent
 * @param competencyList - List of competencies to group
 */
function groupCompetenciesByParent(competencyList: CompetencyBundleAssociation[]) {
    const grouped = new Map<string, CompetencyBundleAssociation[]>()
    
    // First, separate parent and child competencies
    const parentCompetencies = competencyList.filter(c => c.parentId === null)
    const childCompetencies = competencyList.filter(c => c.parentId !== null)
    
    // Create a map of all competencies by ID for quick lookup
    const competencyMap = new Map<number, CompetencyBundleAssociation>()
    competencyList.forEach(c => competencyMap.set(c.competencyId, c))
    
    // Group children by their parent
    childCompetencies.forEach(child => {
        if (child.parentId && competencyMap.has(child.parentId)) {
            // Parent exists in the result set
            const parent = competencyMap.get(child.parentId)!
            const parentKey = `${parent.number} ${parent.name}`
            if (!grouped.has(parentKey)) {
                grouped.set(parentKey, [parent])
            }
            if (!grouped.get(parentKey)!.includes(child)) {
                grouped.get(parentKey)!.push(child)
            }
        } else if (child.parentNumber && child.parentName) {
            // Parent not in result set, but we have parent info
            const parentKey = `${child.parentNumber} ${child.parentName}`
            if (!grouped.has(parentKey)) {
                grouped.set(parentKey, [])
            }
            grouped.get(parentKey)!.push(child)
        }
    })
    
    // Add standalone parent competencies (no children in result set)
    parentCompetencies.forEach(parent => {
        const parentKey = `${parent.number} ${parent.name}`
        if (!grouped.has(parentKey)) {
            grouped.set(parentKey, [parent])
        }
    })
    
    // Sort groups by key (competency number)
    const sortedGrouped = new Map([...grouped.entries()].sort((a, b) => {
        const aNum = a[0].split(' ')[0]
        const bNum = b[0].split(' ')[0]
        return aNum.localeCompare(bNum, undefined, { numeric: true })
    }))
    
    return sortedGrouped
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
        
        const url = `${apiUrl}cts/competencies/bundle-associations${params.toString() ? '?' + params.toString() : ''}`
        const response = await get(url)
        
        if (response.success) {
            competencies.value = response.result
            groupedCompetencies.value = groupCompetenciesByParent(response.result)
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
            <div class="table-wrapper" v-if="!loading">
                <template v-for="[parentKey, children] in groupedCompetencies" :key="parentKey">
                    <div class="parent-section q-pa-md">
                        <div class="parent-header text-h6 text-primary q-mb-md">
                            <q-icon name="folder" class="q-mr-sm" />
                            {{ parentKey }}
                        </div>
                        <q-table
                            v-bind="tableStaticProps"
                            :rows="children"
                            :columns="columns" 
                            :grid="$q.screen.lt.sm"
                            :card-class="$q.screen.lt.sm ? 'bg-grey-1 text-grey-9' : ''"
                            class="child-table"
                        >
                <template v-slot:body-cell-name="props">
                    <q-td :props="props">
                        <div class="col-competency-name">
                            <q-tooltip v-if="props.value && props.value.length > 40" anchor="top middle" self="bottom middle">
                                {{ props.value }}
                            </q-tooltip>
                            {{ props.value }}
                        </div>
                    </q-td>
                </template>
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
                </template>
                <div v-if="groupedCompetencies.size === 0" class="full-width row flex-center text-grey q-gutter-sm q-pa-xl">
                    <q-icon size="2em" name="warning" />
                    <span>
                        {{ error ? error : 'No competencies found matching the selected criteria' }}
                    </span>
                </div>
            </div>
            <div v-else class="row flex-center q-pa-xl">
                <q-spinner color="primary" size="3em" />
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

/* Parent section styling */
.parent-section {
    border-bottom: 1px solid #e0e0e0;
}

.parent-section:last-child {
    border-bottom: none;
}

.parent-header {
    background-color: #f5f5f5;
    padding: 12px;
    border-radius: 4px;
    margin-bottom: 16px;
}

/* Child table styling */
.child-table {
    box-shadow: none;
    background-color: transparent;
}

.child-table :deep(.q-table__top) {
    display: none;
}

.child-table :deep(.q-table__bottom) {
    display: none;
}

/* Force table layout for proper column width control */
:deep(.q-table__table) {
    table-layout: fixed;
    width: 100%;
}

/* Competency name cell styling for text overflow */
.col-competency-name {
    display: block;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    max-width: 100%;
}

/* Apply overflow to competency name column cells */
.competency-name-column {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

/* Ensure name column respects its width and truncates */
:deep(.q-table td:nth-child(2)) {
    overflow: hidden;
    max-width: 0; /* This forces the cell to respect table-layout: fixed */
}

/* Allow wrapping for bundle columns that need it */
:deep(.q-table td:nth-child(3)),
:deep(.q-table td:nth-child(4)) {
    white-space: normal;
    word-break: break-word;
}

/* Bundle chips wrapper */
.bundle-chips {
    flex-wrap: wrap;
}

/* Indentation for child competencies */
.child-table :deep(tbody tr td:nth-child(2)) {
    padding-left: 32px;
}
</style>
