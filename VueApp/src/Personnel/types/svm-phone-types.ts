/**
 * Types for the SVM Phone List.
 */

import type { QSelectOption, QTableProps } from "quasar"
import type { ViperPerson, PhonePerson } from "./phone-types"

type SVMPhoneDisplayRecord = {
    sectionName: string | null
    unitName: string | null
    unitId: number | null
    unitAbbrv: string | null
    officeLocation: string | null
    officeFax: string | null
    deanDirectorFullName: string | null
    deanDirectorDisplayName: string | null
    deanDirectorInterim: string | null
    deanDirectorIam: string | null
    deanDirectorUnitPersonId: number | null
    deanDirectorPhone: string | null
    deanDirectorModifiedDate: Date | null
    deanDirectorModifiedBy: string | null
    adminStaffFullName: string | null
    adminStaffDisplayName: string | null
    adminStaffInterim: string | null
    adminStaffIam: string | null
    adminStaffUnitPersonId: number | null
    adminStaffPhone: string | null
    adminStaffModifiedDate: Date | null
    adminStaffModifiedBy: string | null
    entryId: number
    /**
     * True when this is the only row the unit produces. Deleting it therefore takes the admin
     * staff with it, since no other row would be left to list them.
     */
    isOnlyRowForUnit: boolean
}

type SVMFrequentNumberRecord = {
    label: string
    phone: string
    entryId: number
}

type SVMPhoneSection = {
    title: string
    id: number
    cols: QTableProps["columns"]
    rows: SVMPhoneDisplayRecord[]
}

type SVMSectionAPIResponse = {
    sectionId: number
    name: string
    includeAbbrv: boolean
    unitName: string | null
    directorTitle: string | null
    sortOrder: number
}

type SVMUnitPerson = {
    unitPersonId: number
    unitId: number
    personIam: string
    office: string | null
    posType: string | null
    interim: string | null
    modifiedDate: Date | null
    modifiedBy: string | null
    person: PhonePerson | null
    viperModPerson: ViperPerson | null
}

type SVMUnitAPIResponse = {
    unitId: number
    sectionId: number
    name: string | null
    abbrv: string | null
    sortOrder: number | null
    fax: string | null
    unitPersons: SVMUnitPerson[] | null
}

type SVMFrequentNumberAPIResponse = {
    numberId: number
    label: string
    phone: string
    sortOrder: number | null
}

type UnitOptions = {
    section: number
    units: QSelectOption[]
}

type UnitFaxNumber = {
    unitId: number
    fax: string
}

type UnitAdminStaff = {
    unitId: number
    staffIam: string
    staffFullName: string
    staffPhone: string
    staffInterim: string
    staffUnitPersonId: number
}

type SVMUnitNumberDTO = {
    fax: string
    location: string
    deanIam: string
    deanPhone: string
    deanInterim: string
    deanUnitPerson: number
    staffIam: string
    staffPhone: string
    staffInterim: string
    staffUnitPerson: number
}

export type {
    SVMPhoneDisplayRecord,
    SVMFrequentNumberRecord,
    SVMPhoneSection,
    SVMSectionAPIResponse,
    SVMUnitPerson,
    SVMUnitAPIResponse,
    SVMFrequentNumberAPIResponse,
    UnitOptions,
    UnitFaxNumber,
    UnitAdminStaff,
    SVMUnitNumberDTO,
}
