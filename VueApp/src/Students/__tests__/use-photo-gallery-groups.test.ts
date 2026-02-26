import { ref } from "vue"
import { setActivePinia, createPinia } from "pinia"
import { usePhotoGalleryGroups } from "../composables/use-photo-gallery-groups"
import { usePhotoGalleryStore } from "../stores/photo-gallery-store"

describe(usePhotoGalleryGroups, () => {
    beforeEach(() => {
        // Create a fresh pinia instance for each test
        setActivePinia(createPinia())
    })

    describe("groupTypeOptions", () => {
        it("should show basic options for non-V3 class levels", () => {
            const selectedClassLevel = ref("V4")
            const selectedGroupType = ref<string | null>(null)

            const { groupTypeOptions } = usePhotoGalleryGroups(selectedClassLevel, selectedGroupType)

            expect(groupTypeOptions.value).toEqual([
                { label: "All Students", value: null },
                { label: "Eighths", value: "eighths" },
                { label: "Twentieths", value: "twentieths" },
            ])
        })

        it("should show V3-specific options (Teams, Streams) for V3 class level", () => {
            const selectedClassLevel = ref("V3")
            const selectedGroupType = ref<string | null>(null)

            const { groupTypeOptions } = usePhotoGalleryGroups(selectedClassLevel, selectedGroupType)

            expect(groupTypeOptions.value).toEqual([
                { label: "All Students", value: null },
                { label: "Eighths", value: "eighths" },
                { label: "Twentieths", value: "twentieths" },
                { label: "Teams", value: "teams" },
                { label: "Streams", value: "v3specialty" },
            ])
        })

        it("should hide V3 options when class level changes from V3 to V4", () => {
            const selectedClassLevel = ref("V3")
            const selectedGroupType = ref<string | null>(null)

            const { groupTypeOptions } = usePhotoGalleryGroups(selectedClassLevel, selectedGroupType)

            expect(groupTypeOptions.value).toHaveLength(5)

            // Change to V4
            selectedClassLevel.value = "V4"

            expect(groupTypeOptions.value).toHaveLength(3)
            expect(groupTypeOptions.value.some((opt) => opt.value === "teams")).toBeFalsy()
            expect(groupTypeOptions.value.some((opt) => opt.value === "v3specialty")).toBeFalsy()
        })

        it("should handle null class level", () => {
            const selectedClassLevel = ref<string | null>(null)
            const selectedGroupType = ref<string | null>(null)

            const { groupTypeOptions } = usePhotoGalleryGroups(selectedClassLevel, selectedGroupType)

            expect(groupTypeOptions.value).toEqual([
                { label: "All Students", value: null },
                { label: "Eighths", value: "eighths" },
                { label: "Twentieths", value: "twentieths" },
            ])
        })
    })

    describe("groupOptions", () => {
        it("should return empty array when no group type selected", () => {
            const selectedClassLevel = ref("V4")
            const selectedGroupType = ref<string | null>(null)

            const { groupOptions } = usePhotoGalleryGroups(selectedClassLevel, selectedGroupType)

            expect(groupOptions.value).toEqual([])
        })

        it("should return eighths groups when eighths selected", () => {
            const selectedClassLevel = ref("V4")
            const selectedGroupType = ref<string | null>("eighths")
            const store = usePhotoGalleryStore()

            // Set up store with group counts
            store.groupCounts.eighths = { "1A1": 25, "1A2": 30 }

            const { groupOptions } = usePhotoGalleryGroups(selectedClassLevel, selectedGroupType)

            expect(groupOptions.value).toEqual([
                { label: "1A1 (25)", value: "1A1" },
                { label: "1A2 (30)", value: "1A2" },
                { label: "1B1 (0)", value: "1B1" },
                { label: "1B2 (0)", value: "1B2" },
                { label: "2A1 (0)", value: "2A1" },
                { label: "2A2 (0)", value: "2A2" },
                { label: "2B1 (0)", value: "2B1" },
                { label: "2B2 (0)", value: "2B2" },
            ])
        })

        it("should return twentieths groups when twentieths selected", () => {
            const selectedClassLevel = ref("V4")
            const selectedGroupType = ref<string | null>("twentieths")
            const store = usePhotoGalleryStore()

            // Set up test twentieths data
            store.groupTypes.twentieths = ["T1", "T2", "T3"]
            store.groupCounts.twentieths = { T1: 10, T2: 15, T3: 12 }

            const { groupOptions } = usePhotoGalleryGroups(selectedClassLevel, selectedGroupType)

            expect(groupOptions.value).toEqual([
                { label: "T1 (10)", value: "T1" },
                { label: "T2 (15)", value: "T2" },
                { label: "T3 (12)", value: "T3" },
            ])
        })

        it("should return teams groups when teams selected", () => {
            const selectedClassLevel = ref("V3")
            const selectedGroupType = ref<string | null>("teams")
            const store = usePhotoGalleryStore()

            // Set up test teams data
            store.groupTypes.teams = ["Team 1", "Team 2"]
            store.groupCounts.teams = { "Team 1": 8, "Team 2": 9 }

            const { groupOptions } = usePhotoGalleryGroups(selectedClassLevel, selectedGroupType)

            expect(groupOptions.value).toEqual([
                { label: "Team 1 (8)", value: "Team 1" },
                { label: "Team 2 (9)", value: "Team 2" },
            ])
        })

        it("should return v3specialty groups when v3specialty selected", () => {
            const selectedClassLevel = ref("V3")
            const selectedGroupType = ref<string | null>("v3specialty")
            const store = usePhotoGalleryStore()

            // Set up test v3specialty data
            store.groupTypes.v3specialty = ["Companion Animal", "Equine"]
            store.groupCounts.v3specialty = { "Companion Animal": 20, Equine: 15 }

            const { groupOptions } = usePhotoGalleryGroups(selectedClassLevel, selectedGroupType)

            expect(groupOptions.value).toEqual([
                { label: "Companion Animal (20)", value: "Companion Animal" },
                { label: "Equine (15)", value: "Equine" },
            ])
        })

        it("should handle missing counts gracefully (show 0)", () => {
            const selectedClassLevel = ref("V4")
            const selectedGroupType = ref<string | null>("eighths")
            const store = usePhotoGalleryStore()

            // Set up store with no counts
            store.groupCounts.eighths = {}

            const { groupOptions } = usePhotoGalleryGroups(selectedClassLevel, selectedGroupType)

            expect(groupOptions.value[0].label).toBe("1A1 (0)")
            expect(groupOptions.value[1].label).toBe("1A2 (0)")
        })

        it("should update when group type changes", () => {
            const selectedClassLevel = ref("V4")
            const selectedGroupType = ref<string | null>("eighths")
            const store = usePhotoGalleryStore()

            store.groupTypes.twentieths = ["T1"]
            store.groupCounts.eighths = { "1A1": 25 }
            store.groupCounts.twentieths = { T1: 10 }

            const { groupOptions } = usePhotoGalleryGroups(selectedClassLevel, selectedGroupType)

            expect(groupOptions.value).toHaveLength(8) // Eighths

            // Change to twentieths
            selectedGroupType.value = "twentieths"

            expect(groupOptions.value).toHaveLength(1) // Twentieths
            expect(groupOptions.value[0].value).toBe("T1")
        })
    })

    describe("groupTypeLabel", () => {
        it("should return empty string when no group type selected", () => {
            const selectedClassLevel = ref("V4")
            const selectedGroupType = ref<string | null>(null)

            const { groupTypeLabel } = usePhotoGalleryGroups(selectedClassLevel, selectedGroupType)

            expect(groupTypeLabel.value).toBe("")
        })

        it("should return Eighths for eighths group type", () => {
            const selectedClassLevel = ref("V4")
            const selectedGroupType = ref<string | null>("eighths")

            const { groupTypeLabel } = usePhotoGalleryGroups(selectedClassLevel, selectedGroupType)

            expect(groupTypeLabel.value).toBe("Eighths")
        })

        it("should return Twentieths for twentieths group type", () => {
            const selectedClassLevel = ref("V4")
            const selectedGroupType = ref<string | null>("twentieths")

            const { groupTypeLabel } = usePhotoGalleryGroups(selectedClassLevel, selectedGroupType)

            expect(groupTypeLabel.value).toBe("Twentieths")
        })

        it("should return Team for teams group type", () => {
            const selectedClassLevel = ref("V3")
            const selectedGroupType = ref<string | null>("teams")

            const { groupTypeLabel } = usePhotoGalleryGroups(selectedClassLevel, selectedGroupType)

            expect(groupTypeLabel.value).toBe("Team")
        })

        it("should return Stream for v3specialty group type", () => {
            const selectedClassLevel = ref("V3")
            const selectedGroupType = ref<string | null>("v3specialty")

            const { groupTypeLabel } = usePhotoGalleryGroups(selectedClassLevel, selectedGroupType)

            expect(groupTypeLabel.value).toBe("Stream")
        })

        it("should return the group type itself if no label mapping exists", () => {
            const selectedClassLevel = ref("V4")
            const selectedGroupType = ref<string | null>("unknown")

            const { groupTypeLabel } = usePhotoGalleryGroups(selectedClassLevel, selectedGroupType)

            expect(groupTypeLabel.value).toBe("unknown")
        })
    })
})
