<script setup lang="ts">
import { ref, reactive, onMounted, computed, nextTick, provide } from "vue"
import { useRoute, useRouter, onBeforeRouteLeave } from "vue-router"
import { useQuasar } from "quasar"
import StatusBanner from "@/components/StatusBanner.vue"
import ContactSection from "../components/ContactSection.vue"
import EmergencyContactPageShell from "../components/EmergencyContactPageShell.vue"
import PhoneInput from "../components/PhoneInput.vue"
import { useEmergencyContact } from "../composables/use-emergency-contact"
import { emergencyContactService } from "../services/emergency-contact-service"
import { checkHasOnePermission } from "@/composables/CheckPagePermission"
import { phoneErrorsKey } from "../utils/phone-errors-key"

const route = useRoute()
const router = useRouter()
const $q = useQuasar()
const formRef = ref<{ validate: () => Promise<boolean> } | null>(null)

const personId = computed(() => Number(route.params.pidm))

// Phone validation error tracking — PhoneInput instances register/deregister via inject
const phoneErrors = reactive(new Set<symbol>())
provide(phoneErrorsKey, phoneErrors)

const {
    loading,
    saving,
    detail,
    saveErrors,
    studentInfo,
    contactPermanent,
    localContact,
    emergencyContact,
    permanentContact,
    isDirty,
    hasValidationErrors,
    loadDetail,
    save,
} = useEmergencyContact()

const canEdit = computed(() => detail.value?.canEdit ?? false)
const isReadOnly = computed(() => !canEdit.value)
const canManageAccess = computed(() => checkHasOnePermission(["SVMSecure.Students.EmergencyContactAdmin"]))

// Access status (admin only)
const appOpen = ref(false)
const hasIndividualAccess = ref(false)
const togglingAccess = ref(false)

async function loadAccessStatus(): Promise<boolean> {
    const status = await emergencyContactService.getAccessStatus()
    if (!status) {
        return false
    }
    appOpen.value = status.appOpen
    hasIndividualAccess.value = status.individualGrants.some((g) => g.personId === personId.value)
    return true
}

async function handleToggleIndividualAccess(): Promise<void> {
    const studentName = detail.value?.fullName ?? "this student"
    const granting = !hasIndividualAccess.value
    $q.dialog({
        title: granting ? "Grant Individual Access" : "Revoke Individual Access",
        message: granting
            ? `Grant individual edit access to ${studentName}?`
            : `Remove individual edit access for ${studentName}?`,
        cancel: { label: "Cancel", flat: true },
        ok: { label: granting ? "Grant" : "Revoke", color: granting ? "positive" : "negative" },
        persistent: true,
    }).onOk(async () => {
        togglingAccess.value = true
        // Re-read access status to guard against concurrent changes by another admin
        const refreshed = await loadAccessStatus()
        if (!refreshed) {
            togglingAccess.value = false
            $q.notify({ type: "negative", message: "Unable to verify current access status. Please try again." })
            return
        }
        if (hasIndividualAccess.value === granting) {
            togglingAccess.value = false
            $q.notify({
                type: "warning",
                message: "Access status was already changed. Please review the current state.",
            })
            return
        }
        const result = await emergencyContactService.toggleIndividualAccess(personId.value)
        if (result !== null) {
            hasIndividualAccess.value = result
            $q.notify({
                type: "positive",
                message: result
                    ? `Individual access granted to ${studentName}.`
                    : `Individual access removed for ${studentName}.`,
            })
        }
        togglingAccess.value = false
    })
}

function getContactMissing(
    contact: {
        name?: string | null
        relationship?: string | null
        workPhone?: string | null
        homePhone?: string | null
        cellPhone?: string | null
        email?: string | null
    },
    label: string,
): string[] {
    const missing: string[] = []
    if (!contact.name) missing.push(`${label} name`)
    if (!contact.relationship) missing.push(`${label} relationship`)
    if (!contact.workPhone && !contact.homePhone && !contact.cellPhone) missing.push(`${label} phone`)
    if (!contact.email) missing.push(`${label} email`)
    return missing
}

const missingFields = computed(() => {
    const missing: string[] = []
    const si = studentInfo.value
    if (!si.address || !si.city || !si.zip) {
        missing.push("Student address")
    }
    if (!si.homePhone && !si.cellPhone) {
        missing.push("Student phone")
    }
    missing.push(...getContactMissing(localContact.value, "Local contact"))
    missing.push(...getContactMissing(emergencyContact.value, "Emergency contact"))
    missing.push(...getContactMissing(permanentContact.value, "Permanent contact"))
    return missing
})

const studentTouched: Record<string, boolean> = reactive({})

function markStudentTouched(field: string): void {
    studentTouched[field] = true
}

function updateStudentPhone(field: "homePhone" | "cellPhone", value: string | null): void {
    studentInfo.value = { ...studentInfo.value, [field]: value }
}

async function handleSave(): Promise<void> {
    if (!isDirty.value) {
        $q.notify({ type: "info", message: "No changes to save." })
        return
    }
    if (formRef.value) {
        const valid = await formRef.value.validate()
        if (!valid) {
            // Scroll to the first error field so the user sees what needs fixing
            await nextTick()
            const firstError = document.querySelector(".emergency-contact-form .q-field--error")
            if (firstError) {
                firstError.scrollIntoView({ behavior: "smooth", block: "center" })
            }
            $q.notify({ type: "negative", message: "Please fix the highlighted errors before saving." })
            return
        }
    }
    const success = await save(personId.value)
    if (success) {
        saveErrors.value = []
        $q.notify({ type: "positive", message: "Emergency contact information saved." })
        if (detail.value?.isAdmin) {
            router.push({ name: "EmergencyContactList" })
        } else {
            router.push({ name: "EmergencyContactView", params: { pidm: personId.value } })
        }
    }
}

function handleBack(): void {
    if (detail.value?.isAdmin) {
        router.push({ name: "EmergencyContactList" })
    } else {
        router.push({ name: "EmergencyContactView", params: { pidm: personId.value } })
    }
}

async function initForm(): Promise<void> {
    if (personId.value) {
        await loadDetail(personId.value)
        // Students without edit access should see the read-only view page instead
        if (detail.value && !detail.value.canEdit && !detail.value.isAdmin) {
            router.replace({ name: "EmergencyContactView", params: { pidm: personId.value } })
            return
        }
        if (canManageAccess.value) {
            loadAccessStatus()
        }
    }
}

onMounted(() => {
    initForm()
})

onBeforeRouteLeave(() => {
    if (!isDirty.value) {
        return true
    }
    return new Promise((resolve) => {
        $q.dialog({
            title: "Unsaved Changes",
            message: "You have unsaved changes. Are you sure you want to leave?",
            cancel: {
                label: "Keep Editing",
                flat: true,
            },
            ok: {
                label: "Discard Changes",
                color: "negative",
            },
            persistent: true,
        })
            .onOk(() => resolve(true))
            .onCancel(() => resolve(false))
            .onDismiss(() => resolve(false))
    })
})
</script>

<template>
    <EmergencyContactPageShell
        :loading="loading"
        :detail="detail"
    >
        <template v-if="detail">
            <h1 class="q-ma-none q-mb-md">
                Emergency Contact: {{ detail.fullName }}
                <q-badge
                    v-if="canManageAccess"
                    class="q-ml-sm vertical-top"
                    :color="appOpen ? 'positive' : hasIndividualAccess ? 'warning' : 'negative'"
                    :text-color="hasIndividualAccess && !appOpen ? 'dark' : 'white'"
                >
                    {{ appOpen ? "Editing open" : hasIndividualAccess ? "Editing allowed" : "Editing closed" }}
                </q-badge>
                <q-badge
                    v-else
                    class="q-ml-sm vertical-top"
                    :color="canEdit ? 'positive' : 'grey'"
                >
                    {{ canEdit ? "Editable" : "Read Only" }}
                </q-badge>
            </h1>

            <!-- Admin: individual access control -->
            <StatusBanner
                v-if="canManageAccess"
                type="info"
                icon="person"
            >
                <div class="q-mb-xs">
                    Individual Access:
                    <strong>{{ hasIndividualAccess ? "Granted" : "Not granted" }}</strong>
                </div>
                <q-btn
                    :label="hasIndividualAccess ? 'Revoke Access' : 'Grant Access'"
                    color="primary"
                    :loading="togglingAccess"
                    dense
                    no-caps
                    class="access-toggle-btn"
                    @click="handleToggleIndividualAccess"
                >
                    <template #loading>
                        <q-spinner
                            size="1em"
                            class="q-mr-sm"
                        />
                        {{ hasIndividualAccess ? "Revoke Access" : "Grant Access" }}
                    </template>
                </q-btn>
            </StatusBanner>

            <StatusBanner
                v-if="missingFields.length > 0"
                type="warning"
            >
                <div class="text-weight-bold q-mb-xs">Missing or incomplete information:</div>
                <ul class="q-ma-none q-pl-md">
                    <li
                        v-for="field in missingFields"
                        :key="field"
                    >
                        {{ field }}
                    </li>
                </ul>
            </StatusBanner>

            <div class="form-content text-body2 text-grey-8 q-mb-md">
                The School of Veterinary Medicine collects your personal contact information to be used for academic
                purposes to locate you in the event of an emergency, as well as for any academic issues including your
                VMTH rotations. It is important that we have other individuals who you feel we should contact in case we
                cannot contact you. Please include the most recent information and please feel free to update as
                necessary throughout the year as this is very important.
            </div>

            <StatusBanner
                v-if="saveErrors.length > 0"
                type="error"
            >
                <div
                    v-for="(error, idx) in saveErrors"
                    :key="idx"
                >
                    {{ error }}
                </div>
            </StatusBanner>

            <q-form
                ref="formRef"
                class="emergency-contact-form compact-form form-content"
            >
                <!-- Student Information -->
                <q-card
                    flat
                    bordered
                    class="q-mb-md"
                >
                    <q-card-section class="q-pb-none">
                        <div class="text-subtitle1 text-weight-bold">Student Information</div>
                        <div class="text-caption text-grey-7">
                            Your address and contact information used by the SVM to reach you
                        </div>
                    </q-card-section>
                    <q-card-section>
                        <fieldset class="contact-section-fieldset">
                            <legend class="sr-only">Student Information</legend>
                            <div class="row q-col-gutter-sm">
                                <div class="col-12 col-sm-6">
                                    <q-input
                                        v-model="studentInfo.address"
                                        label="Local Address"
                                        dense
                                        outlined
                                        :readonly="isReadOnly"
                                        @blur="markStudentTouched('address')"
                                    />
                                    <div
                                        v-if="studentTouched.address && !studentInfo.address?.trim()"
                                        class="field-warning-chip"
                                    >
                                        <q-icon
                                            name="warning"
                                            size="1rem"
                                        />
                                        Local address is recommended
                                    </div>
                                </div>
                                <div class="col-12 col-sm-3">
                                    <q-input
                                        v-model="studentInfo.city"
                                        label="City"
                                        dense
                                        outlined
                                        :readonly="isReadOnly"
                                        @blur="markStudentTouched('city')"
                                    />
                                    <div
                                        v-if="studentTouched.city && !studentInfo.city?.trim()"
                                        class="field-warning-chip"
                                    >
                                        <q-icon
                                            name="warning"
                                            size="1rem"
                                        />
                                        City is recommended
                                    </div>
                                </div>
                                <div class="col-12 col-sm-3">
                                    <q-input
                                        v-model="studentInfo.zip"
                                        label="Zip"
                                        dense
                                        outlined
                                        :readonly="isReadOnly"
                                        @blur="markStudentTouched('zip')"
                                    />
                                    <div
                                        v-if="studentTouched.zip && !studentInfo.zip?.trim()"
                                        class="field-warning-chip"
                                    >
                                        <q-icon
                                            name="warning"
                                            size="1rem"
                                        />
                                        Zip is recommended
                                    </div>
                                </div>
                                <div class="col-12 col-sm-4">
                                    <PhoneInput
                                        :model-value="studentInfo.homePhone"
                                        label="Home Phone"
                                        :readonly="isReadOnly"
                                        warn-empty
                                        @update:model-value="updateStudentPhone('homePhone', $event)"
                                    />
                                </div>
                                <div class="col-12 col-sm-4">
                                    <PhoneInput
                                        :model-value="studentInfo.cellPhone"
                                        label="Cell Phone"
                                        :readonly="isReadOnly"
                                        warn-empty
                                        @update:model-value="updateStudentPhone('cellPhone', $event)"
                                    />
                                </div>
                            </div>
                        </fieldset>
                    </q-card-section>
                </q-card>

                <!-- Local Contact -->
                <q-card
                    flat
                    bordered
                    class="q-mb-md"
                >
                    <q-card-section class="q-pb-none">
                        <div class="text-subtitle1 text-weight-bold">Local Contact</div>
                        <div class="text-caption text-grey-7">
                            Person to contact if you cannot be reached (e.g. roommate, significant other)
                        </div>
                    </q-card-section>
                    <q-card-section>
                        <ContactSection
                            v-model="localContact"
                            :readonly="isReadOnly"
                            title="Local Contact"
                            hide-title
                        />
                    </q-card-section>
                </q-card>

                <!-- Emergency Contact -->
                <q-card
                    flat
                    bordered
                    class="q-mb-md"
                >
                    <q-card-section class="q-pb-none">
                        <div class="text-subtitle1 text-weight-bold">Emergency Contact</div>
                        <div class="text-caption text-grey-7">Person to contact in case of an emergency</div>
                    </q-card-section>
                    <q-card-section>
                        <ContactSection
                            v-model="emergencyContact"
                            :readonly="isReadOnly"
                            title="Emergency Contact"
                            hide-title
                        />
                    </q-card-section>
                </q-card>

                <!-- Family/Permanent Contact -->
                <q-card
                    flat
                    bordered
                    class="q-mb-md"
                >
                    <q-card-section class="q-pb-none">
                        <div class="text-subtitle1 text-weight-bold">Family/Permanent Contact</div>
                        <div class="text-caption text-grey-7">Family or permanent contact information</div>
                    </q-card-section>
                    <q-card-section>
                        <ContactSection
                            v-model="permanentContact"
                            :readonly="isReadOnly"
                            title="Family/Permanent Contact"
                            hide-title
                        />
                        <div class="q-mt-sm">
                            <q-toggle
                                v-model="contactPermanent"
                                :disable="isReadOnly"
                                dense
                            >
                                <template #default>
                                    <span class="text-body2 q-ml-sm">
                                        Should this person be contacted in addition to the emergency contact?
                                    </span>
                                </template>
                            </q-toggle>
                        </div>
                    </q-card-section>
                </q-card>

                <div
                    v-if="detail.lastUpdated"
                    class="text-caption text-grey q-mb-md"
                >
                    Last updated: {{ new Date(detail.lastUpdated).toLocaleString() }}
                    <span v-if="detail.updatedBy"> by {{ detail.updatedBy }}</span>
                </div>

                <div
                    v-if="canEdit"
                    class="row q-gutter-sm q-mt-md"
                >
                    <q-btn
                        color="primary"
                        label="Save"
                        no-caps
                        :loading="saving"
                        :disable="hasValidationErrors || phoneErrors.size > 0"
                        @click="handleSave"
                    >
                        <template #loading>
                            <q-spinner
                                size="1em"
                                class="q-mr-sm"
                            />
                            Save
                        </template>
                    </q-btn>
                    <q-btn
                        flat
                        no-caps
                        label="Cancel"
                        @click="handleBack"
                    />
                </div>
            </q-form>
        </template>
    </EmergencyContactPageShell>
</template>

<style scoped>
.form-content {
    max-width: 56rem;
}

.access-toggle-btn {
    min-width: 9rem;
}

.contact-section-fieldset {
    border: none;
    margin: 0;
    padding: 0;
}

.field-warning-chip {
    display: inline-flex;
    align-items: center;
    margin-top: 0.25rem;
    margin-left: 0.25rem;
    background: var(--q-warning);
    color: rgb(0 0 0 / 87%);
    padding: 0.25rem 0.5rem;
    border-radius: 1rem;
    font-size: 0.75rem;
    gap: 0.125rem;
}
</style>

<!-- Shared compact form + error chip styles (unscoped, applied via .compact-form class) -->
<style>
@import url("@/styles/compact-form.css");
</style>
