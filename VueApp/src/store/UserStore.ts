import { defineStore } from "pinia"
import { computed, reactive } from "vue"

interface UserInfo {
    firstName: string
    lastName: string
    mailId: string
    loginId: string
    mothraId: string
    userId: number | null
    token: string
    emulating: boolean
    permissions: string[]
}

function createDefaultUserInfo(): UserInfo {
    return {
        firstName: "",
        lastName: "",
        mailId: "",
        loginId: "",
        mothraId: "",
        userId: null,
        token: "",
        emulating: false,
        permissions: [],
    }
}

const useUserStore = defineStore("userStore", () => {
    const userInfo = reactive<UserInfo>(createDefaultUserInfo())

    const isLoggedIn = computed(() => userInfo.loginId !== "")
    const isEmulating = computed(() => userInfo.emulating)

    function loadUser(payload: UserInfo) {
        Object.assign(userInfo, {
            firstName: payload.firstName,
            lastName: payload.lastName,
            mailId: payload.mailId,
            loginId: payload.loginId,
            mothraId: payload.mothraId,
            userId: payload.userId,
            token: payload.token,
            emulating: payload.emulating,
        })
    }

    function setPermissions(perms: string[]) {
        userInfo.permissions = perms
    }

    /**
     * Adds permissions to those already held, ignoring any already present. Avoids
     * duplicates and issues arising from concurrent modifications.
     */
    function addPermissions(perms: string[]) {
        const held = new Set(userInfo.permissions)
        userInfo.permissions = [...userInfo.permissions, ...perms.filter((p) => !held.has(p))]
    }

    function clearUser() {
        Object.assign(userInfo, {
            firstName: "",
            lastName: "",
            mailId: "",
            loginId: "",
            mothraId: "",
            userId: null,
            token: "",
            emulating: false,
        })
    }

    return { userInfo, isLoggedIn, isEmulating, loadUser, setPermissions, addPermissions, clearUser }
})

export { useUserStore }
// No file imports this by name, so fallow reports it as an unused type,
// but this is incorrect. ProfilePic.vue returns the store itself from setup(),
// so UserInfo resides in that component's inferred public type,
// and declaration emit (composite: true) has to be able to name it.
// Removing this export fails the build with TS4023.
// fallow-ignore-next-line unused-type
export type { UserInfo }
