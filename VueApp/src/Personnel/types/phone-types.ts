/**
 * Types for general phone records reports.
 */

type ViperPerson = {
    personId: number
    firstName: string
    lastName: string
    fullName: string
    iamId: string
    currentEmployee: boolean
    mailId: string
}

type PhonePerson = {
    personIam: string
    phone: string | null
    directPhone: string | null
    office: string | null
    modifiedDate: Date | null
    modifiedBy: string | null
    viperPerson: ViperPerson | null
    viperModPerson: ViperPerson | null
}

type AugmentedViperPerson = {
    personId: number
    firstName: string
    lastName: string
    fullName: string
    iamId: string
    currentEmployee: boolean
    mailId: string
    phoneData: PhonePerson | null
}

export type { ViperPerson, PhonePerson, AugmentedViperPerson }
