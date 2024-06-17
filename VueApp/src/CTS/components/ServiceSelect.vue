<script setup lang="ts">
    import type { Ref } from 'vue'
    import { ref, defineProps, watch } from 'vue'
    import type { ServiceSelect } from '@/CTS/types'
    import { useFetch } from '@/composables/ViperFetch'
    import { useUserStore } from '@/store/UserStore'
    const userStore = useUserStore()

    const props = defineProps({
        forScheduledStudent: {
            type: Boolean,
            default: false,
        },
        forScheduledInstructor: {
            type: Boolean,
            default: false
        },

    })

    const emit = defineEmits(['serviceChange'])
    const selectedService = ref({ serviceId: 0, serviceName: "" }) as Ref<ServiceSelect>
    const services = ref([]) as Ref<ServiceSelect[]>
    const baseUrl = import.meta.env.VITE_API_URL + "cts/"

    const handleServiceChange = () => {
        emit('serviceChange', selectedService.value.serviceId)
    }

    async function getServices() {
        const { result, get } = useFetch()
        await get(baseUrl + "clinicalservices")
        services.value = result.value

        //get all scheduled services along with services scheduled this week and last week
        await get(baseUrl + "clinicalschedule/instructor?mothraId=" + userStore.userInfo.mothraId)
        const scheduledServices = result.value

        let today = new Date()
        today.setHours(0, 0, 0, 0)
        let sunday = new Date(today)
        sunday.setDate(sunday.getDate() - sunday.getDay())
        let schedThisWeek = scheduledServices.find((s: any) => today >= new Date(s.dateStart) && today <= new Date(s.dateEnd))
        let schedLastWeek = scheduledServices.find((s: any) => new Date(s.dateEnd).getTime() == sunday.getTime())

        services.value.forEach(s => {
            s.thisWeek = schedThisWeek && schedThisWeek.serviceId == s.serviceId
            s.lastWeek = schedLastWeek && schedLastWeek.serviceId == s.serviceId
            s.scheduled = scheduledServices.find((ss: any) => ss.serviceId == s.serviceId)
        })

        //auto select a service - this week, then last week
        if (schedThisWeek) {
            selectedService.value = services.value.find((s: any) => s.serviceId == schedThisWeek.serviceId) ?? {} as ServiceSelect
        }
        else if (schedLastWeek) {
            selectedService.value = services.value.find((s: any) => s.serviceId == schedLastWeek.serviceId) ?? {} as ServiceSelect
        }
    }

    watch(selectedService, () => {
        handleServiceChange()
    })

    getServices()
</script>

<template>
    <q-select label="Select Service" dense options-dense outlined
              v-model="selectedService" option-label="serviceName" option-value="serviceId" :options="services">
        <template v-slot:option="scope">
            <q-item v-bind="scope.itemProps">
                <q-item-section side v-if="scope.opt.thisWeek">
                    <q-badge color="green">This Week</q-badge>
                </q-item-section>
                <q-item-section side v-if="scope.opt.lastWeek && !scope.opt.thisWeek">
                    <q-badge color="blue">Last Week</q-badge>
                </q-item-section>
                <q-item-section side v-if="scope.opt.scheduled && !scope.opt.lastWeek && !scope.opt.thisWeek">
                    <q-badge color="grey-5">Scheduled</q-badge>
                </q-item-section>
                <q-item-section>
                    <q-item-label>{{scope.opt.serviceName}}</q-item-label>
                </q-item-section>
            </q-item>
        </template>
    </q-select>
</template>