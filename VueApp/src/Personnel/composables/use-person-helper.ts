import type { PhonePerson, AugmentedViperPerson } from "../types/phone-types"
import type { SVMUnitPerson } from "../types/svm-phone-types"

// Helper functions to populate empty or nearly-empty AugmentedViperPersons,
// PhonePersons, and SVMUnitPerson.
function getEmptySVMUnitPerson(unitId: number): SVMUnitPerson {
    return {
        unitPersonId: -1,
        unitId,
        personIam: "",
        office: "",
        posType: "",
        interim: "",
        modifiedDate: null,
        modifiedBy: "",
        unit: null,
        person: getEmptyPhonePerson(),
        viperModPerson: null,
    }
}

function getEmptyPhonePerson(): PhonePerson {
    return {
        personIam: "",
        phone: "",
        directPhone: "",
        office: "",
        modifiedDate: null,
        modifiedBy: "",
        unitPersons: null,
        phoneListUnitPersons: null,
        viperPerson: getSparseAugmentedViperPerson(),
        viperModPerson: getSparseAugmentedViperPerson(),
    }
}

function getSparseAugmentedViperPerson(fullName = "", iamId = ""): AugmentedViperPerson {
    return {
        personId: -1,
        firstName: "",
        lastName: "",
        fullName,
        iamId,
        currentEmployee: true,
        mailId: "",
        phoneData: null,
    }
}

export { getEmptySVMUnitPerson, getEmptyPhonePerson, getSparseAugmentedViperPerson }
