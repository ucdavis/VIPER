<template>
    <q-drawer v-model="myMainLeftDrawer" show-if-above elevated side="left"
              :mini="!myMainLeftDrawer" no-mini-animation
              :width="300" id="mainLeftDrawer" v-cloak>
        <template v-slot:default>
            <div class="q-pa-sm" id="leftNavMenu">
                <q-btn dense
                       round
                       unelevated
                       color="secondary"
                       icon="close"
                       class="float-right lt-md"
                       @click="myMainLeftDrawer = false"></q-btn>
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
                </q-list>
            </div>
        </template>
    </q-drawer>
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
            navarea: Boolean,
            mainLeftDrawer: Boolean
        },
        data() {
            return {
                myMainLeftDrawer: ref(false),
                navHeader: ref(""),
                menuItems: [] as any[],
                rawItems: [] as any[],
            }
        },
        methods: {
            async getTopNav() {
                var u = new URL(import.meta.env.VITE_API_URL + "layout/leftnav/?area=" + this.navarea + "&nav=" + this.nav, document.baseURI)
                const { result, errors, vfetch } = useFetch(u.toString())
                await vfetch()
                this.navHeader = result.value.menuHeaderText
                this.rawItems = result.value.menuItems
                this.menuItems = result.value.menuItems.map((r: any) => ({
                    menuItemUrl: (r.menuItemURL.length > 4 && r.menuItemURL.substr(0, 4) == "http") ? r.menuItemURL : null,
                    routeTo: (r.menuItemURL.length <= 4 || r.menuItemURL.substr(0, 4) != "http") ? r.menuItemURL : "test",
                    menuItemText: r.menuItemText,
                    clickable: r.menuItemURL.length > 0,
                    displayClass: (r.menuItemURL.length
                        ? "leftNavLink"
                        : (r.isHeader ? "leftNavHeader" : "") + (r.menuItemText == "" ? " leftNavSpacer" : ""))
                }))
            },
            emitLeftDrawerChange() {
                this.$emit('drawer-change', this.myMainLeftDrawer)
            }
        },
        mounted: async function () {
            await this.getTopNav()
        },
        watch: {
            myMainLeftDrawer: function() {
                this.emitLeftDrawerChange()
            },
            mainLeftDrawer: function() {
                this.myMainLeftDrawer = this.mainLeftDrawer
            }
        }
    })
</script>
