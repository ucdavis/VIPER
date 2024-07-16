<script setup lang="ts">
    import {
        Chart as ChartJS,
        Title,
        Tooltip,
        Legend,
        BarElement,
        CategoryScale,
        LinearScale
    } from 'chart.js/auto'
    import { Bar, Line } from 'vue-chartjs'
    const data = {
        labels: [
            'January',
            'February',
            'March',
            'April',
            'May',
            'June',
            'July',
            'August',
            'September',
            'October',
            'November',
            'December'
        ],
        datasets: [
            {
                label: 'Data One',
                backgroundColor: '#f87979',
                data: [40, 20, 12, 39, 10, 40, 39, 80, 40, 20, 12, 11]
            }
        ]
    }

    const options = {
        responsive: true,
        maintainAspectRatio: false
    }
    ChartJS.register(CategoryScale, LinearScale, BarElement, Title, Tooltip, Legend)

</script>
<template>
    <Bar :data="data" :options="options" />
</template>