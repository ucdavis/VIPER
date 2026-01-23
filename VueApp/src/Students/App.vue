<template>
    <main>
        <component
            :is="$route.meta.layout || 'div'"
            nav="viper-students"
            :navarea="false"
            highlighted-top-nav="Students"
            :breadcrumbs="$route.meta?.breadcrumbs"
        >
        </component>
    </main>
    <GenericError />
</template>

<script setup lang="ts">
import GenericError from "@/components/GenericError.vue"
</script>
