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
                                <q-badge
                                    v-if="stats.pendingInstructors > 0"
                                    color="amber-2"
                                    text-color="brown-10"
                                >
                                    {{ stats.pendingInstructors }} pending verification
                                </q-badge>
                                <q-badge
                                    v-else
                                    color="positive"
                                >
                                    All verified
                                </q-badge>
                            </div>
                            <div class="text-caption text-grey-7 q-mt-xs">{{ stats.totalRecords }} effort records</div>
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
                                <q-badge
                                    v-if="stats.coursesWithoutInstructors > 0"
                                    color="amber-2"
                                    text-color="brown-10"
                                    class="clickable-badge"
                                    role="button"
                                    tabindex="0"
                                    @click="scrollToNoInstructorsAlert"
                                    @keyup.enter="scrollToNoInstructorsAlert"
                                    @keyup.space.prevent="scrollToNoInstructorsAlert"
                                >
                                    {{ stats.coursesWithoutInstructors }} without instructors
                                </q-badge>
                                <q-badge
                                    v-else
                                    color="positive"
                                >
                                    All have instructors
                                </q-badge>
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
                                            stats.hygieneSummary.activeAlerts > 0
                                                ? 'text-warning-accessible'
                                                : 'text-positive'
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
                            <div class="term-status-row q-my-sm">
                                <span class="text-h6">{{ stats.currentTerm?.termName }}</span>
                                <q-badge
                                    :color="getTermStatusColor(stats.currentTerm?.status)"
                                    class="q-pa-sm q-ml-sm"
                                >
                                    {{ stats.currentTerm?.status }}
                                </q-badge>
                            </div>
                            <div class="row q-col-gutter-sm q-mt-sm text-caption text-grey-7">
                                <div
                                    v-if="stats.currentTerm?.harvestedDate"
                                    class="col-auto"
                                >
                                    Harvested: {{ formatDate(stats.currentTerm.harvestedDate) }}
                                </div>
                                <div
                                    v-if="stats.currentTerm?.openedDate"
                                    class="col-auto"
                                >
                                    Opened: {{ formatDate(stats.currentTerm.openedDate) }}
                                </div>
                                <div
                                    v-if="stats.currentTerm?.closedDate"
                                    class="col-auto"
                                >
                                    Closed: {{ formatDate(stats.currentTerm.closedDate) }}
                                </div>
                            </div>
                            <div
                                v-if="stats.currentTerm?.closedDate && stats.currentTerm?.openedDate"
                                class="text-caption text-grey-6 q-mt-xs"
                            >
                                Open for
                                {{ getTermDuration(stats.currentTerm.openedDate, stats.currentTerm.closedDate) }}
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
                        <!-- For Open terms: show split view with Needs Follow-up vs On Track -->
                        <template v-if="isTermOpen">
                            <!-- Needs Follow-up Section -->
                            <template v-if="needsFollowupDepts.length > 0">
                                <div class="text-subtitle2 text-negative q-mb-sm">
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
                                        <div
                                            class="dept-row dept-row--clickable"
                                            :class="{ 'dept-row--no-dept': isNoDept(dept) }"
                                            role="button"
                                            tabindex="0"
                                            :aria-label="`View ${getDeptDisplayName(dept)} instructors`"
                                            @click="navigateToDepartment(dept.departmentCode)"
                                            @keyup.enter="navigateToDepartment(dept.departmentCode)"
                                            @keyup.space.prevent="navigateToDepartment(dept.departmentCode)"
                                        >
                                            <div class="dept-name text-truncate">
                                                {{ getDeptDisplayName(dept) }}
                                            </div>
                                            <div class="dept-stats">
                                                <span class="dept-percent text-caption">
                                                    {{ dept.verificationPercent }}%
                                                </span>
                                                <q-linear-progress
                                                    :value="dept.verificationPercent / 100"
                                                    color="negative"
                                                    size="8px"
                                                    class="dept-bar"
                                                    rounded
                                                />
                                                <span class="dept-count text-caption text-grey-6">
                                                    ({{ dept.verifiedInstructors }}/{{ dept.totalInstructors }})
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
                                        <div
                                            class="dept-row dept-row--clickable"
                                            :class="{ 'dept-row--no-dept': isNoDept(dept) }"
                                            role="button"
                                            tabindex="0"
                                            :aria-label="`View ${getDeptDisplayName(dept)} instructors`"
                                            @click="navigateToDepartment(dept.departmentCode)"
                                            @keyup.enter="navigateToDepartment(dept.departmentCode)"
                                            @keyup.space.prevent="navigateToDepartment(dept.departmentCode)"
                                        >
                                            <div class="dept-name text-truncate">
                                                {{ getDeptDisplayName(dept) }}
                                            </div>
                                            <div class="dept-stats">
                                                <span class="dept-percent text-caption">
                                                    {{ dept.verificationPercent }}%
                                                </span>
                                                <q-linear-progress
                                                    :value="dept.verificationPercent / 100"
                                                    color="positive"
                                                    size="8px"
                                                    class="dept-bar"
                                                    rounded
                                                />
                                                <span class="dept-count text-caption text-grey-6">
                                                    ({{ dept.verifiedInstructors }}/{{ dept.totalInstructors }})
                                                </span>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </template>
                        </template>

                        <!-- For Closed terms: show all departments sorted by verification % descending -->
                        <template v-else>
                            <div class="row q-col-gutter-sm">
                                <div
                                    v-for="dept in sortedDepartments"
                                    :key="dept.departmentCode"
                                    class="col-12 col-md-6 col-lg-4"
                                >
                                    <div
                                        class="dept-row dept-row--clickable"
                                        :class="{ 'dept-row--no-dept': isNoDept(dept) }"
                                        role="button"
                                        tabindex="0"
                                        :aria-label="`View ${getDeptDisplayName(dept)} instructors`"
                                        @click="navigateToDepartment(dept.departmentCode)"
                                        @keyup.enter="navigateToDepartment(dept.departmentCode)"
                                        @keyup.space.prevent="navigateToDepartment(dept.departmentCode)"
                                    >
                                        <div class="dept-name text-truncate">
                                            {{ getDeptDisplayName(dept) }}
                                        </div>
                                        <div class="dept-stats">
                                            <span class="dept-percent text-caption">
                                                {{ dept.verificationPercent }}%
                                            </span>
                                            <q-linear-progress
                                                :value="dept.verificationPercent / 100"
                                                :color="getProgressColor(dept.verificationPercent)"
                                                size="8px"
                                                class="dept-bar"
                                                rounded
                                            />
                                            <span class="dept-count text-caption text-grey-6">
                                                ({{ dept.verifiedInstructors }}/{{ dept.totalInstructors }})
                                            </span>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </template>
                    </q-card-section>
                </q-expansion-item>
            </q-card>

            <!-- Recent Changes Section (only shown if user has audit access) -->
            <q-card
                v-if="stats.hasAuditAccess"
                class="q-mb-lg"
            >
                <q-expansion-item
                    v-model="sectionExpanded.recentChanges"
                    header-class="text-h6"
                    expand-icon-class="text-grey-7"
                >
                    <template #header>
                        <q-item-section>
                            <div class="text-h6">Recent Changes</div>
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
                                v-for="change in displayedChanges"
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

                        <!-- Show more / View full audit log -->
                        <div
                            v-if="recentChanges.length > 5"
                            class="text-center q-mt-sm"
                        >
                            <q-btn
                                v-if="!showAllChanges"
                                flat
                                dense
                                size="sm"
                                color="primary"
                                icon-right="expand_more"
                                :label="`Show ${recentChanges.length - 5} more`"
                                @click="showAllChanges = true"
                            />
                            <q-btn
                                v-else
                                flat
                                dense
                                size="sm"
                                color="primary"
                                label="View Full Audit Log"
                                :to="{ name: 'EffortAuditWithTerm', params: { termCode } }"
                            />
                        </div>
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
                            <!-- Unknown Department (High Priority) -->
                            <q-expansion-item
                                v-if="noDeptAlerts.length > 0"
                                v-model="expandedSections.noDept"
                                dense
                                header-class="text-negative"
                            >
                                <template #header>
                                    <q-item-section side>
                                        <q-icon
                                            name="domain_disabled"
                                            color="negative"
                                        />
                                    </q-item-section>
                                    <q-item-section>
                                        Unknown Department ({{ noDeptAlerts.length }})
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
                                                    Ignored<template v-if="alert.reviewedBy">
                                                        by {{ alert.reviewedBy }}</template
                                                    >
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
                                header-class="text-warning-accessible"
                            >
                                <template #header>
                                    <q-item-section side>
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
                                                    Ignored<template v-if="alert.reviewedBy">
                                                        by {{ alert.reviewedBy }}</template
                                                    >
                                                </q-badge>
                                            </q-item-label>
                                            <q-item-label caption
                                                >{{ alert.description }} ({{ alert.recordCount }}
                                                {{ inflect("record", alert.recordCount) }})</q-item-label
                                            >
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
                                header-class="text-warning-accessible"
                            >
                                <template #header>
                                    <q-item-section side>
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
                                                    Ignored<template v-if="alert.reviewedBy">
                                                        by {{ alert.reviewedBy }}</template
                                                    >
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
                                id="no-instructors-alerts"
                                v-model="expandedSections.noInstructors"
                                dense
                                header-class="text-warning-accessible"
                            >
                                <template #header>
                                    <q-item-section side>
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
                                                    Ignored<template v-if="alert.reviewedBy">
                                                        by {{ alert.reviewedBy }}</template
                                                    >
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

                            <!-- Verification Overdue (only for open terms) -->
                            <q-expansion-item
                                v-if="notVerifiedAlerts.length > 0"
                                v-model="expandedSections.notVerified"
                                dense
                                header-class="text-info"
                            >
                                <template #header>
                                    <q-item-section side>
                                        <q-icon
                                            name="schedule"
                                            color="info"
                                        />
                                    </q-item-section>
                                    <q-item-section>
                                        Verification Overdue ({{ notVerifiedAlerts.length }})
                                    </q-item-section>
                                </template>
                                <q-list
                                    dense
                                    separator
                                    class="q-ml-lg"
                                >
                                    <q-item
                                        v-for="alert in notVerifiedAlerts"
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
                                                    Ignored<template v-if="alert.reviewedBy">
                                                        by {{ alert.reviewedBy }}</template
                                                    >
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
                                                    label="View"
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
            <q-card style="width: 600px; max-width: 95vw">
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
import { ref, computed, watch, nextTick } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useQuasar } from "quasar"
import { dashboardService } from "../services/dashboard-service"
import type { DashboardStatsDto, DepartmentVerificationDto, EffortChangeAlertDto, RecentChangeDto } from "../types"
import { inflect } from "inflection"

const $q = useQuasar()
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
const showAllChanges = ref(false)
const expandedSections = ref({
    noDept: false,
    zeroHours: false,
    noInstructors: false,
    noRecords: false,
    notVerified: false,
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

const isTermOpen = computed(() => stats.value?.currentTerm?.status === "Opened")

// For closed terms: sort by verification percentage descending, then by instructor count descending
const sortedDepartments = computed(() =>
    [...departments.value].sort((a, b) => {
        if (b.verificationPercent !== a.verificationPercent) {
            return b.verificationPercent - a.verificationPercent
        }
        return b.totalInstructors - a.totalInstructors
    }),
)

const visibleAlerts = computed(() => {
    if (showIgnoredAlerts.value) {
        return alerts.value
    }
    return alerts.value.filter((a) => a.status !== "Ignored")
})

const displayedChanges = computed(() => {
    if (showAllChanges.value) {
        return recentChanges.value
    }
    return recentChanges.value.slice(0, 5)
})

const noDeptAlerts = computed(() => visibleAlerts.value.filter((a) => a.alertType === "NoDepartment"))

const zeroHoursAlerts = computed(() => visibleAlerts.value.filter((a) => a.alertType === "ZeroHours"))

const noRecordsAlerts = computed(() => visibleAlerts.value.filter((a) => a.alertType === "NoRecords"))

const noInstructorsAlerts = computed(() => visibleAlerts.value.filter((a) => a.alertType === "NoInstructors"))

// Only show verification overdue alerts for open terms - they're not actionable for closed terms
const notVerifiedAlerts = computed(() => {
    if (stats.value?.currentTerm?.status !== "Opened") return []
    return visibleAlerts.value.filter((a) => a.alertType === "NotVerified")
})

function navigateToDepartment(deptCode: string) {
    if (!termCode.value) return
    router.push({
        name: "InstructorList",
        params: { termCode: termCode.value.toString() },
        query: { dept: deptCode },
    })
}

function scrollToNoInstructorsAlert() {
    sectionExpanded.value.dataHygiene = true
    expandedSections.value.noInstructors = true

    // Allow Vue to render the expanded elements, then wait for CSS animations
    nextTick(() => {
        setTimeout(() => {
            const el = document.getElementById("no-instructors-alerts")
            el?.scrollIntoView({ behavior: "smooth", block: "start" })
        }, 300)
    })
}

function getProgressColor(percent: number): string {
    if (percent >= 70) return "positive"
    if (percent >= 30) return "warning"
    return "negative"
}

function formatDate(dateString: string): string {
    const date = new Date(dateString)
    return date.toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" })
}

function getTermDuration(openedDate: string, closedDate: string): string {
    const opened = new Date(openedDate)
    const closed = new Date(closedDate)
    const diffMs = closed.getTime() - opened.getTime()
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24))

    if (diffDays < 7) {
        return `${diffDays} day${diffDays !== 1 ? "s" : ""}`
    }

    const weeks = Math.floor(diffDays / 7)
    const remainingDays = diffDays % 7

    if (remainingDays === 0) {
        return `${weeks} week${weeks !== 1 ? "s" : ""}`
    }
    return `${weeks} week${weeks !== 1 ? "s" : ""}, ${remainingDays} day${remainingDays !== 1 ? "s" : ""}`
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

function getDeptDisplayName(dept: DepartmentVerificationDto): string {
    return dept.departmentCode === "UNK" ? "Unknown Department" : dept.departmentName
}

function isNoDept(dept: DepartmentVerificationDto): boolean {
    return dept.departmentCode === "UNK"
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
        // Fetch stats first to determine if user has audit access
        const [statsData, deptsData, allAlertsData] = await Promise.all([
            dashboardService.getStats(termCode.value),
            dashboardService.getDepartmentVerification(termCode.value),
            dashboardService.getAllAlerts(termCode.value, true),
        ])

        stats.value = statsData
        departments.value = deptsData
        alerts.value = allAlertsData
        allAlerts.value = allAlertsData

        // Only fetch recent changes if user has audit access
        if (statsData?.hasAuditAccess) {
            recentChanges.value = await dashboardService.getRecentChanges(termCode.value, 10)
        } else {
            recentChanges.value = []
        }
    } finally {
        loading.value = false
    }
}

function navigateToEdit(alert: EffortChangeAlertDto) {
    if (!termCode.value) return

    let route
    if (alert.entityType === "Course") {
        route = router.resolve({
            name: "CourseDetail",
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
        $q.notify({
            type: "positive",
            message: "Alert ignored",
        })
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
    gap: 12px;
    padding: 8px 12px;
    border-radius: 4px;
    background-color: #f5f5f5;
}

@media screen and (prefers-reduced-motion: reduce) {
    .dept-row--clickable {
        cursor: pointer;
        transition: none;
    }
}

.dept-row--clickable {
    cursor: pointer;
    transition: background-color 0.2s ease;
}

.dept-row--clickable:hover,
.dept-row--clickable:focus {
    background-color: #e0e0e0;
    outline: none;
}

.dept-row--no-dept {
    background-color: #fdecea;
    border-left: 3px solid var(--q-negative);
}

.dept-row--no-dept:hover,
.dept-row--no-dept:focus {
    background-color: #fad4d0;
}

.dept-row--no-dept .dept-name {
    color: var(--q-negative);
    font-weight: 500;
}

.dept-name {
    width: 80px;
    font-size: 0.9rem;
    flex-shrink: 0;
}

.dept-stats {
    flex: 1;
    display: flex;
    align-items: center;
    gap: 8px;
}

.dept-percent {
    width: 36px;
    text-align: right;
    flex-shrink: 0;
}

.dept-bar {
    flex: 1;
    min-width: 60px;
}

.dept-count {
    width: 65px;
    flex-shrink: 0;
}

.term-status-row {
    display: flex;
    align-items: baseline;
}

/* Accessible warning text color for headers and larger text */
.text-warning-accessible {
    color: #664d03 !important;
}

.clickable-badge {
    cursor: pointer;
    text-decoration: underline;
}

.clickable-badge:hover,
.clickable-badge:focus {
    filter: brightness(0.95);
    outline: none;
}

#no-instructors-alerts {
    scroll-margin-top: 140px;
}
</style>
