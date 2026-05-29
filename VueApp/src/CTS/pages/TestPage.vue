<script setup lang="ts">
import StudentSelect from "@/components/StudentSelect.vue"
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
                <StudentSelect all-students="all"></StudentSelect>
            </div>
            <div>
                All students with toggle
                <StudentSelect
                    all-students="all"
                    :status-toggle="true"
                ></StudentSelect>
            </div>
            <div>
                Service / V4 / Active
                <StudentSelect
                    :service-id="823"
                    :class-level="['V4']"
                ></StudentSelect>
            </div>
            <div>
                V1 - V4
                <StudentSelect
                    all-students="hide"
                    :class-level="['V1', 'V2', 'V3', 'V4']"
                ></StudentSelect>
            </div>
            <div>
                Active class years
                <StudentSelect :class-years="['active']"></StudentSelect>
            </div>
        </div>
        <div class="col-12 col-md-10 col-lg-8">
            <div>
                All class years and class levels
                <StudentSelect
                    all-students="hide"
                    :class-level="['V1', 'V2', 'V3', 'V4']"
                    :class-years="['all']"
                ></StudentSelect>
            </div>
        </div>
    </div>
</template>
