<template>
    <div class="report-layout">
        <div class="report-header no-print-hide">
            <slot name="header" />
        </div>
        <div
            v-if="$slots.toolbar"
            class="report-toolbar no-print"
        >
            <slot name="toolbar" />
        </div>
        <div class="report-content">
            <slot />
        </div>
    </div>
</template>

<script setup lang="ts"></script>

<style scoped>
.report-toolbar {
    display: flex;
    justify-content: flex-end;
    gap: 0.5rem;
    margin-bottom: 0.5rem;
}

@media print {
    .no-print {
        display: none;
    }

    .no-print-hide {
        display: block;
    }
}
</style>

<style>
@media print {
    /* Global print styles for report pages */
    .report-layout {
        font-size: 10pt;
    }

    @page {
        size: landscape;
        margin: 0.5in;
    }

    /* Hide non-report elements */
    #mainLeftDrawer,
    .q-header,
    .q-footer,
    .q-drawer,
    .no-print {
        display: none;
    }

    .q-page-container {
        padding: 0;
    }

    /* Clean print appearance */
    .q-card {
        box-shadow: none;
        border: none;
    }

    /* Ensure table headers repeat on each page */
    thead {
        display: table-header-group;
    }

    /* Prevent rows from breaking across pages */
    tr {
        page-break-inside: avoid;
    }

    /* Remove backgrounds for clean print */
    .q-table--dense .q-td,
    .q-table--dense .q-th {
        background: white;
    }
}
</style>
