<template>
    <div
        class="row gt-sm items-stretch justify-middle q-mt-xs"
        id="mainLayoutHeaderSections"
    >
        <template
            v-for="nav in topNav"
            :key="nav.menuItemURL"
        >
            <a
                :class="navClass + (nav.menuItemText == highlightedTopNav ? 'selectedTopNav' : '')"
                :href="nav.menuItemURL"
            >
                <span class="q-focus-helper"></span>
                <span
                    class="q-btn__content"
                    style="padding-top: 2px"
                >
                    <span class="block">
                        {{ nav.menuItemText }}
                    </span>
                </span>
            </a>
            <hr
                class="q-separator q-separator--vertical q-separator--dark"
                aria-orientation="vertical"
            />
        </template>
        <q-btn
            flat
            href="helpNav.menuItemURL"
            icon="help"
            class="q-px-md text-primary"
        >
            <q-tooltip>Help</q-tooltip>
        </q-btn>
        <!--<hr class="q-separator q-separator--vertical q-separator--dark" aria-orientation="vertical">-->
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from "vue"

defineProps<{
    highlightedTopNav?: string
}>()

interface NavItem {
    menuItemURL: string
    menuItemText: string
}

const topNav = ref<NavItem[]>([])
const helpNav = ref("")
const navClass =
    "q-btn q-btn--flat q-btn--actionable q-btn--no-uppercase q-hoverable q-px-md text-primary text-weight-regular navLink "

async function getTopNav() {
    try {
        const response = await fetch(import.meta.env.VITE_API_URL + "layout/topnav")
        if (!response.ok) {
            topNav.value = []
            return
        }
        const d = await response.json()
        if (d.result && d.result.length > 0) {
            helpNav.value = d.result.pop()
            topNav.value = d.result
        }
    } catch (_e) {
        topNav.value = []
    }
}

onMounted(async () => {
    await getTopNav()
})
</script>
