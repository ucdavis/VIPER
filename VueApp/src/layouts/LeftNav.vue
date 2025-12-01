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
        v-cloak
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

<script lang="ts">
import { ref, defineComponent } from "vue"
import { useFetch } from "@/composables/ViperFetch"
export default defineComponent({
    name: "LeftNav",
    props: {
        nav: {
            type: String,
            default: "",
        },
        navarea: Boolean,
        mainLeftDrawer: Boolean,
    },
    emits: ["drawer-change"],
    data() {
        return {
            myMainLeftDrawer: ref(false),
            navHeader: ref(""),
            menuItems: [] as any[],
            rawItems: [] as any[],
        }
    },
    methods: {
        async getLeftNav() {
            var u = new URL(
                import.meta.env.VITE_API_URL + "layout/leftnav/?area=" + this.navarea + "&nav=" + this.nav,
                document.baseURI,
            )
            const { get } = useFetch()
            const r = await get(u.toString())
            this.navHeader = r.result.menuHeaderText
            this.rawItems = r.result.menuItems
            this.menuItems = r.result.menuItems.map((r: any) => {
                const isExternalUrl = r.menuItemURL.length > 4 && r.menuItemURL.startsWith("http")
                const isRelativeUrl = r.menuItemURL.length > 0 && !isExternalUrl && !r.menuItemURL.startsWith("/")

                let routeToUrl = null
                if (!isExternalUrl && r.menuItemURL.length > 0) {
                    if (isRelativeUrl && this.navarea && this.nav) {
                        // For area-based navigation, prefix relative URLs with the area path
                        routeToUrl = `/${this.nav.toUpperCase()}/${r.menuItemURL}`
                    } else {
                        routeToUrl = r.menuItemURL
                    }
                }

                return {
                    menuItemUrl: isExternalUrl ? r.menuItemURL : null,
                    routeTo: routeToUrl,
                    menuItemText: r.menuItemText,
                    clickable: r.menuItemURL.length > 0,
                    displayClass: r.menuItemURL.length
                        ? "leftNavLink"
                        : (r.isHeader ? "leftNavHeader" : "") + (r.menuItemText == "" ? " leftNavSpacer" : ""),
                }
            })
        },
        emitLeftDrawerChange() {
            this.$emit("drawer-change", this.myMainLeftDrawer)
        },
    },
    mounted: async function () {
        await this.getLeftNav()
    },
    watch: {
        myMainLeftDrawer: function () {
            this.emitLeftDrawerChange()
        },
        mainLeftDrawer: function () {
            this.myMainLeftDrawer = this.mainLeftDrawer
        },
    },
})
</script>
