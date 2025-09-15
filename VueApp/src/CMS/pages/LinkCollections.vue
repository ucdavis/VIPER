<template>
    <h2>Manage Link Collections</h2>
    <q-form @submit="submitLinkCollection" v-bind="linkCollection">

    </q-form>

    <q-form>
        <div class="row q-pa-sm q-gutter-md">
        <template v-for="af in attributeFilters">
            <q-select v-model="af.selected" 
                      dense options-dense
                      :options="af.options" 
                      :label="af.linkCollectionAttribute" 
                      stack-label 
                      clearable
                      class="col col-md-3 col-lg-2 col-xl-1"
                      ></q-select>
        </template>
        </div>
    </q-form>

    <div class="q-pa-md row items-start q-gutter-md">
        <q-card v-for="li in links" :bordered=true :href="li.url" class="link-card">
            <q-card-section>
                <div class="text-h3">{{ li.title }}</div>
            </q-card-section>
            <q-card-section>
                {{ li.description }}
            </q-card-section>
            <q-card-section>
                <template v-for="lca in linkCollectionAttributes">
                    <template v-for="att in li.attributes">
                        <q-badge v-if="att.linkCollectionAttributeId == lca.linkCollectionAttributeId" :color="getLinkCollectionAttributeColor(lca.sortOrder)"
                                 class="q-mx-sm">
                            {{ att.values }}
                        </q-badge>
                    </template>
                </template>
            </q-card-section>
        </q-card>
    </div>
</template>

<script setup lang="ts">
    import type { Ref } from 'vue'
    import { ref, onMounted }  from 'vue'
    type LinkCollection = {
        linkCollectionId: number,
        linkCollection: string,
        attributes: LinkCollectionAttribute[]
    }

    type LinkCollectionAttribute = {
        linkCollectionAttributeId: number,
        linkCollectionAttribute: string,
        sortOrder: number
    }

    type Link = {
        linkId: number,
        url: string,
        title: string,
        description: string,
        attributes: LinkAttribute[],
        order: number
    }

    type LinkAttribute = {
        linkAttributeId: number,
        linkId: number,
        linkCollectionAttributeId: number,
        order: number,
        values: string
    }

    type LinkAttributeFilter = {
        linkCollectionAttributeId: number,
        linkCollectionAttribute: string,
        options: string[],
        selected: string | null
    }

    const attributeFilters: Ref<LinkAttributeFilter[]> = ref([])
    const linkCollection: Ref<LinkCollection | null > = ref(null)
    const linkCollectionAttributes: Ref<LinkCollectionAttribute[]> = ref([
        { linkCollectionAttributeId: 1, linkCollectionAttribute: "Section", sortOrder: 1 },
        { linkCollectionAttributeId: 2, linkCollectionAttribute: "Report Type", sortOrder: 2 },
        { linkCollectionAttributeId: 3, linkCollectionAttribute: "Database", sortOrder: 3 },
    ])

    const links: Ref<Link[]> = ref([
        {
            linkId: 1, url: "http://", title: "Bill to Audit Sheet", description: "First part lists any accession that has more than one Bill To Role. Second part lists any accession that does not have a Bill To Role assigned order the default Bill to box checked.", order: 1,
            attributes: [
                { linkAttributeId: 1, linkId: 1, linkCollectionAttributeId: 1, order: 1, values: "Admin" },
                { linkAttributeId: 2, linkId: 1, linkCollectionAttributeId: 2, order: 2, values: "Tally" },
                { linkAttributeId: 3, linkId: 1, linkCollectionAttributeId: 2, order: 3, values: "Financial" },
                { linkAttributeId: 4, linkId: 1, linkCollectionAttributeId: 3, order: 4, values: "GP" },
                { linkAttributeId: 5, linkId: 1, linkCollectionAttributeId: 3, order: 4, values: "LIMS" }
            ]
        },
        {
            linkId: 2, url: "http://", title: "Test Tally & Billed Report", order: 2, description: "Shows number of tests billed and number of tests performed for specified date range. These will no always match since we don't always bill the same date the test was performed.",
            attributes: [
                { linkAttributeId: 6, linkId: 2, linkCollectionAttributeId: 1, order: 1, values: "Admin" },
                { linkAttributeId: 7, linkId: 2, linkCollectionAttributeId: 2, order: 2, values: "Tally" },
                { linkAttributeId: 8, linkId: 2, linkCollectionAttributeId: 2, order: 3, values: "Financial" },
                { linkAttributeId: 9, linkId: 2, linkCollectionAttributeId: 3, order: 4, values: "GP" },
                { linkAttributeId: 10, linkId: 2, linkCollectionAttributeId: 3, order: 5, values: "LIMS" },
            ]
        }
    ])

    function submitLinkCollection() { }
    function getLinkCollectionAttributeColor(order: number) {
        const colors = ["orange", "grey", "purple", "green", "blue"]
        return colors.length >= order ? colors[order - 1] : colors[colors.length - 1]
    }

    onMounted(() => {
        for (var lca of linkCollectionAttributes.value) {
            attributeFilters.value.push({
                linkCollectionAttributeId: lca.linkCollectionAttributeId,
                linkCollectionAttribute: lca.linkCollectionAttribute,
                options: [],
                selected: null
            });
        }
        for (var l of links.value) {
            for (var a of l.attributes) {
                var af = attributeFilters.value.find(af => af.linkCollectionAttributeId == a.linkCollectionAttributeId)
                if (af) {
                    af.options.push(a.values)
                }
            }
        }

        for (var afoptions of attributeFilters.value) {
            afoptions.options = [...new Set(afoptions.options)]
        }
    })
</script>

<style scoped>
    .link-card {
        max-width: 350px;
        width: 100%;
    }
</style>