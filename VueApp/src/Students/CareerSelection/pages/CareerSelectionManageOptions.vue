<script setup lang="ts">
import AppAccessControls from "@/Students/components/AppAccessControls.vue"
import CareerOptionManager from "../components/CareerOptionManager.vue"
import { careerSelectionService } from "../services/career-selection-service"
import type { CareerOptionType } from "../types"

const optionTypes: CareerOptionType[] = ["career", "species", "postGrad"]

async function getAccessStatus(): Promise<{ appOpen: boolean } | null> {
    const appOpen = await careerSelectionService.getAccessStatus()
    return appOpen === null ? null : { appOpen }
}
</script>

<template>
    <div class="q-pa-md">
        <q-breadcrumbs class="q-mb-sm">
            <q-breadcrumbs-el
                label="Career Selection"
                :to="{ name: 'CareerSelectionList' }"
            />
            <q-breadcrumbs-el label="Manage Options" />
        </q-breadcrumbs>

        <h1 class="q-ma-none q-mb-md">Manage Career Selection Options</h1>

        <section
            aria-labelledby="career-options-access"
            class="q-mb-lg"
        >
            <h2
                id="career-options-access"
                class="q-ma-none q-mb-sm"
            >
                Student Editing
            </h2>
            <AppAccessControls
                :get-status="getAccessStatus"
                :toggle="careerSelectionService.toggleAppAccess"
            />
        </section>

        <p class="text-caption q-mb-sm">
            Options a student has selected cannot be deleted. The "Other" option cannot be renamed or deleted.
        </p>

        <CareerOptionManager
            v-for="type in optionTypes"
            :key="type"
            :type="type"
        />
    </div>
</template>
