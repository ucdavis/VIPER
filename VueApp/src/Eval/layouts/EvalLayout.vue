<template>
    <ViperLayout
        nav="viper-eval"
        :navarea="true"
        highlighted-top-nav="Faculty"
    >
        <template #left-nav="{ drawerOpen, onDrawerChange }">
            <EvalLeftNav
                :drawer-open="drawerOpen"
                :on-drawer-change="onDrawerChange"
            />
        </template>
    </ViperLayout>
</template>

<script setup lang="ts">
import ViperLayout from "@/layouts/ViperLayout.vue"
import EvalLeftNav from "../components/EvalLeftNav.vue"
</script>
