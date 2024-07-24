<script setup lang="ts">
    import type { Ref } from 'vue'
    import type { QTableProps } from 'quasar'
    import { ref, inject } from 'vue'
    import { useFetch } from '@/composables/ViperFetch'
    import { useDateFunctions } from '@/composables/DateFunctions'
    import type { Person } from '@/CTS/types'

    const { formatDate, formatDateTime } = useDateFunctions()

    const apiUrl = inject('apiURL')
    const baseUrl = apiUrl + "cts/"
    const auditRows = ref([]) as Ref<object[]>
    const modifiers = ref([]) as Ref<Person[]>
    const allUsers = ref([]) as Ref<Person[]>
    const actions = ref([]) as Ref<string[]>
    const areas = ref([]) as Ref<string[]>
    const showDetail = ref(false)

    const auditColumns: QTableProps['columns'] = [
        { name: "area", label: "Area", field: "area", align: "left", sortable: true },
        { name: "action", label: "Action", field: "action", align: "left", sortable: true },
        { name: "timestamp", label: "Date/Time", field: "timestamp", align: "left", sortable: true, format: formatDateTime },
        { name: "modifiedBy", label: "By", field: "modifiedByName", align: "left", sortable: true },
        { name: "modifiedPerson", label: "Modified Person", field: "modifiedPersonName", align: "left", sortable: true },
        { name: "detail", label: "Detail", field: "detail", align: "left", sortable: false },
    ]
    const loading = ref(false)
    const pagination = ref({ page: 1, sortBy: "timestamp", descending: true, rowsPerPage: 25, rowsNumber: 100 }) as Ref<any>
    const filter = ref({
        area: null as string | null,
        action: null as string | null,
        dateFrom: null as Date | null,
        dateTo: null as Date | null,
        modifiedBy: null as number | null,
        modifiedPerson: null as number | null,
    }) as Ref<any>

    async function loadAuditRows(props: any) {
        const { page, rowsPerPage, sortBy, descending } = props.pagination
        loading.value = true
        auditRows.value = []

        await loadAudit(page, rowsPerPage, sortBy, descending)
        pagination.value.page = page
        pagination.value.rowsPerPage = rowsPerPage 
        pagination.value.sortBy = sortBy
        pagination.value.descending = descending
        loading.value = false
    }

    async function loadAuditManual() {
        //always start over if filter has changed
        await loadAudit(1, pagination.value.rowsPerPage, pagination.value.sortBy, pagination.value.descending)
    }

    async function loadAudit(page: number, perPage: number, sortBy: string, descending: boolean) {
        const { get } = useFetch()
        const p = new URLSearchParams
        if (filter.value.area != null)
            p.append("area", filter.value.area)
        if (filter.value.action != null)
            p.append("action", filter.value.action)
        if (filter.value.modifiedBy?.personId != null)
            p.append("modifiedById", filter.value.modifiedBy?.personId.toString())
        if (filter.value.modifiedPerson?.personId != null)
            p.append("modifiedPersonId", filter.value.modifiedPerson?.personId.toString())
        if (filter.value.dateFrom != null)
            p.append("dateFrom", filter.value.dateFrom)
        if (filter.value.dateTo != null)
            p.append("dateTo", filter.value.dateTo)

        const u = new URL(baseUrl + "audit", document.baseURI);
        p.append("page", page.toString())
        p.append("perPage", perPage.toString())
        p.append("sortBy", sortBy)
        p.append("descending", descending.toString())
        u.search = p.toString()
        loading.value = true

        get(u.toString())
            .then(({ result, pagination: resultPagination, success }) => {
                auditRows.value = result
                    .map((o: any) => {
                        try {
                            return { ...o, detail2: JSON.parse(o.detail) }
                        }
                        catch (e: any) {
                            return { ...o, detail2: o.detail }
                        }
                    })
                pagination.value.rowsNumber = resultPagination?.totalRecords
            })
        loading.value = false
    }

    async function loadModifiers() {
        const { get } = useFetch()
        get(baseUrl + "audit/modifiers")
            .then(({result}) => modifiers.value = result)
    }

    async function loadUsers() {
        const { get } = useFetch()
        get(apiUrl + "people")
            .then(({ result }) => allUsers.value = result)
    }

    async function loadActions() {
        const { get } = useFetch()
        get(baseUrl + "audit/actions")
            .then(({ result }) => actions.value = result)
    }

    async function loadAreas() {
        const { get } = useFetch()
        get(baseUrl + "audit/areas")
            .then(({ result }) => areas.value = result)
    }

    loadModifiers()
    loadUsers()
    loadActions()
    loadAreas()
</script>

<template>
    <h2>View Audit Log</h2>
    <q-form>
        <div class="row">
            <div class="col-12 col-md-6 col-lg-3">
                <q-select outlined dense options-dense label="Modified By" v-model="filter.modifiedBy" :options="modifiers"
                          option-label="fullNameLastFirst" option-value="personId" clearable></q-select>
            </div>
            <div class="col-12 col-md-6 col-lg-3">
                <q-select outlined dense options-dense label="Modified Person" v-model="filter.modifiedPerson" :options="allUsers"
                          option-label="fullNameLastFirst" option-value="personId" clearable></q-select>
            </div>
            <div class="col-12 col-md-6 col-lg-3">
                <q-select outlined dense options-dense label="Area" v-model="filter.area" :options="areas" clearable></q-select>
            </div>
        </div>
        <div class="row">
            <div class="col-12 col-md-6 col-lg-3">
                <q-input outlined dense type="date" v-model="filter.dateFrom" label="Date from"></q-input>
            </div>
            <div class="col-12 col-md-6 col-lg-3">
                <q-input outlined dense type="date" v-model="filter.dateTo" label="Date To"></q-input>
            </div>
            <div class="col-12 col-md-6 col-lg-3">
                <q-select outlined dense options-dense label="Action" v-model="filter.action" :options="actions" clearable></q-select>
            </div>
        </div>
        <div class="row q-my-sm">
            <div class="col-6 col-md-3 offset-3 text-center">
                <q-btn label="Load Audit Log" color="primary" @click="loadAuditManual()"></q-btn>
            </div>
        </div>
    </q-form>
    <q-table row-key="ctsAuditId"
             :rows="auditRows"
             :loading="loading"
             :columns="auditColumns"
             v-model:pagination="pagination"
             :rows-per-page-options="[5, 10, 15, 25, 50, 100]"
             @request="loadAuditRows">
        <template v-slot:header-cell-detail="props">
            <q-th :props="props">
                <q-toggle v-model="showDetail" label="Details"/>
            </q-th>
        </template>
        <template v-slot:body-cell-detail="props">
            <q-td :props="props" style="max-width:300px;">
                <div v-for="(value, key) in props.row.detail2" v-if="showDetail">
                    {{key}}: {{value}}
                </div>
            </q-td>
        </template>
    </q-table>
</template>