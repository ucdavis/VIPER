/**
 * Types for department Phone Lists.
 */

import type { QTableProps } from "quasar"
import type { PhonePerson, ViperPerson } from "./phone-types"

type PhoneListInfo = {
    phoneListId: number
    code: string
    name: string
    canMaintain: boolean
    canViewDirectPhone: boolean
}

type PhoneListUnitPersonDTO = {
    unitId: number
    office: string
    employeeIam: string
    phone: string
    directPhone: string
    listFirst: boolean
}

type PhoneListUnitPerson = {
    phoneListUnitPersonId: number
    phoneListUnitId: number
    personIam: string
    listFirst: boolean
    phoneListUnit: null
    person: PhonePerson | null
    modifiedBy: string | null
    modifiedDate: Date | null
    viperModPerson: ViperPerson | null
}

type PhoneListUnitAPIResponse = {
    phoneListUnitId: number
    phoneListId: number
    name: string
    sortOrder: number | null
    // Key returned by API but always null for this use case.
    phoneList: null
    phoneListUnitPersons: PhoneListUnitPerson[]
}

type PhoneListDisplayRecord = {
    fullName: string
    name: string
    employeeIam?: string
    employeeMailId: string
    phone: string | null
    directPhone?: string | null
    office: string | null
    listFirst: boolean
    unitPersonId: number
    unitId: number
    unitName: string
    modifiedBy: string | null
    modifiedDate: Date | null
}

type PhoneListUnit = {
    name: string
    id: number
    cols: QTableProps["columns"]
    rows: PhoneListDisplayRecord[]
}

export type {
    PhoneListInfo,
    PhoneListUnitAPIResponse,
    PhoneListUnitPerson,
    PhoneListUnitPersonDTO,
    PhoneListUnit,
    PhoneListDisplayRecord,
}
