import { useUserStore } from "@/store/UserStore"

export function checkHasOnePermission(permissions: string[]): boolean {
    const userStore = useUserStore()
    const userPermissions = userStore.userInfo.permissions
    for (const p of permissions) {
        if (userPermissions.includes(p)) {
            return true
        }
    }
    return false
}
