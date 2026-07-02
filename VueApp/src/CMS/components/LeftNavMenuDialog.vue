<template>
    <q-dialog
        :model-value="modelValue"
        persistent
        aria-labelledby="add-menu-title"
        @update:model-value="emit('update:modelValue', $event)"
        @keydown.escape="handleClose"
    >
        <q-card class="dialog-card-sm">
            <q-card-section class="row items-center q-pb-none">
                <div
                    id="add-menu-title"
                    class="text-h6"
                >
                    Add Left-Nav Menu
                </div>
                <q-space />
                <q-btn
                    icon="close"
                    flat
                    round
                    dense
                    aria-label="Close dialog"
                    @click="handleClose"
                />
            </q-card-section>

            <q-form
                ref="formRef"
                greedy
                @submit.prevent="save"
                @validation-error="onValidationError"
            >
                <q-card-section class="q-pt-sm">
                    <LeftNavMenuSettingsFields v-model="menu" />

                    <StatusBanner
                        v-if="formError"
                        type="error"
                    >
                        {{ formError }}
                    </StatusBanner>
                </q-card-section>

                <q-card-actions align="right">
                    <q-btn
                        flat
                        label="Cancel"
                        dense
                        no-caps
                        @click="handleClose"
                    />
                    <q-btn
                        type="submit"
                        label="Create Menu"
                        color="primary"
                        dense
                        no-caps
                        :loading="saving"
                    >
                        <template #loading>
                            <q-spinner
                                size="1em"
                                class="q-mr-sm"
                            />
                            Create Menu
                        </template>
                    </q-btn>
                </q-card-actions>
            </q-form>
        </q-card>
    </q-dialog>
</template>

<script setup lang="ts">
import { inject, ref, watch } from "vue"
import { useFetch } from "@/composables/ViperFetch"
import { useUnsavedChanges } from "@/composables/use-unsaved-changes"
import LeftNavMenuSettingsFields, { type MenuSettings } from "@/CMS/components/LeftNavMenuSettingsFields.vue"
import StatusBanner from "@/components/StatusBanner.vue"

const props = defineProps<{ modelValue: boolean }>()
const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    created: [leftNavMenuId: number]
}>()

const apiURL = inject("apiURL") + "cms/left-navs"
const { post } = useFetch()

const formRef = ref()
const saving = ref(false)
const formError = ref("")

const emptyMenu = (): MenuSettings => ({
    menuHeaderText: "",
    system: "Viper",
    viperSectionPath: null,
    page: null,
    friendlyName: null,
})

const menu = ref<MenuSettings>(emptyMenu())

const { setInitialState, confirmClose } = useUnsavedChanges(menu)

// Start each open from a clean, empty form and capture that baseline for the unsaved guard.
watch(
    () => props.modelValue,
    (open) => {
        if (open) {
            menu.value = emptyMenu()
            formError.value = ""
            formRef.value?.resetValidation()
            setInitialState()
        }
    },
)

async function handleClose() {
    if (await confirmClose()) {
        emit("update:modelValue", false)
    }
}

function onValidationError() {
    formError.value = "Please complete the required fields before creating this menu."
}

async function save() {
    formError.value = ""
    saving.value = true
    const res = await post(apiURL, {
        menuHeaderText: menu.value.menuHeaderText,
        system: menu.value.system,
        viperSectionPath: menu.value.viperSectionPath || null,
        page: menu.value.page || null,
        friendlyName: menu.value.friendlyName || null,
    })
    saving.value = false

    if (!res.success) {
        formError.value = res.errors?.[0] ?? "Failed to create menu"
        return
    }
    emit("created", res.result.leftNavMenuId)
    emit("update:modelValue", false)
}
</script>
