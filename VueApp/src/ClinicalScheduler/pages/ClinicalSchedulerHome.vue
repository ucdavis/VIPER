<template>
    <div class="clinical-scheduler-home">
        <h1>Clinical Scheduler</h1>

        <div class="view-selector">
            <h2>Select Scheduling View:</h2>

            <div class="view-options">
                <div
                    class="view-card"
                    tabindex="0"
                    role="button"
                    @click="navigateToRotationView"
                    @keydown.enter="navigateToRotationView"
                    @keydown.space="navigateToRotationView"
                >
                    <h3>Schedule by Rotation</h3>
                    <p>Schedule clinicians for a specific rotation</p>
                </div>

                <div
                    class="view-card"
                    tabindex="0"
                    role="button"
                    @click="navigateToClinicianView"
                    @keydown.enter="navigateToClinicianView"
                    @keydown.space="navigateToClinicianView"
                >
                    <h3>Schedule by Clinician</h3>
                    <p>Schedule rotations for a specific clinician</p>
                </div>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
import { defineComponent, onMounted } from "vue"
import { useRouter } from "vue-router"

export default defineComponent({
    name: "ClinicalSchedulerHome",
    setup() {
        const router = useRouter()

        const navigateToRotationView = () => {
            void router.push("/ClinicalScheduler/rotation")
        }

        const navigateToClinicianView = () => {
            void router.push("/ClinicalScheduler/clinician")
        }

        onMounted(() => {
            // Set page title
            document.title = "VIPER - Clinical Scheduler"
        })

        return {
            navigateToRotationView,
            navigateToClinicianView,
        }
    },
})
</script>

<style scoped>
.clinical-scheduler-home {
    padding: 0 20px 20px;
    max-width: 800px;
    margin: 0 auto;
}

h1 {
    color: var(--ucdavis-aggie-blue);
    font-size: 24px;
    margin: 0 0 15px;
}

h2 {
    color: var(--ucdavis-black-80);
    margin-bottom: 20px;
}

.view-options {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 20px;
}

@media screen and (prefers-reduced-motion: reduce) {
    .view-card {
        border: 1px solid #ddd;
        padding: 20px;
        border-radius: 8px;
        cursor: pointer;
        transition: none;
        background-color: #f9f9f9;
    }
}

.view-card {
    border: 1px solid #ddd;
    padding: 20px;
    border-radius: 8px;
    cursor: pointer;
    transition: all 0.3s ease;
    background-color: #f9f9f9;
}

.view-card:hover,
.view-card:focus {
    background-color: #e9e9e9;
    box-shadow: 0 4px 8px rgb(0 0 0 / 10%);
    transform: translateY(-2px);
    outline: 2px solid var(--ucdavis-aggie-blue);
    outline-offset: 2px;
}

.view-card h3 {
    color: var(--ucdavis-aggie-blue);
    margin-bottom: 10px;
}

.view-card p {
    margin-bottom: 10px;
    color: var(--ucdavis-black-50);
}

.view-card .example {
    font-style: italic;
    font-size: 14px;
    color: #777;
}
</style>
