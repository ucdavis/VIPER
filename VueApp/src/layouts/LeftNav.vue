<template>
    <q-drawer
        v-model="myMainLeftDrawer"
        show-if-above
        elevated
        side="left"
        :mini="!myMainLeftDrawer"
        no-mini-animation
        :width="300"
        id="mainLeftDrawer"
        class="no-print"
    >
        <template #default>
            <div
                class="q-pa-sm"
                id="leftNavMenu"
            >
                <q-btn
                    dense
                    round
                    unelevated
                    color="secondary"
                    icon="close"
                    class="float-right lt-md"
                    @click="myMainLeftDrawer = false"
                />
                <h2 v-if="navHeader.length">
                    {{ navHeader }}
                </h2>
                <q-list
                    dense
                    separator
                >
                    <template
                        v-for="(menuItem, index) in menuItems"
                        :key="index"
                    >
                        <q-item
                            v-if="menuItem.routeTo != null"
                            :clickable="menuItem.clickable"
                            :v-ripple="menuItem.clickable"
                            :to="menuItem.routeTo"
                            :class="menuItem.displayClass"
                        >
                            <q-item-section>
                                <q-item-label lines="1">
                                    {{ menuItem.menuItemText }}
                                </q-item-label>
                            </q-item-section>
                        </q-item>
                        <q-item
                            v-else
                            :clickable="menuItem.clickable"
                            :v-ripple="menuItem.clickable"
                            :href="menuItem.menuItemUrl"
                            target="_blank"
                            :class="menuItem.displayClass"
                        >
                            <q-item-section>
                                <q-item-label lines="1">
                                    {{ menuItem.menuItemText }}
                                </q-item-label>
                            </q-item-section>
                        </q-item>
                    </template>
                </q-list>
            </div>
        </template>
    </q-drawer>
</template>

<script setup lang="ts">
import { ref, watch, onMounted } from "vue"
import { useFetch } from "@/composables/ViperFetch"

interface MenuItem {
    menuItemUrl: string | undefined
    routeTo: string | null
    menuItemText: string
    clickable: boolean
    displayClass: string
}

const props = defineProps<{
    nav?: string
    navarea?: boolean
    mainLeftDrawer?: boolean
}>()

const emit = defineEmits<{
    (e: "drawer-change", value: boolean): void
}>()

const myMainLeftDrawer = ref(false)
const navHeader = ref("")
const menuItems = ref<MenuItem[]>([])

async function getLeftNav() {
    try {
        const u = new URL(
            import.meta.env.VITE_API_URL + "layout/leftnav/?area=" + props.navarea + "&nav=" + props.nav,
            document.baseURI,
        )
        const { get } = useFetch()
        const r = await get(u.toString())
        if (!r.success || !r.result) {
            navHeader.value = ""
            menuItems.value = []
            return
        }
        navHeader.value = r.result.menuHeaderText ?? ""
        menuItems.value = (r.result.menuItems ?? []).map((r: any) => {
            const isExternalUrl = r.menuItemURL.length > 4 && r.menuItemURL.startsWith("http")
            const isRelativeUrl = r.menuItemURL.length > 0 && !isExternalUrl && !r.menuItemURL.startsWith("/")

            let routeToUrl = null
            if (!isExternalUrl && r.menuItemURL.length > 0) {
                if (isRelativeUrl && props.navarea && props.nav) {
                    routeToUrl = `/${props.nav.toUpperCase()}/${r.menuItemURL}`
                } else {
                    routeToUrl = r.menuItemURL
                }
            }

            return {
                menuItemUrl: isExternalUrl ? r.menuItemURL : undefined,
                routeTo: routeToUrl,
                menuItemText: r.menuItemText,
                clickable: r.menuItemURL.length > 0,
                displayClass: r.menuItemURL.length
                    ? "leftNavLink"
                    : (r.isHeader ? "leftNavHeader" : "") + (r.menuItemText === "" ? " leftNavSpacer" : ""),
            }
        })
    } catch (_e) {
        navHeader.value = ""
        menuItems.value = []
    }
}

watch(myMainLeftDrawer, () => {
    emit("drawer-change", myMainLeftDrawer.value)
})

watch(
    () => props.mainLeftDrawer,
    () => {
        myMainLeftDrawer.value = props.mainLeftDrawer ?? false
    },
)

onMounted(async () => {
    await getLeftNav()
})
</script>
