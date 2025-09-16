<template>
    <h2>{{ linkCollection?.linkCollection }}</h2>

    <q-form>
        <div class="row q-pa-sm q-gutter-md">
        <template v-for="tf in tagFilters">
            <q-select v-model="tf.selected" 
                      dense options-dense
                      :options="tf.options" 
                      :label="tf.linkCollectionTagCategory" 
                      stack-label 
                      clearable
                      class="col col-md-3 col-lg-2 col-xl-1"
                      ></q-select>
        </template>
        </div>
    </q-form>

    <div class="q-pa-md row items-start q-gutter-md" v-if="linkCollection != null">
        <q-card v-for="li in filteredLinks" :bordered=true :href="li.url" class="link-card">
            <q-card-section class="q-py-sm">
                <div class="text-h3">{{ li.title }}</div>
            </q-card-section>
            <q-card-section class="q-py-sm">
                {{ li.description }}
            </q-card-section>
            <q-card-section class="q-py-sm">
                <template v-for="lct in linkCollection.linkCollectionTagCategories">
                    <template v-for="tag in li.linkTags">
                        <q-badge v-if="tag.linkCollectionTagCategoryId == lct.linkCollectionTagCategoryId" :color="getLinkCollectionTagColor(lct.sortOrder)"
                                 class="link-tag q-px-sm">
                            {{ tag.value }}
                        </q-badge>
                    </template>
                </template>
            </q-card-section>
        </q-card>
    </div>
</template>

<script setup lang="ts">
    import type { Ref } from 'vue'
    import { ref, onMounted, defineProps, watch} from 'vue'
    import type { Link, LinkCollection, LinkCollectionTagCategory, LinkTag, LinkTagFilter } from '@/CMS/types'
    import { useFetch } from '@/composables/ViperFetch'

    const props = defineProps({
        linkCollectionName: {
            type: String,
            required: true
        },
    })
    const tagFilters: Ref<LinkTagFilter[]> = ref([])
    const linkCollection: Ref<LinkCollection | null> = ref(null)
    
    const links: Ref<Link[]> = ref([])
    const filteredLinks: Ref<Link[]> = ref([])

    async function getLinkCollection() {
        const { get } = useFetch()
        const baseUrl = import.meta.env.VITE_API_URL + "cms/linkcollections/"
        const r = await get(baseUrl + "?linkCollectionName=" + encodeURIComponent(props.linkCollectionName))
        linkCollection.value = r.result[0] as LinkCollection
        if (linkCollection.value) {
            await loadLinks(linkCollection.value.linkCollectionId)
            loadFilters()
        }
    }

    async function loadLinks(collectionId: number) {
        const { get } = useFetch()
        const baseUrl = import.meta.env.VITE_API_URL + "cms/linkCollections/" + collectionId + "/links"
        const r = await get(baseUrl)
        links.value = r.result as Link[]
        filteredLinks.value = links.value
    }

    function loadFilters() {
        if (linkCollection.value != null) {
             for (var tagCat of linkCollection.value.linkCollectionTagCategories) {
                 tagFilters.value.push({
                     linkCollectionTagCategoryId: tagCat.linkCollectionTagCategoryId,
                     linkCollectionTagCategory: tagCat.linkCollectionTagCategory,
                     options: [],
                     selected: null
                 });
             }
             for (var l of links.value) {
                 for (var lt of l.linkTags) {
                     var t = tagFilters.value.find(t => t.linkCollectionTagCategoryId == lt.linkCollectionTagCategoryId)
                     if (t) {
                         t.options.push(lt.value)
                     }
                 }
             }
             for (var tagOptions of tagFilters.value) {
                 tagOptions.options = [...new Set(tagOptions.options)]
             }
        }
    }

    function applyFilters() {
        filteredLinks.value = links.value
        for (var tf of tagFilters.value) {
            if (tf.selected != null && tf.selected != "") {
                filteredLinks.value = filteredLinks.value.filter((fl: Link) => {
                    const findTag = fl.linkTags.find((lt: LinkTag) => {
                        return lt.linkCollectionTagCategoryId == tf.linkCollectionTagCategoryId
                            && lt.value == tf.selected
                    })
                    return findTag !== undefined
                })
            }
        }
    }

    watch(tagFilters, () => {
        applyFilters()
    }, {deep: true})

    getLinkCollection()

    function getLinkCollectionTagColor(order: number) {
        const colors = ["orange", "grey", "purple", "green", "blue"]
        return colors.length >= order ? colors[order - 1] : colors[colors.length - 1]
    }
</script>

<style scoped>
    .link-card {
        max-width: 350px;
        width: 100%;
    }

    .link-tag {
        margin-right: 2px;
    }
</style>