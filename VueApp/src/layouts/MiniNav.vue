<template>
    <q-menu persistent>
        <q-list style="min-width: 200px" dense>
            <template v-for="nav in topNav">
                <q-item clickable>
                    <q-item-section>
                        <q-btn stretch flat :label="nav.menuItemText"
                               :href="nav.menuItemURL"></q-btn>
                    </q-item-section>
                </q-item>
            </template>
            <q-item clickable>
                <q-item-section>
                    <q-btn stretch flat label="Help" :href="helpNav.menuItemURL"></q-btn>
                </q-item-section>
            </q-item>
        </q-list>
    </q-menu>
</template>

<script>
    import { ref, defineComponent } from 'vue'
    export default defineComponent({
        name: 'MiniNav',
        props: {
            highlightedTopNav: String
        },
        data() {
            return {
                topNav: ref([]),
                helpNav: ref(""),
                navClass: ref("q-btn q-btn--flat q-btn--actionable q-hoverable q-px-md q-py-sm text-white navLink "),
            }
        },
        methods: {
            async getTopNav() {
                var d = await fetch(import.meta.env.VITE_API_URL + "layout/topnav")
                    .then(r => (r.status == 204 || r.status == 202) ? r : r.json())
                this.helpNav = d.result.pop()
                this.topNav = d.result
            }
        },
        mounted: async function () {
            await this.getTopNav()
        }
    })
</script>