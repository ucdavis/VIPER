<template>
    <div class="q-pa-md">
        <h2>Effort Dashboard</h2>

        <!-- Loading state -->
        <template v-if="loading">
            <div class="text-grey q-my-lg">Loading dashboard...</div>
        </template>

        <!-- Dashboard content -->
        <template v-else-if="stats">
            <!-- Stats Cards Row -->
            <div class="row q-col-gutter-md q-mb-lg">
                <!-- Verification Progress Card -->
                <div class="col-12 col-md-4">
                    <q-card class="stat-card">
                        <q-card-section>
                            <div class="text-overline text-grey-7">VERIFICATION PROGRESS</div>
                            <div class="text-h4 q-my-sm">{{ stats.verificationPercent }}%</div>
                            <q-linear-progress
                                :value="stats.verificationPercent / 100"
                                :color="getProgressColor(stats.verificationPercent)"
                                size="12px"
                                class="q-mb-sm"
                                rounded
                            />
                            <div class="text-caption text-grey-7">
                                {{ stats.verifiedInstructors }} of {{ stats.totalInstructors }} instructors verified
                            </div>
                        </q-card-section>
                        <q-separator />
                        <q-card-actions>
                            <q-btn
                                flat
                                dense
                                color="primary"
                                :to="{ name: 'InstructorList', params: { termCode } }"
                            >
                                View Instructors
                            </q-btn>
                        </q-card-actions>
                    </q-card>
                </div>

                <!-- Instructors Card -->
                <div class="col-12 col-md-4">
                    <q-card class="stat-card">
                        <q-card-section>
                            <div class="text-overline text-grey-7">INSTRUCTORS</div>
                            <div class="text-h4 q-my-sm">{{ stats.totalInstructors }}</div>
                            <div class="text-caption">
                                <span
                                    v-if="stats.pendingInstructors > 0"
                                    class="text-warning"
                                >
                                    {{ stats.pendingInstructors }} pending verification
                                </span>
                                <span
                                    v-else
                                    class="text-positive"
                                >
                                    All verified
                                </span>
                            </div>
                        </q-card-section>
                        <q-separator />
                        <q-card-actions>
                            <q-btn
                                flat
                                dense
                                color="primary"
                                :to="{ name: 'InstructorList', params: { termCode } }"
                            >
                                View All
                            </q-btn>
                        </q-card-actions>
                    </q-card>
                </div>

                <!-- Courses Card -->
                <div class="col-12 col-md-4">
                    <q-card class="stat-card">
                        <q-card-section>
                            <div class="text-overline text-grey-7">COURSES</div>
                            <div class="text-h4 q-my-sm">{{ stats.totalCourses }}</div>
                            <div class="text-caption">
                                <span
                                    v-if="stats.coursesWithoutInstructors > 0"
                                    class="text-warning"
                                >
                                    {{ stats.coursesWithoutInstructors }} without instructors
                                </span>
                                <span
                                    v-else
                                    class="text-positive"
                                >
                                    All have instructors
                                </span>
                            </div>
                        </q-card-section>
                        <q-separator />
                        <q-card-actions>
                            <q-btn
                                flat
                                dense
                                color="primary"
                                :to="{ name: 'CourseList', params: { termCode } }"
                            >
                                View All
                            </q-btn>
                        </q-card-actions>
                    </q-card>
                </div>
            </div>

            <!-- Data Hygiene Summary Card -->
            <div class="row q-col-gutter-md q-mb-lg">
                <div class="col-12 col-md-6">
                    <q-card class="stat-card">
                        <q-card-section>
                            <div class="text-overline text-grey-7">DATA HYGIENE</div>
                            <div class="row q-col-gutter-sm q-mt-sm">
                                <div class="col-4 text-center">
                                    <div
                                        class="text-h5"
                                        :class="
                                            stats.hygieneSummary.activeAlerts > 0 ? 'text-warning' : 'text-positive'
                                        "
                                    >
                                        {{ stats.hygieneSummary.activeAlerts }}
                                    </div>
                                    <div class="text-caption text-grey-7">Active</div>
                                </div>
                                <div class="col-4 text-center">
                                    <div class="text-h5 text-positive">
                                        {{ stats.hygieneSummary.resolvedAlerts }}
                                    </div>
                                    <div class="text-caption text-grey-7">Resolved</div>
                                </div>
                                <div class="col-4 text-center">
                                    <div class="text-h5 text-grey">
                                        {{ stats.hygieneSummary.ignoredAlerts }}
                                    </div>
                                    <div class="text-caption text-grey-7">Ignored</div>
                                </div>
                            </div>
                        </q-card-section>
                        <q-separator />
                        <q-card-actions>
                            <q-btn
                                flat
                                dense
                                color="primary"
                                @click="showAlertsDialog = true"
                            >
                                View All Alerts
                            </q-btn>
                        </q-card-actions>
                    </q-card>
                </div>

                <!-- Term Status Card -->
                <div class="col-12 col-md-6">
                    <q-card class="stat-card">
                        <q-card-section>
                            <div class="text-overline text-grey-7">TERM STATUS</div>
                            <div class="text-h6 q-my-sm">{{ stats.currentTerm?.termName }}</div>
                            <div class="row q-col-gutter-sm">
                                <div class="col-6">
                                    <q-badge
                                        :color="getTermStatusColor(stats.currentTerm?.status)"
                                        class="q-pa-sm"
                                    >
                                        {{ stats.currentTerm?.status }}
                                    </q-badge>
                                </div>
                                <div class="col-6 text-right">
                                    <span class="text-caption text-grey-7">
                                        {{ stats.totalRecords }} effort records
                                    </span>
                                </div>
                            </div>
                            <div
                                v-if="stats.currentTerm?.harvestedDate"
                                class="text-caption text-grey-7 q-mt-sm"
                            >
                                Data harvested: {{ formatDate(stats.currentTerm.harvestedDate) }}
                            </div>
                        </q-card-section>
                        <q-separator />
                        <q-card-actions>
                            <q-btn
                                flat
                                dense
                                color="primary"
                                :to="{ name: 'TermManagement' }"
                            >
                                Manage Terms
                            </q-btn>
                        </q-card-actions>
                    </q-card>
                </div>
            </div>

            <!-- Department Verification Section -->
            <q-card class="q-mb-lg">
                <q-expansion-item
                    v-model="sectionExpanded.departments"
                    header-class="text-h6"
                    expand-icon-class="text-grey-7"
                >
                    <template #header>
                        <q-item-section>
                            <div class="text-h6">Verification by Department</div>
                        </q-item-section>
                    </template>
                    <q-card-section class="q-pt-none">
                        <!-- Needs Follow-up Section -->
                        <template v-if="needsFollowupDepts.length > 0">
                            <div class="text-subtitle2 text-warning q-mb-sm">
                                <q-icon
                                    name="warning"
                                    class="q-mr-xs"
                                />
                                Needs Follow-up (&lt;80% verified)
                            </div>
                            <div class="row q-col-gutter-sm q-mb-md">
                                <div
                                    v-for="dept in needsFollowupDepts"
                                    :key="dept.departmentCode"
                                    class="col-12 col-md-6 col-lg-4"
                                >
                                    <div class="dept-row">
                                        <div class="dept-name text-truncate">{{ dept.departmentName }}</div>
                                        <div class="dept-stats">
                                            <q-linear-progress
                                                :value="dept.verificationPercent / 100"
                                                color="warning"
                                                size="8px"
                                                class="q-mr-sm"
                                                style="width: 100px"
                                                rounded
                                            />
                                            <span class="text-caption">
                                                {{ dept.verificationPercent }}%
                                                <span class="text-grey-6">
                                                    ({{ dept.verifiedInstructors }}/{{ dept.totalInstructors }})
                                                </span>
                                            </span>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </template>

                        <!-- On Track Section -->
                        <template v-if="onTrackDepts.length > 0">
                            <div class="text-subtitle2 text-positive q-mb-sm">
                                <q-icon
                                    name="check_circle"
                                    class="q-mr-xs"
                                />
                                On Track (&ge;80% verified)
                            </div>
                            <div class="row q-col-gutter-sm">
                                <div
                                    v-for="dept in onTrackDepts"
                                    :key="dept.departmentCode"
                                    class="col-12 col-md-6 col-lg-4"
                                >
                                    <div class="dept-row">
                                        <div class="dept-name text-truncate">{{ dept.departmentName }}</div>
                                        <div class="dept-stats">
                                            <q-linear-progress
                                                :value="dept.verificationPercent / 100"
                                                color="positive"
                                                size="8px"
                                                class="q-mr-sm"
                                                style="width: 100px"
                                                rounded
                                            />
                                            <span class="text-caption">
                                                {{ dept.verificationPercent }}%
                                                <span class="text-grey-6">
                                                    ({{ dept.verifiedInstructors }}/{{ dept.totalInstructors }})
                                                </span>
                                            </span>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </template>
                    </q-card-section>
                </q-expansion-item>
            </q-card>

            <!-- Recent Changes Section -->
            <q-card class="q-mb-lg">
                <q-expansion-item
                    v-model="sectionExpanded.recentChanges"
                    header-class="text-h6"
                    expand-icon-class="text-grey-7"
                >
                    <template #header>
                        <q-item-section>
                            <div class="text-h6">Recent Changes</div>
                        </q-item-section>
                        <q-item-section side>
                            <q-btn
                                flat
                                dense
                                size="sm"
                                label="View Full Audit Log"
                                color="primary"
                                :to="{ name: 'EffortAuditWithTerm', params: { termCode } }"
                                @click.stop
                            />
                        </q-item-section>
                    </template>
                    <q-card-section class="q-pt-none">
                        <template v-if="recentChanges.length === 0">
                            <div class="text-grey-6 text-center q-pa-md">No recent changes</div>
                        </template>

                        <q-list
                            v-else
                            dense
                            separator
                        >
                            <q-item
                                v-for="change in recentChanges"
                                :key="change.id"
                            >
                                <q-item-section avatar>
                                    <q-icon
                                        :name="getChangeIcon(change.action)"
                                        :color="getChangeColor(change.action)"
                                    />
                                </q-item-section>
                                <q-item-section>
                                    <q-item-label>
                                        {{ formatChangeAction(change.action) }}
                                        <span
                                            v-if="change.instructorName"
                                            class="text-weight-medium"
                                        >
                                            - {{ change.instructorName }}
                                        </span>
                                        <span
                                            v-if="change.courseCode"
                                            class="text-grey-7"
                                        >
                                            ({{ change.courseCode }})
                                        </span>
                                    </q-item-label>
                                    <q-item-label caption>
                                        {{ formatDate(change.changedDate) }} by {{ change.changedByName }}
                                    </q-item-label>
                                </q-item-section>
                            </q-item>
                        </q-list>
                    </q-card-section>
                </q-expansion-item>
            </q-card>

            <!-- Data Hygiene Alerts Section -->
            <q-card class="q-mb-lg">
                <q-expansion-item
                    v-model="sectionExpanded.dataHygiene"
                    header-class="text-h6"
                    expand-icon-class="text-grey-7"
                >
                    <template #header>
                        <q-item-section>
                            <div class="text-h6">Data Hygiene Alerts</div>
                        </q-item-section>
                        <q-item-section side>
                            <q-btn
                                flat
                                dense
                                size="sm"
                                :label="showIgnoredAlerts ? 'Hide Ignored' : 'Show Ignored'"
                                @click.stop="showIgnoredAlerts = !showIgnoredAlerts"
                            />
                        </q-item-section>
                    </template>
                    <q-card-section class="q-pt-none">
                        <template v-if="visibleAlerts.length === 0">
                            <div class="text-grey-6 text-center q-pa-md">No alerts to display</div>
                        </template>

                        <q-list
                            v-else
                            class="alert-sections"
                        >
                            <!-- No Department Assigned (High Priority) -->
                            <q-expansion-item
                                v-if="noDeptAlerts.length > 0"
                                v-model="expandedSections.noDept"
                                dense
                                header-class="text-negative"
                            >
                                <template #header>
                                    <q-item-section avatar>
                                        <q-icon
                                            name="domain_disabled"
                                            color="negative"
                                        />
                                    </q-item-section>
                                    <q-item-section>
                                        No Department Assigned ({{ noDeptAlerts.length }})
                                    </q-item-section>
                                </template>
                                <q-list
                                    dense
                                    separator
                                    class="q-ml-lg"
                                >
                                    <q-item
                                        v-for="alert in noDeptAlerts"
                                        :key="`${alert.alertType}-${alert.entityId}`"
                                        :class="{ 'bg-grey-2': alert.status !== 'Active' }"
                                    >
                                        <q-item-section>
                                            <q-item-label>
                                                {{ alert.entityName }}
                                                <q-badge
                                                    v-if="alert.status === 'Ignored'"
                                                    color="grey"
                                                    class="q-ml-sm"
                                                >
                                                    Ignored
                                                </q-badge>
                                            </q-item-label>
                                            <q-item-label caption>{{ alert.description }}</q-item-label>
                                        </q-item-section>
                                        <q-item-section side>
                                            <div class="row q-gutter-xs">
                                                <q-btn
                                                    v-if="alert.status === 'Active'"
                                                    flat
                                                    dense
                                                    size="sm"
                                                    label="Edit/Review"
                                                    color="primary"
                                                    @click="navigateToEdit(alert)"
                                                />
                                                <q-btn
                                                    v-if="alert.status === 'Active'"
                                                    flat
                                                    dense
                                                    size="sm"
                                                    label="Ignore"
                                                    color="grey"
                                                    @click="ignoreAlert(alert)"
                                                />
                                            </div>
                                        </q-item-section>
                                    </q-item>
                                </q-list>
                            </q-expansion-item>

                            <!-- Zero Hours Assigned -->
                            <q-expansion-item
                                v-if="zeroHoursAlerts.length > 0"
                                v-model="expandedSections.zeroHours"
                                dense
                                header-class="text-warning"
                            >
                                <template #header>
                                    <q-item-section avatar>
                                        <q-icon
                                            name="timer_off"
                                            color="warning"
                                        />
                                    </q-item-section>
                                    <q-item-section>
                                        Zero Hours Assigned ({{ zeroHoursAlerts.length }})
                                    </q-item-section>
                                </template>
                                <q-list
                                    dense
                                    separator
                                    class="q-ml-lg"
                                >
                                    <q-item
                                        v-for="alert in zeroHoursAlerts"
                                        :key="`${alert.alertType}-${alert.entityId}`"
                                        :class="{ 'bg-grey-2': alert.status !== 'Active' }"
                                    >
                                        <q-item-section>
                                            <q-item-label>
                                                {{ alert.entityName }}
                                                <q-badge
                                                    v-if="alert.status === 'Ignored'"
                                                    color="grey"
                                                    class="q-ml-sm"
                                                >
                                                    Ignored
                                                </q-badge>
                                            </q-item-label>
                                            <q-item-label caption>{{ alert.description }}</q-item-label>
                                        </q-item-section>
                                        <q-item-section side>
                                            <div class="row q-gutter-xs">
                                                <q-btn
                                                    v-if="alert.status === 'Active'"
                                                    flat
                                                    dense
                                                    size="sm"
                                                    label="Edit/Review"
                                                    color="primary"
                                                    @click="navigateToEdit(alert)"
                                                />
                                                <q-btn
                                                    v-if="alert.status === 'Active'"
                                                    flat
                                                    dense
                                                    size="sm"
                                                    label="Ignore"
                                                    color="grey"
                                                    @click="ignoreAlert(alert)"
                                                />
                                            </div>
                                        </q-item-section>
                                    </q-item>
                                </q-list>
                            </q-expansion-item>

                            <!-- Courses with No Instructors -->
                            <q-expansion-item
                                v-if="noInstructorsAlerts.length > 0"
                                v-model="expandedSections.noInstructors"
                                dense
                                header-class="text-warning"
                            >
                                <template #header>
                                    <q-item-section avatar>
                                        <q-icon
                                            name="school"
                                            color="warning"
                                        />
                                    </q-item-section>
                                    <q-item-section>
                                        Courses with No Instructors ({{ noInstructorsAlerts.length }})
                                    </q-item-section>
                                </template>
                                <q-list
                                    dense
                                    separator
                                    class="q-ml-lg"
                                >
                                    <q-item
                                        v-for="alert in noInstructorsAlerts"
                                        :key="`${alert.alertType}-${alert.entityId}`"
                                        :class="{ 'bg-grey-2': alert.status !== 'Active' }"
                                    >
                                        <q-item-section>
                                            <q-item-label>
                                                {{ alert.entityName }}
                                                <q-badge
                                                    v-if="alert.status === 'Ignored'"
                                                    color="grey"
                                                    class="q-ml-sm"
                                                >
                                                    Ignored
                                                </q-badge>
                                            </q-item-label>
                                            <q-item-label caption>{{ alert.description }}</q-item-label>
                                        </q-item-section>
                                        <q-item-section side>
                                            <div class="row q-gutter-xs">
                                                <q-btn
                                                    v-if="alert.status === 'Active'"
                                                    flat
                                                    dense
                                                    size="sm"
                                                    label="Edit/Review"
                                                    color="primary"
                                                    @click="navigateToEdit(alert)"
                                                />
                                                <q-btn
                                                    v-if="alert.status === 'Active'"
                                                    flat
                                                    dense
                                                    size="sm"
                                                    label="Ignore"
                                                    color="grey"
                                                    @click="ignoreAlert(alert)"
                                                />
                                            </div>
                                        </q-item-section>
                                    </q-item>
                                </q-list>
                            </q-expansion-item>

                            <!-- No Effort Records -->
                            <q-expansion-item
                                v-if="noRecordsAlerts.length > 0"
                                v-model="expandedSections.noRecords"
                                dense
                                header-class="text-warning"
                            >
                                <template #header>
                                    <q-item-section avatar>
                                        <q-icon
                                            name="warning"
                                            color="warning"
                                        />
                                    </q-item-section>
                                    <q-item-section>
                                        Instructors with No Effort Records ({{ noRecordsAlerts.length }})
                                    </q-item-section>
                                </template>
                                <q-list
                                    dense
                                    separator
                                    class="q-ml-lg"
                                >
                                    <q-item
                                        v-for="alert in noRecordsAlerts"
                                        :key="`${alert.alertType}-${alert.entityId}`"
                                        :class="{ 'bg-grey-2': alert.status !== 'Active' }"
                                    >
                                        <q-item-section>
                                            <q-item-label>
                                                {{ alert.entityName }}
                                                <q-badge
                                                    v-if="alert.status === 'Ignored'"
                                                    color="grey"
                                                    class="q-ml-sm"
                                                >
                                                    Ignored
                                                </q-badge>
                                            </q-item-label>
                                            <q-item-label caption>{{ alert.description }}</q-item-label>
                                        </q-item-section>
                                        <q-item-section side>
                                            <div class="row q-gutter-xs">
                                                <q-btn
                                                    v-if="alert.status === 'Active'"
                                                    flat
                                                    dense
                                                    size="sm"
                                                    label="Edit/Review"
                                                    color="primary"
                                                    @click="navigateToEdit(alert)"
                                                />
                                                <q-btn
                                                    v-if="alert.status === 'Active'"
                                                    flat
                                                    dense
                                                    size="sm"
                                                    label="Ignore"
                                                    color="grey"
                                                    @click="ignoreAlert(alert)"
                                                />
                                            </div>
                                        </q-item-section>
                                    </q-item>
                                </q-list>
                            </q-expansion-item>
                        </q-list>
                    </q-card-section>
                </q-expansion-item>
            </q-card>
        </template>

        <!-- No term selected -->
        <template v-else>
            <q-card>
                <q-card-section class="text-center q-pa-lg">
                    <q-icon
                        name="calendar_today"
                        size="48px"
                        color="grey-5"
                    />
                    <div class="text-h6 text-grey-7 q-mt-md">No Term Selected</div>
                    <div class="text-grey-6 q-mb-md">Please select a term to view the dashboard.</div>
                    <q-btn
                        color="primary"
                        :to="{ name: 'TermSelection' }"
                    >
                        Select Term
                    </q-btn>
                </q-card-section>
            </q-card>
        </template>

        <!-- All Alerts Dialog -->
        <q-dialog v-model="showAlertsDialog">
            <q-card style="min-width: 600px; max-width: 90vw">
                <q-card-section class="row items-center">
                    <div class="text-h6">All Data Hygiene Alerts</div>
                    <q-space />
                    <q-btn
                        icon="close"
                        flat
                        round
                        dense
                        v-close-popup
                    />
                </q-card-section>
                <q-separator />
                <q-card-section style="max-height: 70vh; overflow: auto">
                    <template v-if="allAlerts.length === 0">
                        <div class="text-grey-6 text-center q-pa-md">No alerts</div>
                    </template>
                    <q-list
                        v-else
                        dense
                        separator
                    >
                        <q-item
                            v-for="alert in allAlerts"
                            :key="`${alert.alertType}-${alert.entityId}`"
                        >
                            <q-item-section avatar>
                                <q-icon
                                    :name="getAlertIcon(alert)"
                                    :color="getAlertColor(alert)"
                                />
                            </q-item-section>
                            <q-item-section>
                                <q-item-label>{{ alert.title }}: {{ alert.entityName }}</q-item-label>
                                <q-item-label caption>{{ alert.description }}</q-item-label>
                            </q-item-section>
                            <q-item-section side>
                                <q-badge :color="getAlertColor(alert)">
                                    {{ alert.severity }}
                                </q-badge>
                            </q-item-section>
                        </q-item>
                    </q-list>
                </q-card-section>
            </q-card>
        </q-dialog>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import { dashboardService } from "../services/dashboard-service"
import type { DashboardStatsDto, DepartmentVerificationDto, EffortChangeAlertDto, RecentChangeDto } from "../types"

const route = useRoute()
const router = useRouter()

const loading = ref(true)
const stats = ref<DashboardStatsDto | null>(null)
const departments = ref<DepartmentVerificationDto[]>([])
const alerts = ref<EffortChangeAlertDto[]>([])
const allAlerts = ref<EffortChangeAlertDto[]>([])
const recentChanges = ref<RecentChangeDto[]>([])
const showIgnoredAlerts = ref(false)
const showAlertsDialog = ref(false)
const expandedSections = ref({
    noDept: true,
    zeroHours: true,
    noInstructors: true,
    noRecords: true,
})
const sectionExpanded = ref({
    departments: true,
    recentChanges: true,
    dataHygiene: true,
})

const termCode = computed(() => {
    const tc = route.params.termCode
    return tc ? parseInt(tc as string, 10) : null
})

const needsFollowupDepts = computed(() => departments.value.filter((d) => !d.meetsThreshold))

const onTrackDepts = computed(() => departments.value.filter((d) => d.meetsThreshold))

const visibleAlerts = computed(() => {
    if (showIgnoredAlerts.value) {
        return alerts.value
    }
    return alerts.value.filter((a) => a.status !== "Ignored")
})

const noDeptAlerts = computed(() => visibleAlerts.value.filter((a) => a.alertType === "NoDepartment"))

const zeroHoursAlerts = computed(() => visibleAlerts.value.filter((a) => a.alertType === "ZeroHours"))

const noRecordsAlerts = computed(() => visibleAlerts.value.filter((a) => a.alertType === "NoRecords"))

const noInstructorsAlerts = computed(() => visibleAlerts.value.filter((a) => a.alertType === "NoInstructors"))

function getProgressColor(percent: number): string {
    if (percent >= 80) return "positive"
    if (percent >= 50) return "warning"
    return "negative"
}

function formatDate(dateString: string): string {
    const date = new Date(dateString)
    return date.toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" })
}

function getChangeIcon(action: string): string {
    if (action.includes("Create")) return "add_circle"
    if (action.includes("Update") || action.includes("Edit")) return "edit"
    if (action.includes("Delete")) return "delete"
    if (action.includes("Verify")) return "check_circle"
    return "info"
}

function getChangeColor(action: string): string {
    if (action.includes("Create")) return "positive"
    if (action.includes("Update") || action.includes("Edit")) return "primary"
    if (action.includes("Delete")) return "negative"
    if (action.includes("Verify")) return "positive"
    return "grey"
}

function formatChangeAction(action: string): string {
    return action
        .replace(/([A-Z])/g, " $1")
        .trim()
        .replace(/^./, (str) => str.toUpperCase())
}

function getTermStatusColor(status: string | undefined): string {
    switch (status) {
        case "Opened":
            return "positive"
        case "Closed":
            return "grey"
        case "Harvested":
            return "info"
        default:
            return "grey"
    }
}

function getAlertIcon(alert: EffortChangeAlertDto): string {
    switch (alert.alertType) {
        case "NoRecords":
            return "warning"
        case "NoInstructors":
            return "school"
        case "NotVerified":
            return "schedule"
        case "NoDepartment":
            return "domain_disabled"
        case "ZeroHours":
            return "timer_off"
        default:
            return "error"
    }
}

function getAlertColor(alert: EffortChangeAlertDto): string {
    switch (alert.severity) {
        case "High":
            return "negative"
        case "Medium":
            return "warning"
        case "Low":
            return "info"
        default:
            return "grey"
    }
}

async function loadDashboard() {
    if (!termCode.value) {
        loading.value = false
        return
    }

    loading.value = true
    try {
        const [statsData, deptsData, allAlertsData, recentChangesData] = await Promise.all([
            dashboardService.getStats(termCode.value),
            dashboardService.getDepartmentVerification(termCode.value),
            dashboardService.getAllAlerts(termCode.value, true),
            dashboardService.getRecentChanges(termCode.value, 10),
        ])

        stats.value = statsData
        departments.value = deptsData
        alerts.value = allAlertsData
        allAlerts.value = allAlertsData
        recentChanges.value = recentChangesData
    } finally {
        loading.value = false
    }
}

function navigateToEdit(alert: EffortChangeAlertDto) {
    if (!termCode.value) return

    let route
    if (alert.entityType === "Course") {
        route = router.resolve({
            name: "CourseEdit",
            params: { termCode: termCode.value, courseId: alert.entityId },
        })
    } else if (alert.alertType === "NoDepartment") {
        route = router.resolve({
            name: "InstructorEdit",
            params: { termCode: termCode.value, personId: alert.entityId },
        })
    } else {
        route = router.resolve({
            name: "InstructorDetail",
            params: { termCode: termCode.value, personId: alert.entityId },
        })
    }
    window.open(route.href, "_blank")
}

async function ignoreAlert(alert: EffortChangeAlertDto) {
    if (!termCode.value) return

    const success = await dashboardService.ignoreAlert(termCode.value, alert.alertType, alert.entityId)
    if (success) {
        alert.status = "Ignored"
        await loadDashboard()
    }
}

// Use immediate watch to handle initial load - ensures termCode is ready from route params
watch(
    termCode,
    () => {
        loadDashboard()
    },
    { immediate: true },
)
</script>

<style scoped>
.stat-card {
    height: 100%;
}

.dept-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 8px 12px;
    border-radius: 4px;
    background-color: #f5f5f5;
}

.dept-name {
    flex: 1;
    font-size: 0.9rem;
    margin-right: 12px;
}

.dept-stats {
    display: flex;
    align-items: center;
    white-space: nowrap;
}
</style>
