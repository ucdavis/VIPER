<template>
    <q-drawer v-model="mainLeftDrawer" show-if-above elevated side="left"
              :mini="!mainLeftDrawer" no-mini-animation
              :width="300" id="mainLeftDrawer" v-cloak>
        <template v-slot:default>
            <div class="q-pa-sm" id="leftNavMenu">
                <q-btn dense
                       round
                       unelevated
                       color="secondary"
                       icon="close"
                       class="float-right lt-md"
                       @click="mainLeftDrawer = false"></q-btn>
                <h2 v-if="navHeader.length">{{ navHeader }}</h2>
                <q-list dense separator>
                    <q-item v-for="menuItem in menuItems"
                            :clickable="menuItem.clickable"
                            :v-ripple="menuItem.clickable"
                            :to="menuItem.routeTo"
                            :class="menuItem.displayClass">
                        <q-item-section>
                            <q-item-label lines="1">{{ menuItem.menuItemText }}</q-item-label>
                        </q-item-section>
                    </q-item>
                    <q-item to="ManageDomains">
                        <q-item-section>
                            MDT
                        </q-item-section>
                    </q-item>
                </q-list>
            </div>
        </template>
    </q-drawer>
    <router-link to="Home">Home</router-link>
</template>

<script lang="ts">
    import { ref, defineComponent } from 'vue'
    import { useFetch } from '@/composables/ViperFetch'
    export default defineComponent({
        name: "LeftNav",
        setup(props) {


        },
        props: {
            nav: String,
            navarea: Boolean
        },
        data() {
            return {
                mainLeftDrawer: ref(false),
                navHeader: ref(""),
                menuItems: [] as any[]
            }
        },
        methods: {
            async getTopNav() {
                var u = new URL(import.meta.env.VITE_API_URL + "layout/leftnav/?area=" + this.navarea + "&nav=" + this.nav, document.baseURI)
                const { result, errors, vfetch } = useFetch(u.toString())
                await vfetch()
                this.navHeader = result.value.menuHeaderText
                this.menuItems = result.value.menuItems.map((r: any) => ({
                    menuItemUrl: r.menuItemURL.substr(0, 5) == "http:" ? r.menuItemUrl : null,
                    routeTo: r.menuItemURL.substr(0, 5) != "http:" ? r.menuItemUrl : null,
                    menuItemText: r.menuItemText,
                    clickable: r.menuItemURL.length > 0,
                    displayClass: (r.menuItemURL.length
                        ? "leftNavLink"
                        : (r.isHeader ? "leftNavHeader" : "") + (r.menuItemText == "" ? " leftNavSpacer" : ""))
                }))

            }
        },
        mounted: async function () {
            await this.getTopNav()
        }
    })
</script>
