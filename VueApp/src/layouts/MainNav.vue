<template>
    <div class="row gt-sm items-stretch justify-middle q-mt-xs" id="mainLayoutHeaderSections">
        <template v-for="nav in topNav">
            <a :class="navClass + (nav.menuItemText == highlightedTopNav ? 'selectedTopNav' : '')"
               :href="nav.menuItemURL">
                <span class="q-focus-helper"></span>
                <span class="q-btn__content" style="padding-top:2px;">
                    <span class="block">
                        {{ nav.menuItemText }}
                    </span>
                </span>
            </a>
            <hr class="q-separator q-separator--vertical q-separator--dark" aria-orientation="vertical" />
        </template>
        <q-btn flat href="helpNav.menuItemURL" icon="help" class="q-px-md text-primary" v-cloak>
            <q-tooltip>Help</q-tooltip>
        </q-btn>
        <!--<hr class="q-separator q-separator--vertical q-separator--dark" aria-orientation="vertical">-->
    </div>
</template>

<script>
    import { ref, defineComponent } from 'vue'
    export default defineComponent({
        name: 'MainNav',
        props: {
            highlightedTopNav: String  
        },
        data() {
            return {
                topNav: ref([]),
                helpNav: ref(""),
                navClass: ref("q-btn q-btn--flat q-btn--actionable q-btn--no-uppercase q-hoverable q-px-md text-primary text-weight-medium navLink "),
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