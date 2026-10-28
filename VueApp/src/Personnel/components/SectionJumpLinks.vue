<template>
    <!-- Nothing to navigate between with one destination, and nothing to offer when a search has
         emptied them all. -->
    <q-btn
        v-if="targets.length > 1"
        dense
        flat
        no-caps
        icon="list"
        label="Jump to section"
        class="text-body2"
    >
        <!--
            A floating menu rather than an inline panel. This component renders inside the sticky
            filter bar, whose measured height is published as --phone-list-filter-height and is what
            the page's scroll padding is built from. An inline panel changes that height on open,
            and the republished value always lags the click, so the browser scrolls using a stale
            padding and lands short of the heading. A portalled menu never enters the bar's
            flow, so the height it publishes stays true.

            transition-duration 0 because motion is near-absent here by design, and because an
            animated close would still be running when the browser follows the link.

            no-refocus because closing would otherwise pull focus back to this button and undo the
            fragment navigation's move to the heading. Focus on open is left alone: a keyboard user
            opening the menu should land in it.
        -->
        <q-menu
            :transition-duration="0"
            no-refocus
            auto-close
        >
            <nav aria-label="Phone list sections">
                <ul class="section-jump-links q-py-xs q-px-none q-ma-none">
                    <li
                        v-for="target in targets"
                        :key="target.id"
                    >
                        <!-- A plain anchor, not a router-link: the browser's own fragment navigation
                             honors the scroll padding that clears the header and filter bar, and moves
                             focus to the heading. The router's scrollBehavior would position with
                             window.scrollTo, which ignores scroll padding entirely. -->
                        <a
                            :href="`#${target.id}`"
                            class="text-primary"
                            >{{ target.label }}</a
                        >
                    </li>
                </ul>
            </nav>
        </q-menu>
    </q-btn>
</template>

<script setup lang="ts">
export type JumpTarget = { id: string; label: string }

defineProps<{ targets: JumpTarget[] }>()
</script>

<style scoped>
/* One link per line, so every link starts at the same edge. The menu caps its own height and
   scrolls, so a long list of sections costs no room on the page. */
.section-jump-links {
    list-style: none;
}

/* Block rather than inline, so the whole row is the tap target, not just the words. */
.section-jump-links a {
    display: block;
    padding: 0.5rem 1rem;
}
</style>
