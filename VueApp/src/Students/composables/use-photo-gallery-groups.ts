import { computed } from "vue"
import type { Ref } from "vue"
import { usePhotoGalleryStore } from "../stores/photo-gallery-store"

/**
 * Composable for managing photo gallery group options
 * Extracts group-related logic to reduce component complexity
 */
export function usePhotoGalleryGroups(selectedClassLevel: Ref<string | null>, selectedGroupType: Ref<string | null>) {
    const galleryStore = usePhotoGalleryStore()

    const groupTypeOptions = computed(() => [
        { label: "All Students", value: null },
        { label: "Eighths", value: "eighths" },
        { label: "Twentieths", value: "twentieths" },
        ...(selectedClassLevel.value === "V3"
            ? [
                  { label: "Teams", value: "teams" },
                  { label: "Streams", value: "v3specialty" },
              ]
            : []),
    ])

    const groupOptions = computed(() => {
        if (!selectedGroupType.value) {
            return []
        }

        let groups: string[] = []
        let countKey: "eighths" | "twentieths" | "teams" | "v3specialty" = "eighths"

        if (selectedGroupType.value === "eighths") {
            groups = galleryStore.groupTypes.eighths
            countKey = "eighths"
        } else if (selectedGroupType.value === "twentieths") {
            groups = galleryStore.groupTypes.twentieths
            countKey = "twentieths"
        } else if (selectedGroupType.value === "teams") {
            groups = galleryStore.groupTypes.teams
            countKey = "teams"
        } else if (selectedGroupType.value === "v3specialty") {
            groups = galleryStore.groupTypes.v3specialty
            countKey = "v3specialty"
        }

        // Use cached counts from the store (populated when class is loaded)
        const counts = galleryStore.groupCounts[countKey] || {}

        return groups.map((group) => {
            const count = counts[group] || 0
            return {
                label: `${group} (${count})`,
                value: group,
            }
        })
    })

    const groupTypeLabel = computed(() => {
        if (!selectedGroupType.value) {
            return ""
        }
        const labels: Record<string, string> = {
            eighths: "Eighths",
            twentieths: "Twentieths",
            teams: "Team",
            v3specialty: "Stream",
        }
        return labels[selectedGroupType.value] || selectedGroupType.value
    })

    return {
        groupTypeOptions,
        groupOptions,
        groupTypeLabel,
    }
}
