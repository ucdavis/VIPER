<script setup lang="ts">
    import { ref } from 'vue'
    const student = ref("")
</script>
<template>
    <h2>Students for VET430 Lab 1 Challenging Communication - Simulated Lab with Actors and Video</h2>

    <h3>Add Student(s)</h3>
    <div class="row q-mb-md">
        <div class="col-12 col-md-6 col-lg-4">
            <q-select dense options-dense outlined :options="['Student 1', 'Student 2']" v-model="student"></q-select>
        </div>
    </div>

    <h3 class="q-mt-md">Logged in students</h3>
    <div class="row">
        <div class="col-12 col-md-8 col-lg-6">
            <table cellspacing="0" cellpadding="3" width="100%">
                <thead>
                    <tr>
                        <th>Assess Competency</th>
                        <th class="text-left">Student</th>
                        <th class="text-left">Time</th>
                        <th class="text-center">Manually Added</th>
                        <th class="text-left">Remove</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td>
                            <q-btn dense flat icon="assignment" size="sm" color="primary" to="AssessmentCompetency?studentId=23631&sessionId="></q-btn>
                        </td>
                        <td>Montserrat Armero</td>
                        <td>10/16/24 2:29:01 PM</td>
                        <td class="text-center">
                            <q-icon name="check" color="primary" size="sm"></q-icon>
                        </td>
                        <td>
                            <q-btn dense icon="delete" color="red-5" flat size="sm"></q-btn>
                        </td>
                    </tr>
                    <tr>
                        <td>
                            <q-btn dense flat icon="assignment" size="sm" color="primary" to="AssessmentCompetency?studentId=34123&sessionId="></q-btn>
                        </td>
                        <td>Hailey Atwood</td>
                        <td>10/16/24 2:29:01 PM</td>
                        <td class="text-center">
                            <q-icon name="check" color="primary" size="sm"></q-icon>
                        </td>
                        <td>
                            <q-btn dense icon="delete" color="red-5" flat size="sm"></q-btn>
                        </td>
                    </tr>
                    <tr>
                        <td>
                            <q-btn dense flat icon="assignment" size="sm" color="primary" to="AssessmentCompetency?studentId=34158&sessionId="></q-btn>
                        </td>
                        <td>Xander Avila</td>
                        <td>10/16/24 2:29:01 PM</td>
                        <td class="text-center">
                            <q-icon name="check" color="primary" size="sm"></q-icon>
                        </td>
                        <td>
                            <q-btn dense icon="delete" color="red-5" flat size="sm"></q-btn>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>
    </div>
</template>