<template>
    <ViperLayout
        nav="viper-effort"
        :navarea="true"
        highlighted-top-nav="Faculty"
    >
        <template #left-nav="{ drawerOpen, onDrawerChange }">
            <EffortLeftNav
                :drawer-open="drawerOpen"
                :on-drawer-change="onDrawerChange"
            />
        </template>
    </ViperLayout>
</template>

<script setup lang="ts">
import ViperLayout from "@/layouts/ViperLayout.vue"
import EffortLeftNav from "../components/EffortLeftNav.vue"
</script>
