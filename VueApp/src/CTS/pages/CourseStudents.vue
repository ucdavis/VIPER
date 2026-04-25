<script setup lang="ts">
import { ref } from "vue"

const student = ref("")

// Mock data for prototype; real implementation will replace with API data.
const loggedInStudents = [
    { id: 23631, name: "Montserrat Armero", time: "10/16/24 2:29:01 PM" },
    { id: 34123, name: "Hailey Atwood", time: "10/16/24 2:29:01 PM" },
    { id: 34158, name: "Xander Avila", time: "10/16/24 2:29:01 PM" },
]
</script>
<template>
    <h1>Students for VET430 Lab 1 Challenging Communication - Simulated Lab with Actors and Video</h1>

    <h2>Add Student(s)</h2>
    <div class="row q-mb-md">
        <div class="col-12 col-md-6 col-lg-4">
            <q-select
                v-model="student"
                dense
                options-dense
                outlined
                label="Student"
                :options="['Student 1', 'Student 2']"
            ></q-select>
        </div>
    </div>

    <h2 class="q-mt-md">Logged in students</h2>
    <div class="row">
        <div class="col-12 col-md-8 col-lg-6">
            <table
                cellspacing="0"
                cellpadding="3"
                width="100%"
            >
                <thead>
                    <tr>
                        <th class="text-center">Assess Competency</th>
                        <th class="text-left">Student</th>
                        <th class="text-left">Time</th>
                        <th class="text-center">Manually Added</th>
                        <th class="text-center">Remove</th>
                    </tr>
                </thead>
                <tbody>
                    <tr
                        v-for="row in loggedInStudents"
                        :key="row.id"
                    >
                        <td class="text-center">
                            <q-btn
                                dense
                                flat
                                icon="assignment"
                                size="sm"
                                color="primary"
                                :aria-label="`Assess competency for ${row.name}`"
                                :to="`AssessmentCompetency?studentId=${row.id}&sessionId=`"
                            ></q-btn>
                        </td>
                        <td>{{ row.name }}</td>
                        <td>{{ row.time }}</td>
                        <td class="text-center">
                            <q-icon
                                name="check"
                                color="primary"
                                size="sm"
                                aria-label="Manually added"
                            ></q-icon>
                        </td>
                        <td class="text-center">
                            <q-btn
                                dense
                                icon="delete"
                                color="negative"
                                flat
                                size="sm"
                                :aria-label="`Remove ${row.name}`"
                            ></q-btn>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>
    </div>
</template>
