<template>
    {{ collections }}

    <h2>Link Collections</h2>

    <q-form>
        <div class="row items-center q-mb-md">
            <div class="col-12 col-sm-8 col-md-4 col-lg-2">
                <q-select v-model="collectionId"
                          map-options
                          emit-value
                          dense options-dense
                          class="q-mx-lg"
                          label="Link Collection"
                          :options="collections"
                          option-value="linkCollectionId"
                          option-label="linkCollection">
                </q-select>
            </div>
            <div class="col-auto">
                <q-btn color="green"
                       icon="add"
                       label="New Collection"
                       no-caps
                       dense
                       class="q-mx-lg"
                       @click="showCollectionDialog = true" />
            </div>
            <div class="col-auto" v-if="collectionId != null">
                <q-btn color="primary"
                       icon="edit"
                       label="Edit Collection"
                       dense
                       no-caps
                       class="q-mx-lg"
                       @click="showCollectionDialog = true" />
            </div>
        </div>
    </q-form>


    <!-- Create/Edit Dialog -->
    <q-dialog v-model="showCollectionDialog" persistent>
        <q-card style="min-width: 350px">
            <q-card-section>
                <div class="text-h6">{{ collectionId != null ? 'Edit' : 'Create' }} Collection</div>
            </q-card-section>

            <q-card-section class="q-pt-none">
                <q-input dense
                         v-model="collectionData.linkCollection"
                         label="Collection Name"
                         :rules="[val => val && val.length > 0 || 'Please enter collection name']"
                         ref="collectionNameInput" />
            </q-card-section>

            <q-card-actions align="right" class="text-primary">
                <q-btn flat label="Cancel" @click="cancelCollectionDialog" dense no-caps />
                <q-btn flat
                       label="Save"
                       @click="saveCollection" 
                       dense no-caps
                       />
                <q-btn flat
                       label="Delete"
                       @click="deleteCollection"
                       dense no-caps
                       color="red-5"/>
            </q-card-actions>

            <q-card-section>
                Tags
            </q-card-section>
        </q-card>
    </q-dialog>

    <template v-if="collectionId != 0">
        <h2>Links</h2>
        {{ links }}
    </template>
</template>

<script setup lang="ts">
    import { ref, inject, watch } from 'vue'
    import type { Ref } from 'vue'
    import type { LinkCollection, Link, LinkCollectionTagCategory, LinkTag, LinkTagFilter } from '@/CMS/types/'
    import { useFetch } from '@/composables/ViperFetch'

    const apiURL = inject('apiURL') + 'cms/linkCollections/' as string
    const collections: Ref<LinkCollection[]> = ref([])
    const collectionId: Ref<number | null> = ref(null)
    const collection: Ref<LinkCollection | null> = ref(null)
    const collectionData: Ref<any> = ref({})
    const showCollectionDialog = ref(false)
    const collectionNameInput: Ref<any> = ref(null)

    const collectionTags: Ref<LinkCollectionTagCategory[]> = ref([])

    const links: Ref<Link[]> = ref([])
    const link: Ref<Link | null> = ref(null)

    async function loadCollections() {
        const { get } = useFetch()
        const res = await get(apiURL)
        collections.value = res.result
    }

    async function saveCollection() {
        if (collectionData.value.linkCollection == null || collectionData.value.linkCollection.trim().length == 0) {
            collectionNameInput.value.focus()
            return
        }

        const { put, post } = useFetch()

        if (collection.value != null && collection.value.linkCollectionId > 0) {
            await put(apiURL + collection.value.linkCollectionId, { linkCollection: collectionData.value.linkCollection })
        } else {
            await post(apiURL, { linkCollection: collectionData.value.linkCollection })
        }

        await loadCollections()
        cancelCollectionDialog()
    }

    async function deleteCollection() {
        const { del } = useFetch()
        await del(apiURL + collection.value?.linkCollectionId)
        await loadCollections()
        cancelCollectionDialog()
    }

    async function cancelCollectionDialog() {
        showCollectionDialog.value = false
    }

    async function loadLinks() {
        const { get } = useFetch()
        if (collectionId.value) {
            const res = await get(apiURL + collectionId.value + '/links')
            links.value = res.result
        } else {
            links.value = []
        }
    }

    async function loadTags() {
        const { get } = useFetch()
        if (collectionId.value) {
            const res = await get(apiURL + collectionId.value + '/tags')
            collectionTags.value = res.result
        } else {
            collectionTags.value = []
        }

    }

    watch(collectionId, () => {
        loadLinks()
        loadTags()
        collection.value = collections.value.find(c => c.linkCollectionId == collectionId.value) || null
        collectionData.value.linkCollection = collection.value?.linkCollection || ''
    })

    loadCollections()

</script>

<style scoped>

</style>