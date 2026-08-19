<template>
    <q-btn
        no-caps
        color="primary"
        label="Log In"
        :href="loginUrl"
    />
</template>

<script setup lang="ts">
import { getLoginUrl } from "@/composables/RequireLogin"

// Every re-authentication prompt renders this, so the label, casing, colour and same-tab behaviour
// stay identical wherever the action appears. The href comes from the shared builder, which carries
// the app-base normalization, open-redirect validation and encoding a hand-rolled URL keeps missing.
const loginUrl = getLoginUrl()
</script>
