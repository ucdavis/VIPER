<template>
    <q-input
        v-model="menu.menuHeaderText"
        dense
        outlined
        label="Menu header"
        class="required-field q-mb-sm"
        :rules="[(v: string | null) => (v && v.trim().length > 0) || 'Menu header is required']"
        aria-required="true"
        hide-bottom-space
    />

    <q-select
        v-model="menu.system"
        dense
        options-dense
        outlined
        label="System"
        class="q-mb-sm"
        :options="['Viper', 'Public']"
    />

    <q-input
        v-model="menu.viperSectionPath"
        dense
        outlined
        label="VIPER section path"
        class="q-mb-sm"
    />

    <q-input
        v-model="menu.page"
        dense
        outlined
        label="Page"
        class="q-mb-sm"
    />

    <q-input
        v-model="menu.friendlyName"
        dense
        outlined
        label="Friendly name"
        hint="Used by pages to look up this menu"
        class="q-mb-sm"
    />
</template>

<script lang="ts">
export type MenuSettings = {
    menuHeaderText: string | null
    system: string
    viperSectionPath: string | null
    page: string | null
    friendlyName: string | null
}

// A blank menu-settings baseline. Shared with LeftNavMenuDialog (add modal) and LeftNavEdit
// (edit page reset + initial state) so a "new menu" starts identical in all three spots.
export function createEmptyMenuSettings(): MenuSettings {
    return {
        menuHeaderText: "",
        system: "Viper",
        viperSectionPath: null,
        page: null,
        friendlyName: null,
    }
}
</script>

<script setup lang="ts">
// Shared by the add modal (LeftNavMenuDialog) and the edit page (LeftNavEdit) so the menu
// settings form stays identical in both. The model is a shared reactive object; field edits
// mutate it in place, which both callers observe.
const menu = defineModel<MenuSettings>({ required: true })
</script>

<style scoped>
.required-field :deep(.q-field__label)::after {
    content: " *";
    color: var(--q-negative);
}
</style>
