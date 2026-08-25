<template>
    <RecordFormDialog
        :model-value="modelValue"
        title-id="phone-list-add-record-dialog-title"
        :title="isEdit ? 'Edit Phone Record' : 'Add Phone Record'"
        :is-edit="isEdit"
        :saving="saving"
        :form-error="formError"
        submit-label="Upload"
        @update:model-value="emit('update:modelValue', $event)"
        @submit="save"
        @validation-error="onValidationError"
        @hide="resetForm"
    >
        <div>Unit: {{ form.unit.name }}</div>

        <PersonSelector
            v-if="!isEdit"
            v-model="form.employee"
            label="Employee"
            :list-code="listCode"
            @update:model-value="($event) => updateForm($event)"
        ></PersonSelector>
        <div v-else>Employee: {{ form.employee.fullName }}</div>

        <q-input
            v-model="form.phone"
            dense
            outlined
            label="Public Phone"
            maxlength="25"
        />

        <q-input
            v-model="form.directPhone"
            dense
            outlined
            label="Direct Phone"
            maxlength="25"
        />

        <q-input
            v-model="form.office"
            dense
            outlined
            label="Office"
            maxlength="100"
            hint="The employee's office"
        />

        <div>
            <q-checkbox
                v-model="form.listFirst"
                label="List name first in unit"
            />
        </div>

        <template v-if="isEdit">
            <div>Modified By: {{ editData?.modifiedBy }}</div>
            <div>
                Modified Date:
                {{ formatDate(editData?.modifiedDate?.toString() ?? "") || "Never" }}
            </div>
        </template>
    </RecordFormDialog>
</template>

<script setup lang="ts">
import { phoneListUnitService } from "../services/phone-list-unit-service.ts"
import { useAddRecordDialog } from "../composables/use-add-record-dialog.ts"
import { useDateFunctions } from "@/composables/DateFunctions.ts"
import { getSparseAugmentedViperPerson } from "../composables/use-person-helper.ts"
import RecordFormDialog from "@/components/RecordFormDialog.vue"
import PersonSelector from "./PersonSelector.vue"
import type { AugmentedViperPerson } from "../types/phone-types.ts"
import type { PhoneListDisplayRecord, PhoneListUnitPersonDTO } from "../types/phone-list-phone-types.ts"

const props = defineProps<{
    modelValue: boolean
    unit: {
        name: string
        id: number
    }
    editData?: PhoneListDisplayRecord | null
    listCode: string
}>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    saved: [value: boolean]
}>()

const { formatDate } = useDateFunctions()

type PhoneListAddForm = {
    unit: {
        name: string
        id: number
    }
    office: string
    employee: AugmentedViperPerson
    phone: string
    directPhone: string
    listFirst: boolean
    employeeUnitPersonId: number
}

function emptyForm(): PhoneListAddForm {
    return {
        unit: {
            name: props.unit.name ?? "",
            id: props.unit.id ?? -1,
        },
        office: "",
        employee: getSparseAugmentedViperPerson(),
        phone: "",
        directPhone: "",
        listFirst: false,
        employeeUnitPersonId: -1,
    }
}

/** A field the record does not carry shows as blank on the form. */
function text(value: string | null | undefined): string {
    return value ?? ""
}

function formFromEditData(): PhoneListAddForm {
    // The dialog re-derives its form whenever editData changes, including when it is cleared on
    // close, so treat "nothing to edit" as a record with no fields set rather than repeating a
    // null check against every one of them.
    const record: Partial<PhoneListDisplayRecord> = props.editData ?? {}
    return {
        unit: {
            name: text(record.unitName),
            id: record.unitId ?? -1,
        },
        office: text(record.office),
        employee: getSparseAugmentedViperPerson(text(record.name), text(record.employeeIam)),
        phone: text(record.phone),
        directPhone: text(record.directPhone),
        listFirst: record.listFirst ?? false,
        employeeUnitPersonId: record.unitPersonId ?? -1,
    }
}

function buildFormData(form: PhoneListAddForm): PhoneListUnitPersonDTO {
    return {
        unitId: form.unit.id,
        office: form.office,
        employeeIam: form.employee.iamId,
        phone: form.phone,
        directPhone: form.directPhone,
        listFirst: form.listFirst,
    }
}

const { form, saving, formError, isEdit, save, resetForm, onValidationError } = useAddRecordDialog<
    PhoneListAddForm,
    PhoneListDisplayRecord
>({
    editData: () => props.editData,
    resetOn: () => props.unit,
    emptyForm,
    formFromEditData,
    validate: (f) => ((f.employee?.iamId ?? "") === "" ? "Please select an employee." : null),
    sendSave: (f, editing) => {
        const dto = buildFormData(f)
        return editing
            ? phoneListUnitService.updateUnitPersonData(props.listCode, f.employeeUnitPersonId, dto)
            : phoneListUnitService.addUnitPersonData(props.listCode, dto)
    },
    onSaved: (result) => emit("saved", result),
    onClose: () => emit("update:modelValue", false),
})

function updateForm(person: AugmentedViperPerson) {
    form.value.phone = person?.phoneData?.phone ?? ""
    form.value.directPhone = person?.phoneData?.directPhone ?? ""
    form.value.office = person?.phoneData?.office ?? ""
}
</script>
