<script setup lang="ts">
import { reactive } from "vue"
import { useVModel } from "@vueuse/core"
import { patterns } from "quasar"
import type { ContactInfo } from "../types"
import PhoneInput from "./PhoneInput.vue"

const props = defineProps<{
    modelValue: ContactInfo
    readonly: boolean
    title: string
    helperText?: string
    hideTitle?: boolean
}>()

const emit = defineEmits<{
    (e: "update:modelValue", value: ContactInfo): void
}>()

const contact = useVModel(props, "modelValue", emit)

const touched: Record<string, boolean> = reactive({})

function markTouched(field: string): void {
    touched[field] = true
}

function updatePhone(field: "workPhone" | "homePhone" | "cellPhone", value: string | null): void {
    contact.value = { ...contact.value, [field]: value }
}

function isValidEmail(val: string): boolean | string {
    if (!val) return true
    return patterns.testPattern.email(val) || "Please enter a valid email address"
}
</script>

<template>
    <fieldset :class="['contact-section-fieldset', { 'q-mb-md': !hideTitle }]">
        <legend :class="hideTitle ? 'sr-only' : 'text-subtitle1 text-weight-bold'">{{ title }}</legend>
        <div
            v-if="helperText"
            class="text-caption text-grey-7 q-mb-sm"
        >
            {{ helperText }}
        </div>
        <div class="row q-col-gutter-sm">
            <div class="col-12 col-sm-6">
                <q-input
                    v-model="contact.name"
                    label="Name"
                    dense
                    outlined
                    :readonly="readonly"
                    @blur="markTouched('name')"
                />
                <div
                    v-if="touched.name && !contact.name?.trim()"
                    class="field-warning-chip"
                >
                    <q-icon
                        name="warning"
                        size="1rem"
                    />
                    Name is recommended
                </div>
            </div>
            <div class="col-12 col-sm-6">
                <q-input
                    v-model="contact.relationship"
                    label="Relationship"
                    dense
                    outlined
                    :readonly="readonly"
                    @blur="markTouched('relationship')"
                />
                <div
                    v-if="touched.relationship && !contact.relationship?.trim()"
                    class="field-warning-chip"
                >
                    <q-icon
                        name="warning"
                        size="1rem"
                    />
                    Relationship is recommended
                </div>
            </div>
            <div class="col-12 col-sm-4">
                <PhoneInput
                    :model-value="contact.workPhone"
                    label="Work Phone"
                    :readonly="readonly"
                    warn-empty
                    @update:model-value="updatePhone('workPhone', $event)"
                />
            </div>
            <div class="col-12 col-sm-4">
                <PhoneInput
                    :model-value="contact.homePhone"
                    label="Home Phone"
                    :readonly="readonly"
                    warn-empty
                    @update:model-value="updatePhone('homePhone', $event)"
                />
            </div>
            <div class="col-12 col-sm-4">
                <PhoneInput
                    :model-value="contact.cellPhone"
                    label="Cell Phone"
                    :readonly="readonly"
                    warn-empty
                    @update:model-value="updatePhone('cellPhone', $event)"
                />
            </div>
            <div class="col-12 col-sm-6">
                <q-input
                    v-model="contact.email"
                    label="Email"
                    type="email"
                    dense
                    outlined
                    :readonly="readonly"
                    :rules="[isValidEmail]"
                    lazy-rules
                    @blur="markTouched('email')"
                />
                <div
                    v-if="touched.email && !contact.email?.trim()"
                    class="field-warning-chip"
                >
                    <q-icon
                        name="warning"
                        size="1rem"
                    />
                    Email is recommended
                </div>
            </div>
        </div>
    </fieldset>
</template>

<style scoped>
.contact-section-fieldset {
    border: none;
    margin: 0;
    padding: 0;
}

.contact-section-fieldset > legend {
    padding: 0;
    margin-bottom: 0.5rem;
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
