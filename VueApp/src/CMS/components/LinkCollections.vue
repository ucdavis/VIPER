<template>
    <h2>{{ linkCollection?.linkCollection }}</h2>

    <q-form v-if="loaded">
        <div class="row q-pa-sm q-gutter-md">
            <q-input
                v-model="search"
                dense
                outlined
                label="Search"
                stack-label
                clearable
                class="col col-md-3 col-lg-2 q-ml-lg"
            >
                <template #append>
                    <q-icon name="search" />
                </template>
            </q-input>
            <template
                v-for="tf in tagFilters"
                :key="tf.linkCollectionTagCategoryId"
            >
                <q-select
                    v-model="tf.selected"
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
        </div>
    </q-form>

    <template v-if="props.groupByTagCategory == null || props.groupByTagCategory.length == 0">
        <div
            class="q-pa-md row q-gutter-md"
            v-if="linkCollection != null"
        >
            <template
                v-for="li in filteredLinks"
                :key="li.linkId"
            >
                <LinkComponent
                    :link="li"
                    :link-collection="linkCollection"
                ></LinkComponent>
            </template>
            <div
                v-if="filteredLinks.length == 0"
                class="text-subtitle2 q-pa-md"
            >
                No links found.
            </div>
        </div>
    </template>
    <template v-else-if="linkCollection != null">
        <div
            v-if="filteredLinks.length == 0"
            class="text-subtitle2 q-pa-md"
        >
            No links found.
        </div>
        <div
            class="q-pa-md row q-gutter-md"
            v-for="groupBy in groupByValues"
            :key="groupBy"
        >
            <div
                class="col-12"
                v-if="getInGroup(filteredLinks, groupBy).length > 0"
            >
                <h3 class="q-mt-lg q-mb-sm">{{ groupBy }}</h3>
            </div>
            <template
                v-for="li in getInGroup(filteredLinks, groupBy)"
                :key="li.linkId"
            >
                <LinkComponent
                    :link="li"
                    :link-collection="linkCollection"
                ></LinkComponent>
            </template>
        </div>
    </template>
</template>

<script setup lang="ts">
import type { Ref } from "vue"
import { ref, defineProps, watch } from "vue"
import type { Link, LinkCollection, LinkTag, LinkTagFilter } from "@/CMS/types"
import { useFetch } from "@/composables/ViperFetch"
import { default as LinkComponent } from "@/CMS/components/Link.vue"

const props = defineProps({
    linkCollectionName: {
        type: String,
        required: true,
    },
    groupByTagCategory: {
        type: String,
        required: false,
        default: null,
    },
})
const tagFilters: Ref<LinkTagFilter[]> = ref([])
const linkCollection: Ref<LinkCollection | null> = ref(null)
const search: Ref<string> = ref("")

const links: Ref<Link[]> = ref([])
const filteredLinks: Ref<Link[]> = ref([])
const loaded: Ref<boolean> = ref(false)

const groupByValues: Ref<string[]> = ref([])
const groupById: Ref<number | null> = ref(null)

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
    let baseUrl = import.meta.env.VITE_API_URL + "cms/linkCollections/" + collectionId + "/links"
    if (props.groupByTagCategory != null && props.groupByTagCategory.length > 0) {
        baseUrl += "?groupByTagCategory=" + encodeURIComponent(props.groupByTagCategory)
    }

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
                selected: null,
            })
        }
        for (var l of links.value) {
            for (var lt of l.linkTags) {
                var t = tagFilters.value.find((t) => t.linkCollectionTagCategoryId == lt.linkCollectionTagCategoryId)
                if (t) {
                    t.options.push(lt.value)
                }
            }
        }
        for (var tf of tagFilters.value) {
            tf.options = tf.options.sort()
        }

        for (var tagOptions of tagFilters.value) {
            tagOptions.options = [...new Set(tagOptions.options)]
            if (props.groupByTagCategory != null && props.groupByTagCategory == tagOptions.linkCollectionTagCategory) {
                groupByValues.value = tagOptions.options.sort()
                groupById.value = tagOptions.linkCollectionTagCategoryId
            }
        }
    }
}

function getInGroup(linksIn: Link[], groupByValue: string) {
    if (props.groupByTagCategory == null || props.groupByTagCategory.length == 0 || groupById.value == null) {
        return linksIn
    }
    return linksIn.filter((l) => {
        const findTag = l.linkTags.find((lt) => {
            return lt.linkCollectionTagCategoryId == groupById.value && lt.value == groupByValue
        })
        return findTag !== undefined
    })
}

function applyFilters() {
    filteredLinks.value = links.value
    for (var tf of tagFilters.value) {
        if (tf.selected != null && tf.selected != "") {
            filteredLinks.value = filteredLinks.value.filter((fl: Link) => {
                const findTag = fl.linkTags.find((lt: LinkTag) => {
                    return lt.linkCollectionTagCategoryId == tf.linkCollectionTagCategoryId && lt.value == tf.selected
                })
                return findTag !== undefined
            })
        }
    }
    if (search.value != null && search.value.length) {
        let sv = search.value.toLowerCase()
        filteredLinks.value = filteredLinks.value.filter((fl: Link) => {
            return (
                fl.title.toLowerCase().includes(sv) ||
                fl.description.toLowerCase().includes(sv) ||
                fl.linkTags.some((t: LinkTag) => t.value.toLowerCase().includes(sv))
            )
        })
    }
}

watch(
    tagFilters,
    () => {
        applyFilters()
    },
    { deep: true },
)
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
