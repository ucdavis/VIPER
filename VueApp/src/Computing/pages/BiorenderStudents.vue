<script setup lang="ts">
    import { ref, inject } from 'vue'
    import type { Ref } from 'vue'
    import type { QTableProps } from 'quasar'
    import { useFetch } from '@/composables/ViperFetch'
    import { exportTable } from '@/composables/QuasarTableUtilities'

    const emails = ref("") as Ref<string>
    const rows = ref([]) as Ref<any[]>
    const search = ref("")
    const cols: QTableProps['columns'] = [
        { name: "email", label: "Email", field: "email", align: "left" },
        { name: "iamId", label: "Iam Id", field: "iamId", align: "left", sortable: true },
        { name: "firstName", label: "First Name", field: "firstName", align: "left", sortable: true },
        { name: "lastName", label: "Last Name", field: "lastName", align: "left", sortable: true },
        { name: "studentType", label: "Student Type", field: "studentType", align: "left", sortable: true },
        { name: "collegeSchool", label: "College / School", field: "collegeSchool", align: "left", sortable: true },
        { name: "studentAssociations", label: "Student Associations (College/Major/Level/Class)", field: "", align: "left", sortable: true },
    ]
    const pagination = ref({ page: 1, sortBy: "lastName", descending: true, rowsPerPage: 0 }) as Ref<any>
    const stdUrl = inject('apiURL') + "people/biorenderStudents"

    async function getData() {
        const { get } = useFetch()
        const url = new URL(stdUrl, document.baseURI)
        var emailArray = emails.value.split(",")
        emailArray.forEach(e => url.searchParams.append('emails', e))
        const r = await get(url.toString())
        rows.value = r.result
    }

    function exportData() {
        exportTable(cols, rows.value, ["studentAssociations"])
    }

</script>
<template>
    <h2>Biorender Student Lookup</h2>
    <q-form class="q-mb-md">
        <div class="row">
            <q-input type="textarea" v-model="emails" dense outlined label="Emails (comma separated)" class="col col-12 col-md-6 col-lg-4"></q-input>
        </div>
        <q-btn dense no-caps color="primary" label="Get data" @click="getData()" class="q-px-md"></q-btn>
    </q-form>

    <q-table :rows="rows"
             :columns="cols"
             row-key="email"
             dense
             v-model:pagination="pagination"
             :filter="search"
             title="Student Info">
        <template v-slot:top-right="props">
            <q-btn dense no-caps label="Export" @click="exportData()" color="primary" class="q-px-md q-mr-md"></q-btn>
            <q-input class="q-ml-xs q-mr-xs" v-model="search" dense outlined debounce="300" placeholder="Filter Results">
                <template v-slot:append>
                    <q-icon name="filter_alt" />
                </template>
            </q-input>
        </template>
        <template v-slot:body-cell-studentAssociations="props">
            <q-td :props="props">
                <div v-for="s in props.row.studentAssociations">
                    {{ s.assocRank }}.
                    {{ s.collegeCode }}
                    {{ s.collegeName }}
                    /
                    {{ s.majorCode }}
                    {{ s.majorName }}
                    /
                    {{ s.levelCode }}
                    {{ s.levelName  }}
                    /
                    {{ s.classCode  }}
                    {{ s.className  }}
                </div>
            </q-td>
        </template>
    </q-table>
</template>