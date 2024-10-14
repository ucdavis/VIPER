import { useUserStore } from '@/store/UserStore'

export default function checkHasOnePermission(permissions: string[]): boolean {
    const userStore = useUserStore()
    const userPermissions = userStore.userInfo.permissions
    for (const p of permissions) {
        if (userPermissions.indexOf(p) >= 0) {
            return true
        }
    }
    return false
}
