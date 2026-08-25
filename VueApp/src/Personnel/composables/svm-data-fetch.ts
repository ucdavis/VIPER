import { svmFrequentNumberService } from "../services/svm-frequent-number-service"
import { svmSectionService } from "../services/svm-section-service"
import { svmUnitService } from "../services/svm-unit-service"
import { getEmptyPhonePerson, getEmptySVMUnitPerson, getSparseAugmentedViperPerson } from "./use-person-helper"
import type { QSelectOption, QTableProps } from "quasar"
import type {
    SVMFrequentNumberAPIResponse,
    SVMFrequentNumberRecord,
    SVMPhoneDisplayRecord,
    SVMPhoneSection,
    SVMSectionAPIResponse,
    SVMUnitAPIResponse,
    SVMUnitPerson,
    UnitAdminStaff,
    UnitFaxNumber,
    UnitOptions,
} from "../types/svm-phone-types"

// Creates a framework for the sections in the SVM list, accounting for different
// columns in edit mode.
async function getSections(isEdit: boolean): Promise<SVMPhoneSection[]> {
    const sections: SVMPhoneSection[] = []
    const r: SVMSectionAPIResponse[] = await svmSectionService.getSections()
    r.forEach((result: SVMSectionAPIResponse) => {
        const cols: QTableProps["columns"] = []
        let { unitName } = result
        if (!unitName) {
            unitName = ""
        }
        if (isEdit) {
            cols.push(
                { name: "unitName", label: unitName, field: "unitName", align: "left", sortable: true },
                {
                    name: "deanDirector",
                    label: result.directorTitle,
                    field: "deanDirectorDisplayName",
                    align: "left",
                    sortable: true,
                },
                {
                    name: "adminStaff",
                    label: "Admin Staff",
                    field: "adminStaffDisplayName",
                    align: "left",
                    sortable: true,
                },
                { name: "edit", label: "Edit", field: "edit", align: "left" },
                { name: "delete", label: "Delete", field: "delete", align: "left" },
            )
        } else {
            cols.push({
                name: "unitName",
                label: unitName,
                field: "unitName",
                align: "left",
                sortable: true,
            })
            if (result.includeAbbrv) {
                cols.push({
                    name: "abbreviation",
                    label: "Abbrv",
                    field: "unitAbbrv",
                    align: "left",
                    sortable: true,
                })
            }
            cols.push(
                { name: "location", label: "Location", field: "officeLocation", align: "left", sortable: true },
                {
                    name: "deanDirector",
                    label: result.directorTitle,
                    field: "deanDirectorDisplayName",
                    align: "left",
                    sortable: true,
                },
                { name: "dirPhone", label: "Phone", field: "deanDirectorPhone", align: "left" },
                { name: "fax", label: "Fax", field: "officeFax", align: "left" },
                {
                    name: "adminStaff",
                    label: "Admin Staff",
                    field: "adminStaffDisplayName",
                    align: "left",
                    sortable: true,
                },
                { name: "adminPhone", label: "Phone", field: "adminStaffPhone", align: "left" },
            )
        }
        sections.push({
            rows: [],
            cols,
            title: result.name,
            id: result.sectionId,
        })
    })
    return sections
}

// Helper function to full in empty/default values for the person
// associated with an SVMUnitPerson, if needed.
function populateEmptyPerson(unitPerson: SVMUnitPerson): SVMUnitPerson {
    if (unitPerson.person === null) {
        unitPerson.person = getEmptyPhonePerson()
    }
    if (unitPerson.person.viperPerson === null) {
        unitPerson.person.viperPerson = getSparseAugmentedViperPerson()
    }
    return unitPerson
}

// Returns data about the given admin staff for inclusion in a row in an SVM table.
function getAdminStaffData(staff: SVMUnitPerson | null) {
    if (staff === null) {
        return {
            adminStaffFullName: "",
            adminStaffDisplayName: "",
            adminStaffInterim: "",
            adminStaffIam: "",
            adminStaffUnitPersonId: -1,
            adminStaffPhone: "",
            adminStaffModifiedBy: null,
            adminStaffModifiedDate: null,
        }
    }
    let staffDisplayName = ""
    // Ensures staff.person.viperPerson is not null.
    const populatedStaff = populateEmptyPerson(staff)
    staffDisplayName = populatedStaff.person!.viperPerson!.fullName
    if (populatedStaff.interim) {
        staffDisplayName += ` (${populatedStaff.interim})`
    }
    return {
        adminStaffFullName: staff.person!.viperPerson!.fullName,
        adminStaffDisplayName: staffDisplayName,
        adminStaffInterim: staff.interim ?? "",
        adminStaffIam: staff.person!.personIam,
        adminStaffUnitPersonId: staff.unitPersonId,
        adminStaffPhone: staff.person!.phone ?? "",
        adminStaffModifiedBy: staff.person!.viperModPerson?.fullName ?? null,
        adminStaffModifiedDate: staff.person!.modifiedDate,
    }
}

// Helper function to return the staff data data to populate into an add dialog box,
// or an empty array if no data should be populated.
function unitAdminStaffEntries(
    unitId: number,
    staff: SVMUnitPerson | null,
    adminStaffPartialRow: ReturnType<typeof getAdminStaffData>,
): UnitAdminStaff[] {
    if (staff === null) {
        return []
    }
    return [
        {
            unitId,
            staffIam: adminStaffPartialRow.adminStaffIam,
            staffFullName: adminStaffPartialRow.adminStaffFullName,
            staffPhone: adminStaffPartialRow.adminStaffPhone,
            staffInterim: adminStaffPartialRow.adminStaffInterim,
            staffUnitPersonId: adminStaffPartialRow.adminStaffUnitPersonId,
        },
    ]
}

// Populates and returns the data used to populate the SVM phone list and
// to autocomplete fields when adding or editing rows.
// Autopopulated data includes a unit's fax number and admin staff.
async function getSVMData(isEdit: boolean) {
    const unitOptions: UnitOptions[] = []
    const unitFaxNumbers: UnitFaxNumber[] = []
    const unitAdminStaff: UnitAdminStaff[] = []
    const [sections, allUnits] = await Promise.all([getSections(isEdit), svmUnitService.getAllUnits()])
    const unitsBySection = new Map<number, SVMUnitAPIResponse[]>()
    for (const unit of allUnits) {
        const sectionUnits = unitsBySection.get(unit.sectionId)
        if (sectionUnits === undefined) {
            unitsBySection.set(unit.sectionId, [unit])
        } else {
            sectionUnits.push(unit)
        }
    }

    for (const section of sections) {
        const r = unitsBySection.get(section.id) ?? []
        const units: QSelectOption[] = []
        const rows: SVMPhoneDisplayRecord[] = []
        r.forEach((result: SVMUnitAPIResponse) => {
            units.push({ label: result.name ?? "", value: result.unitId.toString() })
            unitFaxNumbers.push({ unitId: result.unitId, fax: result.fax ?? "" })
            if (result.unitPersons !== null) {
                const leaders: SVMUnitPerson[] = []
                let staff: SVMUnitPerson | null = null
                for (const unitPerson of result.unitPersons) {
                    if (unitPerson.posType === "Staff") {
                        staff = unitPerson
                    } else if (unitPerson?.posType) {
                        leaders.push(unitPerson)
                    }
                }
                const adminStaffPartialRow = getAdminStaffData(staff)
                // Lets the add dialog auto-populate the admin staff fields when adding another
                // leader to a unit that already has one.
                unitAdminStaff.push(...unitAdminStaffEntries(result.unitId, staff, adminStaffPartialRow))
                // The front end prevents new units from having a director but no admin staff,
                // but if the listed director is no longer a current employee,
                // we still want to display the admin staff and show that there is no active director.
                if (leaders.length === 0 && staff !== null) {
                    leaders.push(getEmptySVMUnitPerson(result.unitId))
                }
                for (let leader of leaders) {
                    // Ensures leader.person.viperPerson is not null.
                    leader = populateEmptyPerson(leader)
                    let leaderDisplayName = leader.person!.viperPerson!.fullName
                    if (leader.interim) {
                        leaderDisplayName += ` (${leader.interim})`
                    }
                    const partialRow = {
                        sectionName: section.title,
                        unitName: result.name,
                        unitId: result.unitId,
                        unitAbbrv: result.abbrv,
                        officeLocation: leader.office,
                        officeFax: result.fax,
                        deanDirectorFullName: leader.person!.viperPerson!.fullName,
                        deanDirectorDisplayName: leaderDisplayName,
                        deanDirectorInterim: leader.interim ?? "",
                        deanDirectorIam: leader.person!.personIam,
                        deanDirectorUnitPersonId: leader.unitPersonId,
                        deanDirectorPhone: leader.person!.phone ?? "",
                        deanDirectorModifiedBy: leader.person!.viperModPerson?.fullName ?? null,
                        deanDirectorModifiedDate: leader.person!.modifiedDate,
                        // The admin staff belongs to the unit, so the API keeps them until no
                        // leader row is left. Recording that here lets the delete confirmation
                        // name everyone it is about to remove.
                        isOnlyRowForUnit: leaders.length === 1,
                    }
                    // Ensure that row-key is unique by using leader's unitPersonId if it exists,
                    // or staff's if not. At least one should exist. If not, there are no
                    // people for this row, so hide it.
                    const entryId = leader.unitPersonId === -1 ? staff?.unitPersonId : leader.unitPersonId
                    if (entryId !== undefined && entryId !== -1) {
                        const entryIdObj = { entryId }
                        rows.push({ ...partialRow, ...adminStaffPartialRow, ...entryIdObj })
                    }
                }
            }
        })
        unitOptions.push({ section: section.id, units })
        section.rows = rows
    }
    return {
        newSections: sections,
        newUnitOptions: unitOptions,
        newUnitFaxNumbers: unitFaxNumbers,
        newUnitAdminStaff: unitAdminStaff,
    }
}

// Queries and returns the frequently called numbers displayed at the bottom
// of SVM phone list pages.
async function getFrequentlyCalledNumbers(): Promise<SVMFrequentNumberRecord[]> {
    const rows: SVMFrequentNumberRecord[] = []

    const r = await svmFrequentNumberService.getFrequentNumbers()
    r.forEach((result: SVMFrequentNumberAPIResponse) => {
        rows.push({
            label: result.label,
            phone: result.phone,
            entryId: result.numberId,
        })
    })

    return rows
}

export { getSVMData, getFrequentlyCalledNumbers }
