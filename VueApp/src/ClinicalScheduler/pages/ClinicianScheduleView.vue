<template>
    <div class="clinical-scheduler-container">
        <div class="navigation-bar">
            <router-link to="/ClinicalScheduler/" class="nav-link">Home</router-link>
            <span class="nav-divider">|</span>
            <router-link to="/ClinicalScheduler/rotation" class="nav-link">Schedule by Rotation</router-link>
            <span class="nav-divider">|</span>
            <span class="nav-link active">Schedule by Clinician</span>
        </div>

        <h2>
            Schedule for
            <select class="title-dropdown" v-model="selectedClinician">
                <option>Ad, Yael</option>
                <option>Choi, April</option>
                <option>Chromik, Melissa</option>
                <option>Dear, Jonathan</option>
                <option>Hulsebosch, Sean</option>
            </select>
        </h2>

        <!-- Instructions -->
        <div class="instructions">
            <p>This list of rotations should be contain any rotations this clinician is scheduled for in the current or previous year.</p>
            <p>Click on a rotation to select it and then click on any week to schedule the clinician.</p>
        </div>

        <!-- Rotation selector section -->
        <div class="rotation-selector-section">
            <div class="rotation-buttons">
                <button class="rotation-btn" :class="{ selected: selectedRotation === 'Internal Medicine A' }" @click="selectRotation('Internal Medicine A')">Internal Medicine A</button>
                <button class="rotation-btn" :class="{ selected: selectedRotation === 'Internal Medicine B' }" @click="selectRotation('Internal Medicine B')">Internal Medicine B</button>
                <button class="rotation-btn" :class="{ selected: selectedRotation === 'Small Animal Anesthesia' }" @click="selectRotation('Small Animal Anesthesia')">Small Animal Anesthesia</button>
                <button class="rotation-btn" :class="{ selected: selectedRotation === 'Surgery' }" @click="selectRotation('Surgery')">Surgery</button>
                <button class="rotation-btn" :class="{ selected: selectedRotation === 'Emergency' }" @click="selectRotation('Emergency')">Emergency</button>
            </div>
            <select class="rotation-dropdown">
                <option>Add Rotation</option>
                <option>Pathology</option>
                <option>Radiology</option>
                <option>Cardiology</option>
                <option>Dermatology</option>
            </select>
        </div>

        <!-- Note about stars and X -->
        <div class="action-notes">
            <p>X removes the rotation from the schedule. Star marks them as primary (and unmarks the current primary, if any).</p>
        </div>

        <!-- Season headers -->
        <h3>Spring 2025</h3>

        <!-- Week grid -->
        <div class="week-grid">
            <div class="week-cell">
                <div class="week-header">Week 1<br>4/14/25</div>
                <div class="rotation-list">
                    <div class="rotation-item">
                        <span class="remove-btn">✖</span>
                        <span>SAMedA</span>
                        <span class="primary-star">☆</span>
                    </div>
                </div>
            </div>

            <div class="week-cell">
                <div class="week-header">Week 2<br>4/14/25</div>
                <div class="rotation-list">
                    <div class="rotation-item">
                        <span class="remove-btn">✖</span>
                        <span>SAMedA</span>
                        <span class="primary-star">☆</span>
                    </div>
                </div>
            </div>

            <div class="week-cell">
                <div class="week-header">Week 3<br>4/14/25</div>
                <div class="rotation-list">
                    <div class="rotation-item">
                        <span class="remove-btn">✖</span>
                        <span>SAMedB</span>
                        <span class="primary-star">★</span>
                    </div>
                </div>
            </div>

            <div class="week-cell">
                <div class="week-header">Week 4<br>4/14/25</div>
                <div class="rotation-list">
                    <div class="rotation-item">
                        <span class="remove-btn">✖</span>
                        <span>SAMedB</span>
                        <span class="primary-star">★</span>
                    </div>
                </div>
            </div>

            <div class="week-cell">
                <div class="week-header">Week 5<br>4/14/25</div>
                <div class="rotation-list">
                    <div class="rotation-item">
                        <span class="remove-btn">✖</span>
                        <span>SAMedA</span>
                        <span class="primary-star">☆</span>
                    </div>
                </div>
            </div>

            <div class="week-cell">
                <div class="week-header">Week 6<br>4/14/25</div>
                <div class="rotation-list">
                    <div class="rotation-item">
                        <span class="remove-btn">✖</span>
                        <span>SAMedA</span>
                        <span class="primary-star">☆</span>
                    </div>
                </div>
            </div>
        </div>

        <!-- Summer Season -->
        <h3>Summer 2025</h3>
        <div class="week-grid single-week">
            <div class="week-cell">
                <div class="week-header">Week 7<br>4/14/25</div>
                <div class="rotation-list">
                    <!-- Empty week -->
                </div>
            </div>
        </div>

        <div class="schedule-note">
            Schedule continues similar to https://
            secure-test.vetmed.ucdavis.edu/clinicialscheduler/scheduler/
            default.cfm?mothraID=01654831&amp;page=clinicianRO
        </div>
    </div>
</template>

<script lang="ts">
import { defineComponent, ref } from 'vue'

export default defineComponent({
    name: 'ClinicianScheduleView',
    setup() {
        const selectedClinician = ref('Ad, Yael')
        const selectedRotation = ref('Internal Medicine A')
        const selectedNewRotation = ref('')

        const selectRotation = (rotation: string) => {
            selectedRotation.value = rotation
        }

        return {
            selectedClinician,
            selectedRotation,
            selectedNewRotation,
            selectRotation
        }
    }
})
</script>

<style scoped>
.clinical-scheduler-container {
    padding: 20px;
    font-family: Arial, sans-serif;
}

.navigation-bar {
    margin-bottom: 20px;
    padding: 10px;
    background-color: #f0f0f0;
    border-radius: 5px;
}

.nav-link {
    color: #0066cc;
    text-decoration: none;
    padding: 5px 10px;
}

.nav-link:hover {
    text-decoration: underline;
}

.nav-link.active {
    font-weight: bold;
    color: #8B0000;
}

.nav-divider {
    margin: 0 10px;
    color: #999;
}

h2 {
    color: #8B0000;
    font-size: 20px;
    margin-bottom: 15px;
    display: flex;
    align-items: center;
    gap: 10px;
}

.title-dropdown {
    font-size: 18px;
    font-weight: bold;
    color: #8B0000;
    border: 1px solid #ccc;
    padding: 2px 5px;
    background-color: #fff;
}

.instructions {
    background-color: #f5f5f5;
    padding: 10px;
    margin-bottom: 15px;
    font-size: 13px;
}

.instructions p {
    margin: 5px 0;
}


.rotation-selector-section {
    margin-bottom: 20px;
    padding: 15px;
    background-color: #f9f9f9;
    border: 1px solid #ddd;
}

.rotation-buttons {
    display: inline-block;
    margin-right: 20px;
}

.rotation-btn {
    padding: 5px 10px;
    margin: 2px;
    background-color: #fff;
    border: 1px solid #ccc;
    cursor: pointer;
    font-size: 13px;
}

.rotation-btn:hover {
    background-color: #e0e0e0;
}

.rotation-btn.selected {
    background-color: #4CAF50;
    color: white;
    border-color: #4CAF50;
}

.rotation-dropdown {
    padding: 5px;
    min-width: 150px;
    vertical-align: middle;
}

.action-notes {
    font-size: 12px;
    color: #666;
    margin-bottom: 15px;
}

.action-notes p {
    margin: 3px 0;
}

h3 {
    color: #333;
    font-size: 16px;
    margin-top: 20px;
    margin-bottom: 10px;
}

.week-grid {
    display: grid;
    grid-template-columns: repeat(6, 1fr);
    gap: 10px;
    margin-bottom: 20px;
}

.week-cell {
    border: 1px solid #ccc;
    min-height: 120px;
    background-color: #fff;
    cursor: pointer;
    position: relative;
}

.week-cell:hover {
    background-color: #f9f9f9;
}

.week-header {
    background-color: #e0e0e0;
    padding: 5px;
    font-weight: bold;
    font-size: 12px;
    text-align: center;
    border-bottom: 1px solid #ccc;
}

.rotation-list {
    padding: 5px;
}

.rotation-item {
    display: flex;
    align-items: center;
    padding: 3px 5px;
    margin: 2px 0;
    font-size: 13px;
    border: 1px solid transparent;
}

.remove-btn {
    color: #ff0000;
    font-weight: bold;
    margin-right: 5px;
    cursor: pointer;
    font-size: 12px;
}

.primary-star {
    margin-left: auto;
    color: #000;
    font-size: 16px;
}

.week-grid.single-week {
    grid-template-columns: 1fr 5fr;
    max-width: 300px;
}

.schedule-note {
    margin-top: 20px;
    font-size: 11px;
    color: #999;
    font-style: italic;
}
</style>