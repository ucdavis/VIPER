<script setup lang="ts">
import AccessNoticeBanner from "@/Students/components/AccessNoticeBanner.vue"
import { useAccessNotice } from "@/Students/composables/use-access-notice"

// Says why, when a Students app has turned the user away and sent them here.
const accessNotice = useAccessNotice()
</script>

<template>
    <h1>Students Home</h1>
    <AccessNoticeBanner :notice="accessNotice" />
</template>
