<script setup lang="ts">
import { onMounted, computed, ref } from "vue"
import { useRoute, useRouter } from "vue-router"
import { emergencyContactService } from "../services/emergency-contact-service"
import { stripToDigits, formatPhone } from "../utils/phone"
import type { StudentContactDetail } from "../types"

const route = useRoute()
const router = useRouter()

const personId = computed(() => Number(route.params.pidm))
const loading = ref(false)
const detail = ref<StudentContactDetail | null>(null)

function handleEdit(): void {
    router.push({ name: "EmergencyContactEdit", params: { pidm: personId.value } })
}

function displayValue(value: string | null | undefined): string {
    return value || "\u2014"
}

function displayPhone(value: string | null | undefined): string {
    if (!value) {
        return "\u2014"
    }
    const digits = stripToDigits(value)
    if (digits.length === 7 || digits.length === 10) {
        return formatPhone(digits)
    }
    return value
}

async function load(): Promise<void> {
    loading.value = true
    detail.value = await emergencyContactService.getDetail(personId.value)
    loading.value = false
}

onMounted(() => {
    if (personId.value) {
        load()
    }
})
</script>

<template>
    <div class="q-pa-md">
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
            <h2 class="q-ma-none q-mb-md">
                Emergency Contact: {{ detail.fullName }}
                <q-btn
                    v-if="detail.canEdit"
                    flat
                    dense
                    no-caps
                    icon="edit"
                    label="Edit"
                    class="q-ml-sm edit-btn"
                    color="primary"
                    @click="handleEdit"
                />
            </h2>

            <div class="form-content">
                <!-- Student Info Section -->
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
                        <div class="row q-col-gutter-sm">
                            <div class="col-12 col-sm-6">
                                <div class="text-caption text-grey-7">Local Address</div>
                                <div class="text-body2">{{ displayValue(detail.studentInfo.address) }}</div>
                            </div>
                            <div class="col-12 col-sm-3">
                                <div class="text-caption text-grey-7">City</div>
                                <div class="text-body2">{{ displayValue(detail.studentInfo.city) }}</div>
                            </div>
                            <div class="col-12 col-sm-3">
                                <div class="text-caption text-grey-7">Zip</div>
                                <div class="text-body2">{{ displayValue(detail.studentInfo.zip) }}</div>
                            </div>
                            <div class="col-12 col-sm-4">
                                <div class="text-caption text-grey-7">Home Phone</div>
                                <div class="text-body2">{{ displayPhone(detail.studentInfo.homePhone) }}</div>
                            </div>
                            <div class="col-12 col-sm-4">
                                <div class="text-caption text-grey-7">Cell Phone</div>
                                <div class="text-body2">{{ displayPhone(detail.studentInfo.cellPhone) }}</div>
                            </div>
                        </div>
                    </q-card-section>
                </q-card>

                <!-- Local Contact Section -->
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
                        <div class="row q-col-gutter-sm">
                            <div class="col-12 col-sm-6">
                                <div class="text-caption text-grey-7">Name</div>
                                <div class="text-body2">{{ displayValue(detail.localContact.name) }}</div>
                            </div>
                            <div class="col-12 col-sm-6">
                                <div class="text-caption text-grey-7">Relationship</div>
                                <div class="text-body2">{{ displayValue(detail.localContact.relationship) }}</div>
                            </div>
                            <div class="col-12 col-sm-4">
                                <div class="text-caption text-grey-7">Work Phone</div>
                                <div class="text-body2">{{ displayPhone(detail.localContact.workPhone) }}</div>
                            </div>
                            <div class="col-12 col-sm-4">
                                <div class="text-caption text-grey-7">Home Phone</div>
                                <div class="text-body2">{{ displayPhone(detail.localContact.homePhone) }}</div>
                            </div>
                            <div class="col-12 col-sm-4">
                                <div class="text-caption text-grey-7">Cell Phone</div>
                                <div class="text-body2">{{ displayPhone(detail.localContact.cellPhone) }}</div>
                            </div>
                            <div class="col-12 col-sm-6">
                                <div class="text-caption text-grey-7">Email</div>
                                <div class="text-body2">{{ displayValue(detail.localContact.email) }}</div>
                            </div>
                        </div>
                    </q-card-section>
                </q-card>

                <!-- Emergency Contact Section -->
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
                        <div class="row q-col-gutter-sm">
                            <div class="col-12 col-sm-6">
                                <div class="text-caption text-grey-7">Name</div>
                                <div class="text-body2">{{ displayValue(detail.emergencyContact.name) }}</div>
                            </div>
                            <div class="col-12 col-sm-6">
                                <div class="text-caption text-grey-7">Relationship</div>
                                <div class="text-body2">
                                    {{ displayValue(detail.emergencyContact.relationship) }}
                                </div>
                            </div>
                            <div class="col-12 col-sm-4">
                                <div class="text-caption text-grey-7">Work Phone</div>
                                <div class="text-body2">{{ displayPhone(detail.emergencyContact.workPhone) }}</div>
                            </div>
                            <div class="col-12 col-sm-4">
                                <div class="text-caption text-grey-7">Home Phone</div>
                                <div class="text-body2">{{ displayPhone(detail.emergencyContact.homePhone) }}</div>
                            </div>
                            <div class="col-12 col-sm-4">
                                <div class="text-caption text-grey-7">Cell Phone</div>
                                <div class="text-body2">{{ displayPhone(detail.emergencyContact.cellPhone) }}</div>
                            </div>
                            <div class="col-12 col-sm-6">
                                <div class="text-caption text-grey-7">Email</div>
                                <div class="text-body2">{{ displayValue(detail.emergencyContact.email) }}</div>
                            </div>
                        </div>
                    </q-card-section>
                </q-card>

                <!-- Permanent Contact Section -->
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
                        <div class="row q-col-gutter-sm">
                            <div class="col-12 col-sm-6">
                                <div class="text-caption text-grey-7">Name</div>
                                <div class="text-body2">{{ displayValue(detail.permanentContact.name) }}</div>
                            </div>
                            <div class="col-12 col-sm-6">
                                <div class="text-caption text-grey-7">Relationship</div>
                                <div class="text-body2">
                                    {{ displayValue(detail.permanentContact.relationship) }}
                                </div>
                            </div>
                            <div class="col-12 col-sm-4">
                                <div class="text-caption text-grey-7">Work Phone</div>
                                <div class="text-body2">{{ displayPhone(detail.permanentContact.workPhone) }}</div>
                            </div>
                            <div class="col-12 col-sm-4">
                                <div class="text-caption text-grey-7">Home Phone</div>
                                <div class="text-body2">{{ displayPhone(detail.permanentContact.homePhone) }}</div>
                            </div>
                            <div class="col-12 col-sm-4">
                                <div class="text-caption text-grey-7">Cell Phone</div>
                                <div class="text-body2">{{ displayPhone(detail.permanentContact.cellPhone) }}</div>
                            </div>
                            <div class="col-12 col-sm-6">
                                <div class="text-caption text-grey-7">Email</div>
                                <div class="text-body2">{{ displayValue(detail.permanentContact.email) }}</div>
                            </div>
                            <div class="col-12">
                                <div class="text-caption text-grey-7">Contact in addition to emergency contact?</div>
                                <div class="text-body2">{{ detail.contactPermanent ? "Yes" : "No" }}</div>
                            </div>
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
            </div>
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
.form-content {
    max-width: 56rem;
}

.edit-btn {
    vertical-align: middle;
}
</style>
