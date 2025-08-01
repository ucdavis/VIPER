<template>
    <div class="clinical-scheduler-container">
        <div class="navigation-bar">
            <router-link to="/ClinicalScheduler/" class="nav-link">Home</router-link>
            <span class="nav-divider">|</span>
            <span class="nav-link active">Schedule by Rotation</span>
            <span class="nav-divider">|</span>
            <router-link to="/ClinicalScheduler/clinician" class="nav-link">Schedule by Clinician</router-link>
        </div>

        <h2>
            Clinician Schedule for
            <select class="title-dropdown" v-model="selectedRotation">
                <option>Internal Medicine A</option>
                <option>Internal Medicine B</option>
                <option>Small Animal Anesthesia</option>
                <option>Anatomic Pathology</option>
            </select>
        </h2>

        <!-- Instructions -->
        <div class="instructions">
            <p>This list of clinicians should be contain any clinician scheduled for the rotation in the current or previous year.</p>
            <p>Click on a clinician to select them, and then click on any week to schedule them.</p>
        </div>

        <!-- Clinician selector section -->
        <div class="clinician-selector-section">
            <div class="clinician-buttons">
                <button class="clinician-btn" :class="{ selected: selectedClinician === 'Ad, Yael' }" @click="selectClinician('Ad, Yael')">Ad, Yael</button>
                <button class="clinician-btn" :class="{ selected: selectedClinician === 'Dear, Jonathan' }" @click="selectClinician('Dear, Jonathan')">Dear, Jonathan</button>
                <button class="clinician-btn" :class="{ selected: selectedClinician === 'Hulsebosch, Sean' }" @click="selectClinician('Hulsebosch, Sean')">Hulsebosch, Sean</button>
                <button class="clinician-btn" :class="{ selected: selectedClinician === 'Marsilio, Sina' }" @click="selectClinician('Marsilio, Sina')">Marsilio, Sina</button>
                <button class="clinician-btn" :class="{ selected: selectedClinician === 'Palm, Carrie' }" @click="selectClinician('Palm, Carrie')">Palm, Carrie</button>
            </div>
            <select class="clinician-dropdown">
                <option>Add Clinician</option>
                <option>Choi, April</option>
                <option>Chromik, Melissa</option>
                <option>Keil, Tessa</option>
                <option>Lui, Clinson</option>
            </select>
        </div>

        <!-- Note about stars and X -->
        <div class="action-notes">
            <p>X removes the clinician from the schedule. Star marks them as primary (and unmarks the current primary, if any).</p>
        </div>

        <!-- Season headers -->
        <h3>Spring 2025</h3>

        <!-- Week grid -->
        <div class="week-grid">
            <div class="week-cell">
                <div class="week-header">Week 1<br>4/14/25</div>
                <div class="clinician-list">
                    <div class="clinician-item">
                        <span class="remove-btn">✖</span>
                        <span>Ad, Yael</span>
                        <span class="primary-star">☆</span>
                    </div>
                </div>
            </div>

            <div class="week-cell">
                <div class="week-header">Week 2<br>4/14/25</div>
                <div class="clinician-list">
                    <div class="clinician-item">
                        <span class="remove-btn">✖</span>
                        <span>Ad, Yael</span>
                        <span class="primary-star">☆</span>
                    </div>
                </div>
            </div>

            <div class="week-cell">
                <div class="week-header">Week 3<br>4/14/25</div>
                <div class="clinician-list">
                    <div class="clinician-item">
                        <span class="remove-btn">✖</span>
                        <span>Ad, Yael</span>
                        <span class="primary-star">☆</span>
                    </div>
                </div>
            </div>

            <div class="week-cell">
                <div class="week-header">Week 4<br>4/14/25</div>
                <div class="clinician-list">
                    <div class="clinician-item">
                        <span class="remove-btn">✖</span>
                        <span>Ad, Yael</span>
                        <span class="primary-star">☆</span>
                    </div>
                </div>
            </div>

            <div class="week-cell">
                <div class="week-header">Week 5<br>4/14/25</div>
                <div class="clinician-list">
                    <div class="clinician-item">
                        <span class="remove-btn">✖</span>
                        <span>Ad, Yael</span>
                        <span class="primary-star">☆</span>
                    </div>
                </div>
            </div>

            <div class="week-cell">
                <div class="week-header">Week 6<br>4/14/25</div>
                <div class="clinician-list">
                    <div class="clinician-item">
                        <span class="remove-btn">✖</span>
                        <span>Ad, Yael</span>
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
                <div class="clinician-list">
                    <div class="clinician-item">
                        <span class="remove-btn">✖</span>
                        <span>Ad, Yael</span>
                    </div>
                </div>
            </div>
        </div>

        <div class="schedule-note">
            Schedule continues similar to https://
            secure-test.vetmed.ucdavis.edu/clinicialscheduler/scheduler/
            default.cfm?mothraID=01654831&amp;page=clinician&lt;O
        </div>
    </div>
</template>

<script lang="ts">
import { defineComponent, ref } from 'vue'

export default defineComponent({
    name: 'RotationScheduleView',
    setup() {
        const selectedRotation = ref('Internal Medicine A')
        const selectedClinician = ref('Hulsebosch, Sean')

        const selectClinician = (clinician: string) => {
            selectedClinician.value = clinician
        }

        return {
            selectedRotation,
            selectedClinician,
            selectClinician
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

.clinician-selector-section {
    margin-bottom: 20px;
    padding: 15px;
    background-color: #f9f9f9;
    border: 1px solid #ddd;
}

.clinician-buttons {
    display: inline-block;
    margin-right: 20px;
}

.clinician-btn {
    padding: 5px 10px;
    margin: 2px;
    background-color: #fff;
    border: 1px solid #ccc;
    cursor: pointer;
    font-size: 13px;
}

.clinician-btn:hover {
    background-color: #e0e0e0;
}

.clinician-btn.selected {
    background-color: #4CAF50;
    color: white;
    border-color: #4CAF50;
}

.clinician-dropdown {
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

.clinician-list {
    padding: 5px;
}

.clinician-item {
    display: flex;
    align-items: center;
    padding: 3px 5px;
    margin: 2px 0;
    font-size: 13px;
    border: 1px solid transparent;
}

.clinician-item.highlight-green {
    background-color: #90EE90;
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