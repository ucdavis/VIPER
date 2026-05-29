<script setup lang="ts">
import { ref, watch, computed, nextTick, inject, onUnmounted } from "vue"
import { useFormChild } from "quasar"
import { stripToDigits, formatPhone, isValidPhone } from "../utils/phone"
import { phoneErrorsKey } from "../utils/phone-errors-key"

const props = defineProps<{
    modelValue: string | null
    label: string
    readonly?: boolean
    warnEmpty?: boolean
}>()

const emit = defineEmits<{
    (e: "update:modelValue", value: string | null): void
}>()

const rawDigits = ref("")
const inputValue = ref("")
const useMask = ref(true)
const touched = ref(false)
const isValid = ref(true)

// Report phone errors to parent form via provide/inject
const phoneInputId = Symbol()
const phoneErrors = inject(phoneErrorsKey, undefined)

watch(isValid, (valid) => {
    if (!phoneErrors) return
    if (!valid) phoneErrors.add(phoneInputId)
    else phoneErrors.delete(phoneInputId)
})

onUnmounted(() => phoneErrors?.delete(phoneInputId))

const inputMask = computed(() => (useMask.value ? "(###) ###-####" : ""))

/** Set display mode: 7-digit uses plain text, everything else uses the mask. */
function setDisplay(digits: string): void {
    rawDigits.value = digits
    if (digits.length === 7) {
        // Remove mask first, then set formatted value after Quasar processes the mask removal
        useMask.value = false
        nextTick(() => {
            inputValue.value = formatPhone(digits)
        })
    } else {
        useMask.value = true
        inputValue.value = digits
    }
}

// Initialize from prop
setDisplay(props.modelValue ? stripToDigits(props.modelValue) : "")

/** Format error: red chip */
const formatError = computed(() => {
    if (!touched.value || !rawDigits.value) {
        return null
    }
    if (!isValid.value) {
        return "Enter a 7 or 10-digit phone number"
    }
    return null
})

/** Empty warning: yellow chip */
const emptyWarning = computed(() => {
    if (!touched.value || !props.warnEmpty) {
        return null
    }
    if (!rawDigits.value) {
        return `${props.label} is recommended`
    }
    return null
})

watch(
    () => props.modelValue,
    (val) => {
        const d = val ? stripToDigits(val) : ""
        // Skip when parent echoes null back for invalid input — preserves error state
        if (d !== rawDigits.value && isValid.value) {
            setDisplay(d)
        }
    },
)

function onFocus(): void {
    if (!useMask.value) {
        // Switch from 7-digit plain display to mask mode for editing
        useMask.value = true
        inputValue.value = rawDigits.value
    }
}

function onBlur(): void {
    touched.value = true
    const d = stripToDigits(inputValue.value)
    rawDigits.value = d

    if (!d) {
        useMask.value = true
        inputValue.value = ""
        isValid.value = true
        emit("update:modelValue", null)
        return
    }

    isValid.value = isValidPhone(d)
    if (isValid.value) {
        emit("update:modelValue", d)
        setDisplay(d)
    } else {
        // Keep invalid digits visible so the error chip is meaningful
        useMask.value = false
        nextTick(() => {
            inputValue.value = d
        })
        emit("update:modelValue", null)
    }
}

/**
 * Called by parent q-form validate(). Only fails for format errors, not empty fields.
 */
function validate(): boolean {
    touched.value = true
    const d = stripToDigits(inputValue.value)
    if (d && !isValidPhone(d)) {
        isValid.value = false
        return false
    }
    isValid.value = true
    rawDigits.value = d
    return true
}

function resetValidation(): void {
    touched.value = false
    isValid.value = true
}

useFormChild({ validate, resetValidation, requiresQForm: true })

defineExpose({ validate })
</script>

<template>
    <div>
        <q-input
            v-model="inputValue"
            :label="label"
            type="tel"
            dense
            outlined
            :readonly="readonly"
            :mask="inputMask"
            unmasked-value
            :error="touched && !isValid"
            hide-bottom-space
            no-error-icon
            :aria-label="label"
            @focus="onFocus"
            @blur="onBlur"
        />
        <div
            v-if="formatError"
            class="phone-error-chip"
        >
            <q-icon
                name="error"
                size="1rem"
            />
            {{ formatError }}
        </div>
        <div
            v-else-if="emptyWarning"
            class="phone-warning-chip"
        >
            <q-icon
                name="warning"
                size="1rem"
            />
            {{ emptyWarning }}
        </div>
    </div>
</template>

<style scoped>
.phone-error-chip {
    display: inline-flex;
    align-items: center;
    margin-top: 0.25rem;
    background: var(--q-negative);
    color: white;
    padding: 0.25rem 0.5rem;
    border-radius: 1rem;
    font-size: 0.75rem;
    gap: 0.125rem;
}

.phone-warning-chip {
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
