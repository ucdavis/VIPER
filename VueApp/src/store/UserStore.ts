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

    return { userInfo, isLoggedIn, isEmulating, loadUser, setPermissions, clearUser }
})

export { useUserStore }
export type { UserInfo }
