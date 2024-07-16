import { defineStore } from 'pinia'
class userObject {
    firstName = ""
    lastName = ""
    mailId = ""
    loginId = ""
    mothraId = ""
    userId = null as number | null
    token = ""
    emulating = false
    permissions = [] as string[]
}
export const useUserStore = defineStore('userStore', {
    state: () => ({
        userInfo: {
            firstName: "",
            lastName: "",
            mailId: "",
            loginId: "",
            mothraId: "",
            userId: null,
            token: "",
            emulating: false,
            permissions: []
        } as userObject
    }),
    getters: {
        isLoggedIn: (state) => state.userInfo.loginId != "",
        isEmulating: (state) => state.userInfo.emulating,
        //userPermissions: (state) => state.userInfo.permissions,
    },
    actions: {
        loadUser(payload: userObject) {
            this.userInfo.firstName = payload.firstName
            this.userInfo.lastName = payload.lastName
            this.userInfo.mailId = payload.mailId
            this.userInfo.loginId = payload.loginId
            this.userInfo.mothraId = payload.mothraId
            this.userInfo.userId = payload.userId
            this.userInfo.token = payload.token
            this.userInfo.emulating = payload.emulating
        },
        setPermissions(perms: []) {
            this.userInfo.permissions = perms
        },
        clearUser() {
            this.userInfo.firstName = ""
            this.userInfo.lastName = ""
            this.userInfo.mailId = ""
            this.userInfo.loginId = ""
            this.userInfo.mothraId = ""
            this.userInfo.userId = null
            this.userInfo.token = ""
            this.userInfo.emulating = false
        }
    }
})