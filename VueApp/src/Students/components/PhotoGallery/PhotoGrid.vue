<template>
    <div class="photo-grid">
        <div class="row q-col-gutter-sm">
            <div
                v-for="(student, index) in students"
                :key="student.mailId"
                class="col-4 col-sm-3 col-md-2 col-lg-2 col-xl-1"
            >
                <StudentPhotoCard
                    :student="student"
                    :students="students"
                    :current-index="index"
                />
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import type { StudentPhoto } from "../../services/photo-gallery-service"
import StudentPhotoCard from "./StudentPhotoCard.vue"

defineProps<{
    students: StudentPhoto[]
}>()
</script>

<style scoped>
.photo-grid {
    width: 100%;
}
</style>
