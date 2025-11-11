<script setup lang="ts">
    import { ref, inject } from 'vue'
    import type { Ref } from 'vue'
    import { useFetch } from '@/composables/ViperFetch'

    const pgm = ref(null)
    const glSummary = ref(null)
    const files = ref(null)
    //default start dates are 10/1 this year to 9/30 next year. NB javascript months are 0 indexed
    const startDate = ref(new Date(new Date().getFullYear() - 1, 9, 1).toISOString().split('T')[0])
    const endDate = ref(new Date(new Date().getFullYear(), 8, 30).toISOString().split('T')[0])
    const downloadOk = ref(false)
    const processing = ref(false)
    const errorMessage = ref('')
    const successMessage = ref('')
    const baseUrl = inject('apiURL') + "research/aggieEnterpriseReports/"

    const onPgmChange = (file) => {
        errorMessage.value = ''
        successMessage.value = ''
    }

    const onGlSummaryChange = (file) => {
        errorMessage.value = ''
        successMessage.value = ''
    }

    const onRejected = (rejectedEntries) => {
        const entry = rejectedEntries[0]
        if (entry.failedPropValidation === 'max-file-size') {
            errorMessage.value = 'File size exceeds 10MB limit'
        } else {
            errorMessage.value = 'Invalid file format. Please upload .xlsx.'
        }
    }

    //Upload files. Does not trigger validation of the files on the server.
    const processFiles = async () => {
        if (!pgm.value || !glSummary.value) {
            errorMessage.value = 'Please upload both files'
            return
        }

        processing.value = true
        errorMessage.value = ''
        successMessage.value = ''

        try {
            const formData = new FormData();
            formData.append('pgmData', pgm.value);
            formData.append('glSummary', glSummary.value);
            const response = await fetch(baseUrl + "upload", {
                method: 'POST',
                body: formData
            })
            if (!response.ok) {
                errorMessage.value = "Error uploading file"
            }

        } catch (error) {
            errorMessage.value = 'Error processing files: ' + error.message
        } finally {
            processing.value = false
        }
    }

    //Download report as blob, create an anchor to simulate a download
    const downloadReport = async () => {
        processing.value = true
        const qs = "fiscalYearStart=" + startDate.value + "&fiscalYearEnd=" + endDate.value
        fetch(baseUrl + "generateReport?" + qs, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        })
        .then(response => {
            if(!response.ok) {
                throw "Download failed"
            }
            // Get filename from Content-Disposition header if available
            const contentDisposition = response.headers.get('Content-Disposition')
            let filename = 'Aggie Enterprise Report.xlsx'; // default filename
            if (contentDisposition) {
                const filenameMatch = contentDisposition.match(/filename="?(.+)"?/i)
                if (filenameMatch) {
                    filename = filenameMatch[1].split(";")[0]
                }
            }
    
            return response.blob().then(blob => ({ blob, filename }))
        })
        .then(({ blob, filename }) => {
            // Create download link
            const url = window.URL.createObjectURL(blob)
            const a = document.createElement('a')
            a.href = url
            a.download = filename
            document.body.appendChild(a)
            a.click()
    
            // Cleanup
            window.URL.revokeObjectURL(url)
            document.body.removeChild(a)
            processing.value = false
        })
        .catch((error) => {
            errorMessage.value = "Download failed"
            processing.value = false
        })

    }

    //clear files
    const resetFiles = () => {
        pgm.value = null
        glSummary.value = null
        errorMessage.value = ''
        successMessage.value = ''
    }

    //check that files exist on the server. allow download of report if they do.
    const checkFiles = async () => {
        processing.value = true
        const { get } = useFetch()
        const r = await get(baseUrl + "filecheck")
        if (r.success) {
            files.value = r.result
            downloadOk.value = files.value.length == 2 && files.value[0] != "" && files.value[1] != ""
        }
        processing.value = false
    }

    checkFiles()
</script>

<template>
    <q-card style="width: 600px; max-width: 90vw;">
        <q-card-section>
            <div class="text-h2 q-mb-xs">Generate AD419 Report</div>
        </q-card-section>
        <!--Show files that already exist on the server, if any. Allow download of report if both files are present. -->
        <q-card-section>
            <div v-for="(f, idx) in files" class="row">
                <template v-if="f == ''">
                    <div class="col-12 text-red">
                        {{ idx == 0 ? 'PGM Master Data:' : 'GL Summary:'}} file missing
                    </div>
                </template>
                <template v-else>
                    <div class="col-8">
                        {{ f.split(' ')[0] }}
                    </div>
                    <div>
                        {{ f.split(' ')[1] }}
                    </div>
                </template>
            </div>
        </q-card-section>
        <q-card-section>
            <div class="row q-mb-sm">
                <div class="col-12 col-sm-8 col-md-6">
                    <q-input type="date"
                             v-model="startDate"
                             dense
                             outlined
                             label="Fiscal Year Start"
                             clearable />
                </div>
                <div class="col-12 col-sm-8 col-md-6">
                    <q-input type="date"
                             v-model="endDate"
                             dense
                             outlined
                             label="Fiscal Year Start"
                             clearable />
                </div>
            </div>
            <q-btn unelevated
                   label="Download report using these files"
                   color="primary"
                   icon="download"
                   @click="downloadReport"
                   :loading="processing"
                   :disable="!downloadOk" />
        </q-card-section>

        <q-separator />

        <!--Form to upload PGM master data and GL Summary-->
        <q-card-section>
            <div class="text-h6">Upload PGM Master data and GL Summary</div>
        </q-card-section>
        <q-card-section>
            <div class="q-gutter-md">
                <!-- File 1 Upload -->
                <div>
                    <q-file v-model="pgm"
                            label="PGM Master Data"
                            dense
                            outlined
                            accept=".xlsx"
                            max-file-size="20971520"
                            @update:model-value="onPgmChange"
                            @rejected="onRejected">
                        <template v-slot:prepend>
                            <q-icon name="attach_file" />
                        </template>
                        <template v-slot:append>
                            <q-icon v-if="pgm"
                                    name="cancel"
                                    @click.stop.prevent="pgm = null"
                                    class="cursor-pointer" />
                        </template>
                    </q-file>
                </div>
                <!-- File 2 Upload -->
                <div>
                    <q-file v-model="glSummary"
                            label="GL Summary"
                            dense
                            outlined
                            accept=".xlsx"
                            max-file-size="20971520"
                            @update:model-value="onGlSummaryChange"
                            @rejected="onRejected">
                        <template v-slot:prepend>
                            <q-icon name="attach_file" />
                        </template>
                        <template v-slot:append>
                            <q-icon v-if="glSummary"
                                    name="cancel"
                                    @click.stop.prevent="glSummary = null"
                                    class="cursor-pointer" />
                        </template>
                    </q-file>
                </div>

                <!-- Status Messages -->
                <q-banner v-if="errorMessage" class="bg-negative text-white" rounded>
                    <template v-slot:avatar>
                        <q-icon name="error" />
                    </template>
                    {{ errorMessage }}
                </q-banner>

                <q-banner v-if="successMessage" class="bg-positive text-white" rounded>
                    <template v-slot:avatar>
                        <q-icon name="check_circle" />
                    </template>
                    {{ successMessage }}
                </q-banner>
            </div>
        </q-card-section>

        <q-card-actions align="right" class="q-pa-md">
            <q-btn flat
                   label="Reset"
                   color="grey-7"
                   @click="resetFiles"
                   :disable="!pgm && !glSummary" />
            <q-btn unelevated
                   label="Upload Files"
                   color="primary"
                   icon="play_arrow"
                   @click="processFiles"
                   :loading="processing"
                   :disable="!pgm || !glSummary" />
        </q-card-actions>

        <q-card-section class="q-pt-none">
            <div class="text-caption text-grey-7">
                <q-icon name="info" size="xs" class="q-mr-xs" />
                Supported formats: Excel (.xlsx) - Max 20MB per file.
            </div>
        </q-card-section>
    </q-card>
</template>

<style scoped>

</style>
