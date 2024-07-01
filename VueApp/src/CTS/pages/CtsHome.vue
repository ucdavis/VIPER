<template>
    <div v-if="loadHome">
        <h2>Competency Tracking System (CTS)</h2>
        <p>
            The Council on Education has mandated that veterinary graduates must have the basic scientific knowledge, skills and values to practice 
            veterinary medicine, independently, at the time of graduation. At a minimum, graduates must be competent in providing 
            entry-level health care for a variety of animal species.
        </p>
        <p>
            The Council on Education has also charged each school with addressing clinical competencies. The Council has defined 9 areas 
            of "clinical competency outcomes" in which a school must prove their graduates are competent. This means that schools must 
            establish objectives for the stated clinical competencies, collect evidence-based data for each competency, and demonstrate 
            how these data are being used to determine that graduates are prepared for entry-level veterinary practice.
        </p>
        <p>
            In order to move UC Davis toward reaching the accreditation requirements, the school has established a set of DVM Learning 
            Outcomes and CORE DVM Clinical Competencies in which ALL GRADUATES must demonstrate proficiency regardless of senior track 
            selection and then DVM Clinical Competencies for each of the species tracks.
        </p>
        <p>
            We have designed the Competency Tracking System (CTS) to work together with CREST to link session content with DVM 
            Clinical Competencies.
        </p>
    </div>
</template>
<script setup lang="ts">
    import { useQuasar } from 'quasar'
    import { ref, defineComponent, inject } from 'vue'
    import { useFetch } from '@/composables/ViperFetch'
    import { useUserStore } from '@/store/UserStore'

</script>
<script lang="ts">
    export default defineComponent({
        name: "CtsHome",
        data() {
            return {
                userInfo: ref({}),
                loadHome: ref(false)
            }
        }
        ,
        mounted: async function () {
            const baseUrl = inject('apiURL')
            const userStore = useUserStore()
            const $q = useQuasar()
            $q.loading.show({
                message: "Logging in",
                delay: 250 // ms
            })

            const { get } = useFetch()
            const r = await get(baseUrl + "loggedInUser")
            if (!r.success || !r.result.userId) {
                window.location.href = import.meta.env.VITE_VIPER_HOME + "login?ReturnUrl=" + import.meta.env.VITE_VIPER_HOME + "CTS/"
            }
            else {
                userStore.loadUser(r.result)
            }
            $q.loading.hide()

            if (userStore.isLoggedIn) {
                if (this.$route.query.sendBackTo != undefined) {
                    const redirect = this.$route.query.sendBackTo?.toString()
                    let paramString = redirect.split("?")[1]
                    let params = {} as any
                    if (paramString) {
                        let queryString = new URLSearchParams(paramString)
                        queryString.forEach((val: string, key: string) => {
                            params[key] = val
                        })
                    }
                    this.$router.push({ path: redirect.split("?")[0], query: params ?? null })
                }
                else {
                    this.loadHome = true
                }
                
            }
        }
    })
</script>