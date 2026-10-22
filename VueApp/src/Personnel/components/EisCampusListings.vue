<template>
    <StatusBanner
        v-if="!available"
        type="warning"
    >
        The campus directory could not be reached. Try again later.
    </StatusBanner>
    <p v-else-if="listings.length === 0">No campus directory listing.</p>
    <div
        v-for="(listing, index) in listings"
        :key="index"
        class="q-mb-md"
    >
        <h4 class="q-mb-xs">
            {{ listing.isPrimary ? "Primary listing" : `Additional listing ${index}` }}
            <StatusBadge
                v-if="!listing.isPublic"
                color="warning"
                label="Not public"
                class="q-ml-sm"
            />
        </h4>
        <EisFactList :facts="listingFacts(listing)" />
    </div>
</template>

<script setup lang="ts">
import StatusBadge from "@/components/StatusBadge.vue"
import StatusBanner from "@/components/StatusBanner.vue"
import EisFactList from "./EisFactList.vue"
import { listingFacts } from "../services/eis-facts"
import type { EisCampusListing } from "../types/eis-types"

/** The campus directory listings; additional listings the directory doesn't publish are marked. */
defineProps<{ listings: EisCampusListing[]; available: boolean }>()
</script>
