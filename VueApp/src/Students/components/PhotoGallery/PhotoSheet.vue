<template>
    <div class="photo-sheet">
        <div class="print-header q-mb-md row items-center">
            <div class="col text-left">
                <div class="text-h6">{{ title }}</div>
            </div>
            <div class="col-auto text-right">
                <div class="text-subtitle2">{{ formattedDate }}</div>
            </div>
        </div>

        <div class="photo-grid">
            <div
                v-for="student in students"
                :key="student.mailId"
                class="student-cell"
            >
                <div class="photo-container">
                    <img
                        :src="photoUrl(student)"
                        :alt="`${student.firstName} ${student.lastName}`"
                        class="student-photo"
                    />
                </div>
                <div class="student-name">
                    <div class="text-caption">{{ student.lastName }},</div>
                    <div class="text-caption">{{ student.firstName }}</div>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed } from "vue"
import type { StudentPhoto } from "../../services/photo-gallery-service"
import { getPhotoUrl } from "../../composables/use-photo-url"

defineProps<{
    students: StudentPhoto[]
    title: string
}>()

const formattedDate = computed(() => {
    const now = new Date()
    const month = now.getMonth() + 1
    const day = now.getDate()
    const year = now.getFullYear()
    return `As of ${month}/${day}/${year}`
})

function photoUrl(student: StudentPhoto): string {
    return getPhotoUrl(student)
}
</script>

<style scoped>
.photo-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(55px, 55px));
    gap: 2px;
    justify-content: start;
}

.student-cell {
    display: flex;
    flex-direction: column;
    align-items: center;
}

.student-photo {
    width: 55px;
    height: 70px;
    object-fit: cover;
    display: block;
}

.student-name {
    text-align: center;
    line-height: 1.1;
}

.text-caption {
    font-size: 9px;
}

@media print {
    .no-print {
        display: none !important;
    }

    .print-header {
        break-before: auto;
        margin-bottom: 10px !important;
        display: flex !important;
        flex-direction: row !important;
        align-items: center !important;
        justify-content: space-between !important;
    }

    .print-header .text-h6 {
        text-align: left !important;
    }

    .print-header .text-subtitle2 {
        text-align: right !important;
    }

    .photo-sheet {
        display: block !important;
        visibility: visible !important;
    }

    .photo-grid {
        display: grid !important;
        grid-template-columns: repeat(auto-fit, minmax(55px, 55px)) !important;
        gap: 2px !important;
        justify-content: start !important;
        visibility: visible !important;
    }

    .student-cell {
        display: flex !important;
        flex-direction: column !important;
        align-items: center !important;
        visibility: visible !important;
    }

    .photo-container {
        display: block !important;
        visibility: visible !important;
    }

    /* Ensure images display properly in print */
    .student-photo {
        display: block !important;
        visibility: visible !important;
        width: 55px !important;
        height: 70px !important;
        object-fit: cover !important;
        opacity: 1 !important;
        -webkit-print-color-adjust: exact !important;
        print-color-adjust: exact !important;
    }

    /* Ensure text is visible */
    .student-name {
        display: block !important;
        visibility: visible !important;
        color: #000 !important;
        text-align: center !important;
        line-height: 1.1 !important;
    }

    .text-caption {
        display: block !important;
        visibility: visible !important;
        font-size: 8px !important;
        color: #000 !important;
    }

    .text-h6,
    .text-subtitle2 {
        color: #000 !important;
        display: block !important;
        visibility: visible !important;
    }
}
</style>
