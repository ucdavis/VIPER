<script setup lang="ts">
    import StudentSelect from '@/components/StudentSelect.vue'
</script>

<template>
    <div class="row">
        <div class="col-12 col-md-8 col-lg-6">
            <div>
                Default (active students)
                <StudentSelect></StudentSelect>
            </div>
            <div>
                All students
                <StudentSelect allStudents="all"></StudentSelect>
            </div>
            <div>
                All students with toggle
                <StudentSelect allStudents="all" :statusToggle="true"></StudentSelect>
            </div>
            <div>
                Service / V4 / Active
                <StudentSelect :serviceId="823" :classLevel="['V4']"></StudentSelect>
            </div>
            <div>
                V1 - V4
                <StudentSelect allStudents="hide" :classLevel="['V1', 'V2', 'V3', 'V4']"></StudentSelect>
            </div>
            <div>
                Active class years
                <StudentSelect :classYears="['active']"></StudentSelect>
            </div>
        </div>
        <div class="col-12 col-md-10 col-lg-8">

            <div>
                All class years and class levels
                <StudentSelect allStudents="hide" :classLevel="['V1', 'V2', 'V3', 'V4']" :classYears="['all']"></StudentSelect>
            </div>
        </div>
    </div>
</template>