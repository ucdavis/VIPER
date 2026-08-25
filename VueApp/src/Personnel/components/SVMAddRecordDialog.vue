<template>
    <RecordFormDialog
        :model-value="modelValue"
        title-id="svm-add-record-dialog-title"
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
        <div>Section: {{ form.section.label }}</div>

        <q-select
            v-if="!isEdit"
            v-model="form.unit"
            dense
            options-dense
            outlined
            label="Unit"
            :options="activeUnits"
            :rules="[(v: QSelectOption<any> | null) => !!v?.value || 'Please select a unit']"
            @update:model-value="onUpdatedUnit"
            hint="The unit this record belongs in"
        />
        <div v-else>Unit: {{ form.unit.label }}</div>

        <q-input
            v-model="form.location"
            dense
            outlined
            label="Location"
            maxlength="100"
            hint="The dean/director's office"
        />

        <q-input
            v-model="form.fax"
            dense
            outlined
            label="Fax"
            maxlength="25"
        />

        <PersonSelector
            v-model="form.deanDirector"
            label="Dean/Director"
            list-code=""
            @update:model-value="($event) => (form.deanDirectorPhone = $event?.phoneData?.phone ?? '')"
        ></PersonSelector>

        <q-input
            v-model="form.deanDirectorPhone"
            dense
            outlined
            label="Dean/Director Phone"
            maxlength="25"
        />
        <q-select
            v-model="form.deanDirectorInterim"
            dense
            options-dense
            outlined
            label="Dean/Director Interim/Vice Status"
            :options="interimOptions"
        />

        <PersonSelector
            v-model="form.staff"
            label="Admin Staff"
            list-code=""
            @update:model-value="($event) => (form.staffPhone = $event?.phoneData?.phone ?? '')"
        ></PersonSelector>

        <q-input
            v-model="form.staffPhone"
            dense
            outlined
            label="Admin Staff Phone"
            maxlength="25"
        />
        <q-select
            v-model="form.staffInterim"
            dense
            options-dense
            outlined
            label="Admin Staff Interim/Vice Status"
            :options="interimOptions"
        />

        <template v-if="isEdit">
            <div>
                Dean/Director Modified
                {{ formatDate(editData?.deanDirectorModifiedDate?.toString() ?? "") || "Never" }}
                <span v-if="editData?.deanDirectorModifiedBy">by {{ editData?.deanDirectorModifiedBy }}</span>
            </div>
            <div>
                Admin Staff Modified
                {{ formatDate(editData?.adminStaffModifiedDate?.toString() ?? "") || "Never" }}
                <span v-if="editData?.adminStaffModifiedBy">by {{ editData?.adminStaffModifiedBy }}</span>
            </div>
        </template>
    </RecordFormDialog>
</template>

<script setup lang="ts">
import { computed } from "vue"
import { svmUnitService } from "../services/svm-unit-service.ts"
import { useAddRecordDialog } from "../composables/use-add-record-dialog.ts"
import { useDateFunctions } from "@/composables/DateFunctions.ts"
import { getSparseAugmentedViperPerson } from "../composables/use-person-helper.ts"
import RecordFormDialog from "@/components/RecordFormDialog.vue"
import PersonSelector from "./PersonSelector.vue"
import type { QSelectOption } from "quasar"
import type {
    SVMPhoneDisplayRecord,
    SVMUnitNumberDTO,
    UnitAdminStaff,
    UnitFaxNumber,
    UnitOptions,
} from "../types/svm-phone-types"
import type { AugmentedViperPerson } from "../types/phone-types"

const props = defineProps<{
    modelValue: boolean
    section: QSelectOption<any>
    units: UnitOptions[]
    unitFaxNumbers: UnitFaxNumber[]
    unitAdminStaff: UnitAdminStaff[]
    editData?: SVMPhoneDisplayRecord | null
}>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    saved: [value: boolean]
}>()

const { formatDate } = useDateFunctions()

type SVMPhoneForm = {
    section: QSelectOption<any>
    unit: QSelectOption<any>
    fax: string
    location: string
    deanDirector: AugmentedViperPerson
    deanDirectorPhone: string
    deanDirectorInterim: QSelectOption<any>
    deanDirectorUnitPersonId: number
    staff: AugmentedViperPerson
    staffPhone: string
    staffInterim: QSelectOption<any>
    staffUnitPersonId: number
}

function emptyForm(): SVMPhoneForm {
    return {
        section: props.section,
        unit: { label: "", value: null },
        fax: "",
        location: "",
        deanDirector: getSparseAugmentedViperPerson(),
        deanDirectorPhone: "",
        deanDirectorInterim: { label: "", value: "" },
        deanDirectorUnitPersonId: -1,
        staff: getSparseAugmentedViperPerson(),
        staffPhone: "",
        staffInterim: { label: "", value: "" },
        staffUnitPersonId: -1,
    }
}

function formFromEditData(): SVMPhoneForm {
    return {
        section: { label: props.editData?.sectionName ?? "", value: null },
        unit: {
            label: props.editData?.unitName ?? "",
            value: props.editData?.unitId ?? null,
        },
        fax: props.editData?.officeFax ?? "",
        location: props.editData?.officeLocation ?? "",
        deanDirector: getSparseAugmentedViperPerson(
            props.editData?.deanDirectorFullName ?? "",
            props.editData?.deanDirectorIam ?? "",
        ),
        deanDirectorPhone: props.editData?.deanDirectorPhone ?? "",
        deanDirectorInterim: {
            label: props.editData?.deanDirectorInterim ? `(${props.editData?.deanDirectorInterim})` : "",
            value: props.editData?.deanDirectorInterim ?? "",
        },
        deanDirectorUnitPersonId: props.editData?.deanDirectorUnitPersonId ?? -1,
        staff: getSparseAugmentedViperPerson(
            props.editData?.adminStaffFullName ?? "",
            props.editData?.adminStaffIam ?? "",
        ),
        staffPhone: props.editData?.adminStaffPhone ?? "",
        staffInterim: {
            label: props.editData?.adminStaffInterim ? `(${props.editData?.adminStaffInterim})` : "",
            value: props.editData?.adminStaffInterim ?? "",
        },
        staffUnitPersonId: props.editData?.adminStaffUnitPersonId ?? -1,
    }
}

const interimOptions: QSelectOption<any>[] = [
    { label: "", value: "" },
    { label: "(Acting)", value: "Acting" },
    { label: "(Interim)", value: "Interim" },
    { label: "(Vice)", value: "Vice" },
]

function buildFormData(form: SVMPhoneForm): SVMUnitNumberDTO {
    return {
        fax: form.fax,
        location: form.location,
        deanIam: form.deanDirector.iamId,
        deanPhone: form.deanDirectorPhone,
        deanInterim: form.deanDirectorInterim.value,
        deanUnitPerson: form.deanDirectorUnitPersonId,
        staffIam: form.staff.iamId,
        staffPhone: form.staffPhone,
        staffInterim: form.staffInterim.value,
        staffUnitPerson: form.staffUnitPersonId,
    }
}

const { form, saving, formError, isEdit, save, resetForm, onValidationError } = useAddRecordDialog<
    SVMPhoneForm,
    SVMPhoneDisplayRecord
>({
    editData: () => props.editData,
    resetOn: () => props.section,
    emptyForm,
    formFromEditData,
    validate: (f) => ((f.deanDirector?.iamId ?? "") === "" ? "Must specify leadership." : null),
    sendSave: (f, editing) => {
        const dto = buildFormData(f)
        return editing
            ? svmUnitService.updateUnitData(f.unit.value, dto)
            : svmUnitService.addUnitData(f.unit.value, dto)
    },
    onSaved: (result) => emit("saved", result),
    onClose: () => emit("update:modelValue", false),
})

const activeUnits = computed(() => props.units.find((e) => e.section.toString() === form.value.section.value)?.units)

function onUpdatedUnit() {
    const faxLookup = props.unitFaxNumbers.find((e) => e.unitId.toString() === form.value.unit.value)
    if (faxLookup !== undefined && faxLookup.fax) {
        form.value.fax = faxLookup.fax
    }

    // A unit has only one admin staff, so auto-populate any existing staff's data
    // when adding a new record for the unit.
    const staffLookup = props.unitAdminStaff.find((e) => e.unitId.toString() === form.value.unit.value)
    if (staffLookup !== undefined) {
        form.value.staff = getSparseAugmentedViperPerson(staffLookup.staffFullName, staffLookup.staffIam)
        form.value.staffPhone = staffLookup.staffPhone
        form.value.staffInterim = {
            label: staffLookup.staffInterim ? `(${staffLookup.staffInterim})` : "",
            value: staffLookup.staffInterim,
        }
        form.value.staffUnitPersonId = staffLookup.staffUnitPersonId
    } else {
        form.value.staff = getSparseAugmentedViperPerson()
        form.value.staffPhone = ""
        form.value.staffInterim = { label: "", value: "" }
        form.value.staffUnitPersonId = -1
    }
}
</script>
