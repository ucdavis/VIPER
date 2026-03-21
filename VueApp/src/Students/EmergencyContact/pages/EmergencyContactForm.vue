<script setup lang="ts">
import { ref, reactive, onMounted, computed, nextTick } from "vue"
import { useRoute, useRouter, onBeforeRouteLeave } from "vue-router"
import { useQuasar } from "quasar"
import ContactSection from "../components/ContactSection.vue"
import PhoneInput from "../components/PhoneInput.vue"
import { useEmergencyContact } from "../composables/use-emergency-contact"

const route = useRoute()
const router = useRouter()
const $q = useQuasar()
const formRef = ref<{ validate: () => Promise<boolean> } | null>(null)

const personId = computed(() => Number(route.params.pidm))

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

const missingFields = computed(() => {
    const missing: string[] = []
    const si = studentInfo.value
    if (!si.address) {
        missing.push("Student local address")
    }
    if (!si.city) {
        missing.push("Student city")
    }
    if (!si.zip) {
        missing.push("Student zip")
    }
    if (!si.homePhone) {
        missing.push("Student home phone")
    }
    if (!si.cellPhone) {
        missing.push("Student cell phone")
    }
    const lc = localContact.value
    if (!lc.name) {
        missing.push("Local contact name")
    }
    if (!lc.relationship) {
        missing.push("Local contact relationship")
    }
    if (!lc.workPhone) {
        missing.push("Local contact work phone")
    }
    if (!lc.homePhone) {
        missing.push("Local contact home phone")
    }
    if (!lc.cellPhone) {
        missing.push("Local contact cell phone")
    }
    if (!lc.email) {
        missing.push("Local contact email")
    }
    const ec = emergencyContact.value
    if (!ec.name) {
        missing.push("Emergency contact name")
    }
    if (!ec.relationship) {
        missing.push("Emergency contact relationship")
    }
    if (!ec.workPhone) {
        missing.push("Emergency contact work phone")
    }
    if (!ec.homePhone) {
        missing.push("Emergency contact home phone")
    }
    if (!ec.cellPhone) {
        missing.push("Emergency contact cell phone")
    }
    if (!ec.email) {
        missing.push("Emergency contact email")
    }
    const pc = permanentContact.value
    if (!pc.name) {
        missing.push("Permanent contact name")
    }
    if (!pc.relationship) {
        missing.push("Permanent contact relationship")
    }
    if (!pc.workPhone) {
        missing.push("Permanent contact work phone")
    }
    if (!pc.homePhone) {
        missing.push("Permanent contact home phone")
    }
    if (!pc.cellPhone) {
        missing.push("Permanent contact cell phone")
    }
    if (!pc.email) {
        missing.push("Permanent contact email")
    }
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

onMounted(() => {
    if (personId.value) {
        loadDetail(personId.value)
    }
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
    <div>
        <q-breadcrumbs class="q-mb-sm">
            <q-breadcrumbs-el
                label="Emergency Contacts"
                :to="detail?.isAdmin ? { name: 'EmergencyContactList' } : undefined"
            />
            <q-breadcrumbs-el :label="detail?.fullName ?? 'Loading...'" />
        </q-breadcrumbs>

        <q-spinner
            v-if="loading"
            color="primary"
            size="2rem"
            class="q-ma-lg"
            aria-label="Loading contact information"
        />

        <template v-else-if="detail">
            <div class="row items-baseline q-mb-md">
                <h2 class="q-ma-none">Emergency Contact: {{ detail.fullName }}</h2>
                <q-badge
                    class="q-ml-md"
                    :color="canEdit ? 'positive' : 'grey'"
                >
                    {{ canEdit ? "Editable" : "Read Only" }}
                </q-badge>
            </div>

            <q-banner
                v-if="missingFields.length > 0"
                class="bg-warning text-dark q-mb-md"
                rounded
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
            </q-banner>

            <div
                class="text-body2 text-grey-8 q-mb-md"
                style="max-width: 56rem"
            >
                The School of Veterinary Medicine collects your personal contact information to be used for academic
                purposes to locate you in the event of an emergency, as well as for any academic issues including your
                VMTH rotations. It is important that we have other individuals who you feel we should contact in case we
                cannot contact you. Please include the most recent information and please feel free to update as
                necessary throughout the year as this is very important.
            </div>

            <q-banner
                v-if="saveErrors.length > 0"
                class="bg-negative text-white q-mb-md"
                rounded
                role="alert"
                aria-live="assertive"
            >
                <div
                    v-for="(error, idx) in saveErrors"
                    :key="idx"
                >
                    {{ error }}
                </div>
            </q-banner>

            <q-form
                ref="formRef"
                class="emergency-contact-form"
                style="max-width: 56rem"
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
                        :disable="!isDirty || hasValidationErrors"
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

        <q-banner
            v-else
            class="bg-warning q-mt-md"
            rounded
        >
            Student contact record not found.
        </q-banner>
    </div>
</template>

<style scoped>
.contact-section-fieldset {
    border: none;
    margin: 0;
    padding: 0;
}

.sr-only {
    position: absolute;
    width: 1px;
    height: 1px;
    padding: 0;
    margin: -1px;
    overflow: hidden;
    clip: rect(0, 0, 0, 0);
    white-space: nowrap;
    border: 0;
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

<!-- Unscoped so error chip styles apply to q-field children in nested components -->
<style>
/* Compact field bottom area — no reserved space for errors */
.emergency-contact-form .q-field.q-field--with-bottom {
    padding-bottom: 0;
}

.emergency-contact-form .q-field .q-field__bottom {
    position: relative;
    min-height: 0;
    padding-top: 0;
    transform: none;
}

/* Error chip styling matching Effort system */
.emergency-contact-form .q-field--error .q-field__bottom {
    padding-top: 0.25rem;
}

.emergency-contact-form .q-field--error .q-field__messages {
    display: inline-flex;
    align-items: center;
    background: var(--q-negative);
    color: white !important;
    flex: none;
    width: fit-content;
    padding: 0.25rem 0.5rem;
    border-radius: 1rem;
    font-size: 0.75rem;
    gap: 0.125rem;
}

.emergency-contact-form .q-field--error .q-field__messages::before {
    content: "error";
    font-family: "Material Icons";
    font-size: 1rem;
    line-height: 1;
}
</style>
