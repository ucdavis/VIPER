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
                    aria-label="Close menu"
                    class="float-right lt-md"
                    @click="myMainLeftDrawer = false"
                />
                <h2 v-if="navHeader.length">
                    {{ navHeader }}
                </h2>
                <nav :aria-label="navHeader || 'Section navigation'">
                    <q-list
                        dense
                        separator
                        role="list"
                    >
                        <template
                            v-for="(menuItem, index) in menuItems"
                            :key="index"
                        >
                            <q-item
                                v-if="menuItem.routeTo != null"
                                :clickable="menuItem.clickable"
                                v-ripple
                                :to="menuItem.routeTo"
                                :class="menuItem.displayClass"
                            >
                                <q-item-section>
                                    <q-item-label
                                        v-overflow-title
                                        lines="1"
                                    >
                                        {{ menuItem.menuItemText }}
                                    </q-item-label>
                                </q-item-section>
                            </q-item>
                            <q-item
                                v-else-if="menuItem.menuItemUrl"
                                clickable
                                v-ripple
                                :href="menuItem.menuItemUrl"
                                target="_blank"
                                rel="noopener noreferrer"
                                :class="menuItem.displayClass"
                            >
                                <q-item-section>
                                    <q-item-label
                                        v-overflow-title
                                        lines="1"
                                    >
                                        {{ menuItem.menuItemText }}
                                        <template v-if="menuItem.isExternalSite">
                                            <q-icon
                                                name="open_in_new"
                                                size="xs"
                                                class="q-ml-xs"
                                                aria-hidden="true"
                                            >
                                                <q-tooltip>Opens in new window</q-tooltip>
                                            </q-icon>
                                            <span class="sr-only">(opens in new window)</span>
                                        </template>
                                    </q-item-label>
                                </q-item-section>
                            </q-item>
                            <q-item
                                v-else
                                :class="menuItem.displayClass"
                            >
                                <q-item-section>
                                    <q-item-label
                                        v-overflow-title
                                        lines="1"
                                    >
                                        {{ menuItem.menuItemText }}
                                    </q-item-label>
                                </q-item-section>
                            </q-item>
                        </template>
                    </q-list>
                </nav>
            </div>
        </template>
    </q-drawer>
</template>

<script setup lang="ts">
import { ref, watch, onMounted } from "vue"
import { useFetch } from "@/composables/ViperFetch"

type OverflowTitleElement = HTMLElement & {
    _overflowTitleObserver?: ResizeObserver
}

function updateOverflowTitle(el: HTMLElement) {
    const text = el.firstChild?.textContent?.trim() ?? ""
    if (!text) {
        el.removeAttribute("title")
        return
    }
    if (el.scrollWidth > el.clientWidth) {
        const hasExternal = el.querySelector(".sr-only") !== null
        el.title = hasExternal ? `${text} (opens in new window)` : text
    } else {
        el.removeAttribute("title")
    }
}

// Sets title attribute only when text is truncated by ellipsis.
// Uses ResizeObserver to update when drawer toggles or window resizes.
const vOverflowTitle = {
    mounted(el: OverflowTitleElement) {
        updateOverflowTitle(el)
        if (typeof ResizeObserver !== "undefined") {
            el._overflowTitleObserver = new ResizeObserver(() => {
                updateOverflowTitle(el)
            })
            el._overflowTitleObserver.observe(el)
        }
    },
    updated(el: OverflowTitleElement) {
        updateOverflowTitle(el)
    },
    unmounted(el: OverflowTitleElement) {
        el._overflowTitleObserver?.disconnect()
        delete el._overflowTitleObserver
    },
}

interface MenuItem {
    menuItemUrl: string | undefined
    routeTo: string | null
    menuItemText: string
    clickable: boolean
    displayClass: string
    isExternalSite: boolean
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

            // VIPER1 links share the same hostname — only show external icon for truly external sites
            let isExternalSite = false
            if (isExternalUrl) {
                try {
                    isExternalSite = new URL(r.menuItemURL).hostname !== window.location.hostname
                } catch {
                    isExternalSite = true
                }
            }

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
                isExternalSite,
                displayClass:
                    (r.menuItemURL.length
                        ? "leftNavLink"
                        : (r.isHeader ? "leftNavHeader" : "") + (r.menuItemText === "" ? " leftNavSpacer" : "")) +
                    (r.indentLevel > 0 ? ` leftNavIndent${r.indentLevel}` : ""),
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

onMounted(() => getLeftNav())
</script>
