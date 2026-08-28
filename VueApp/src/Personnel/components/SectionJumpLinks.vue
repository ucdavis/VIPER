<template>
    <!-- Nothing to navigate between with one destination, and nothing to offer when a search has
         emptied them all. -->
    <q-expansion-item
        v-if="targets.length > 1"
        dense
        dense-toggle
        expand-separator
        icon="list"
        label="Jump to section"
        header-class="text-body2"
    >
        <nav aria-label="Phone list sections">
            <ul class="section-jump-links q-pa-sm q-ma-none">
                <li
                    v-for="target in targets"
                    :key="target.id"
                >
                    <!-- A plain anchor, not a router-link: the browser's own fragment navigation
                         honours the scroll-margin that clears the header and filter bar, and moves
                         focus to the heading. The router's scrollBehavior would position with
                         window.scrollTo, which ignores scroll-margin entirely. -->
                    <a
                        :href="`#${target.id}`"
                        class="text-primary"
                        >{{ target.label }}</a
                    >
                </li>
            </ul>
        </nav>
    </q-expansion-item>
</template>

<script setup lang="ts">
export type JumpTarget = { id: string; label: string }

defineProps<{ targets: JumpTarget[] }>()
</script>

<style scoped>
/* A wrapping row of links rather than a stacked list: a dozen sections stacked would be taller
   than the content they navigate to. */
.section-jump-links {
    display: flex;
    flex-wrap: wrap;
    gap: 0.25rem 1rem;
    list-style: none;
}
</style>
