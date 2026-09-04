import { phoneListUnitService } from "../services/phone-list-unit-service"
import { getSparseAugmentedViperPerson } from "./use-person-helper"
import type { QTableProps } from "quasar"
import type { PhoneListUnit, PhoneListDisplayRecord } from "../types/phone-list-phone-types"

/**
 * The columns a phone-list table shows. Direct numbers are for maintainers and for the people on
 * the list itself; the row controls are for maintainers alone. The backend enforces both
 * independently, so this only decides what is rendered.
 */
function buildColumns(isEdit: boolean, isInternal: boolean): QTableProps["columns"] {
    const cols: QTableProps["columns"] = [
        { name: "name", label: "Name", field: "name", align: "left", sortable: true },
        { name: "phone", label: "Phone", field: "phone", align: "left", sortable: false },
    ]
    if (isInternal || isEdit) {
        cols.push({
            name: "directPhone",
            label: "Direct Phone",
            field: "directPhone",
            align: "left",
            sortable: false,
        })
    }
    cols.push({ name: "office", label: "Office", field: "office", align: "left", sortable: false })
    if (isEdit) {
        cols.push(
            {
                name: "listFirst",
                label: "List First",
                field: "listFirst",
                align: "center",
                sortable: false,
                // The table draws a tick through its own cell slot, but a card list has no slot to
                // draw into and would otherwise print the raw boolean. Formatting here rather than
                // in the card keeps one answer for what this column says, and an unset flag reads
                // as nothing at all rather than as "No".
                format: (listFirst: boolean) => (listFirst ? "Yes" : ""),
            },
            {
                name: "edit",
                label: "Edit",
                field: "edit",
                align: "left",
                sortable: false,
            },
            {
                name: "delete",
                label: "Delete",
                field: "delete",
                align: "left",
                sortable: false,
            },
        )
    }
    return cols
}

// Retrieve all units and associated people for the given list code (e.g., VDMO).
// The data included depends on whether this is for editing or displaying the list.
// isInternal also affects displayed columns, but the backend independently
// enforces permissions to ensure no unintended data gets through.
async function getPhoneListData(code: string, isEdit: boolean, isInternal: boolean) {
    const units: PhoneListUnit[] = []
    const r = await phoneListUnitService.getUnitsByList(code)
    for (const unit of r) {
        const rows: PhoneListDisplayRecord[] = []
        const cols = buildColumns(isEdit, isInternal)
        for (const unitPerson of unit.phoneListUnitPersons) {
            // If a person is no longer an active employee,
            // person or viperPerson may be null.
            // Display only active employees.
            if (unitPerson.person !== null) {
                if (unitPerson.person.viperPerson === null) {
                    unitPerson.person.viperPerson = getSparseAugmentedViperPerson()
                }
                rows.push({
                    unitId: unit.phoneListUnitId,
                    unitPersonId: unitPerson.phoneListUnitPersonId,
                    fullName: unitPerson.person.viperPerson.fullName,
                    name: `${unitPerson.person.viperPerson.lastName}, ${unitPerson.person.viperPerson.firstName}`,
                    employeeIam: unitPerson.person.personIam,
                    employeeMailId: unitPerson.person.viperPerson.mailId,
                    phone: unitPerson.person.phone,
                    directPhone: unitPerson.person.directPhone,
                    office: unitPerson.person.office,
                    listFirst: unitPerson.listFirst,
                    unitName: unit.name,
                    modifiedBy: unitPerson.viperModPerson?.fullName ?? "",
                    modifiedDate: unitPerson.modifiedDate,
                })
            }
        }
        units.push({
            name: unit.name,
            id: unit.phoneListUnitId,
            cols,
            rows,
        })
    }
    return units
}

export { getPhoneListData }
