<template>
    <h2>{{ linkCollection?.linkCollection }}</h2>

    <q-form v-if="loaded">
        <div class="row q-pa-sm q-gutter-md">
        <template v-for="tf in tagFilters">
            <q-select v-model="tf.selected" 
                      dense 
                      options-dense
                      outlined
                      :options="tf.options" 
                      :label="tf.linkCollectionTagCategory" 
                      stack-label 
                      clearable
                      class="col col-md-3 col-lg-2 col-xl-1"
                      ></q-select>
        </template>
            <q-input v-model="search"
                     dense
                     outlined
                     label="Search"
                     stack-label
                     clearable
                     class="col col-md-3 col-lg-2"></q-input>
        </div>
    </q-form>

    <div class="q-pa-md row q-gutter-md" v-if="linkCollection != null">
        <q-card v-for="li in filteredLinks" 
                :bordered=true 
                clickable
                @click="openWebReports(li.url)"
                class="link-card cursor-pointer q-hoverable"
                v-ripple>
            <span class="q-focus-helper"></span>
            <q-card-section class="q-py-sm">
                <div class="text-h3">
                    <a :href="li.url" target="_blank">
                        {{ li.title }}
                    </a>
                </div>
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
    const search: Ref<string> = ref("")

    const links: Ref<Link[]> = ref([])
    const filteredLinks: Ref<Link[]> = ref([])
    const loaded: Ref<boolean> = ref(false)

    async function getLinkCollection() {
        const { get } = useFetch()
        const baseUrl = import.meta.env.VITE_API_URL + "cms/linkcollections/"
        const r = await get(baseUrl + "?linkCollectionName=" + encodeURIComponent(props.linkCollectionName))
        linkCollection.value = r.result[0] as LinkCollection
        if (linkCollection.value) {
            await loadLinks(linkCollection.value.linkCollectionId)
            await loadFilters()
        }
        loaded.value = true
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
        let allTags: LinkTag[] = []
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
        if (search.value.length) {
            filteredLinks.value = filteredLinks.value.filter((fl: Link) => {
                return fl.title.toLowerCase().includes(search.value)
                    || fl.description.toLowerCase().includes(search.value)
                    || fl.linkTags.some((t: LinkTag) => t.value.toLowerCase().includes(search.value))
            })
        }
    }

    function getLinkCollectionTagColor(order: number) {
        const colors = ["orange", "grey", "purple", "green", "blue"]
        return colors.length >= order ? colors[order - 1] : colors[colors.length - 1]
    }

    function openWebReports(url: string) {
        window.open(url, "_blank")?.focus()
    }

    watch(tagFilters, () => {
        applyFilters()
    }, {deep: true})
    watch(search, () => {
        applyFilters()
    })

    getLinkCollection()
</script>

<style scoped>
    .link-card {
        max-width: 350px;
        width: 100%;
    }

    .link-card a {
        text-decoration: none;
        color: inherit;
    }

    .link-tag {
        margin-right: 2px;
    }
</style>