<script setup lang="ts">
import { computed } from "vue"
import { checkHasOnePermission } from "@/composables/CheckPagePermission"
import { CAREER_SELECTION_PERMISSIONS } from "../constants/permissions"

/**
 * The heading row the career selection roster and report share: the page title, and the
 * admin-only link to the option lists. The manage page has no such link and keeps its own h1.
 */
defineProps<{
    title: string
}>()

const isAdmin = computed(() => checkHasOnePermission([CAREER_SELECTION_PERMISSIONS.ADMIN]))
</script>

<template>
    <div class="row items-center q-mb-md">
        <h1 class="q-ma-none">{{ title }}</h1>
        <q-space />
        <q-btn
            v-if="isAdmin"
            :to="{ name: 'CareerSelectionManageOptions' }"
            label="Manage Options"
            icon="settings"
            color="primary"
            dense
            no-caps
            padding="xs sm"
        />
    </div>
</template>
