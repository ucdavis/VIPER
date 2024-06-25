import { defineStore } from 'pinia'
class userObject {
    firstName = ""
    lastName = ""
    mailId = ""
    loginId = ""
    mothraId = ""
    userId = ""
    token = ""
    emulating = false

}
export const useUserStore = defineStore('userStore', {
    state: () => ({
        userInfo: {
            firstName: "",
            lastName: "",
            mailId: "",
            loginId: "",
            mothraId: "",
            userId: "",
            token: "",
            emulating: false
        }
    }),
    getters: {
        isLoggedIn: (state) => state.userInfo.loginId != "",
        isEmulating: (state) => state.userInfo.emulating
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
        clearUser() {
            this.userInfo.firstName = ""
            this.userInfo.lastName = ""
            this.userInfo.mailId = ""
            this.userInfo.loginId = ""
            this.userInfo.mothraId = ""
            this.userInfo.userId = ""
            this.userInfo.token = ""
            this.userInfo.emulating = false
        }
    }
})