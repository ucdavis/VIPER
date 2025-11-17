/* eslint-disable max-lines -- Comprehensive test suite requires extensive test coverage */
import { describe, it, expect, beforeEach, vi } from "vitest"
import { setActivePinia, createPinia } from "pinia"
import { usePhotoGalleryStore } from "../stores/photo-gallery-store"
import { photoGalleryService } from "../services/photo-gallery-service"
import type { PhotoGalleryViewModel, GalleryMenu, CourseInfo } from "../services/photo-gallery-service"

// Mock the photo gallery service
vi.mock("../services/photo-gallery-service", () => ({
    photoGalleryService: {
        getClassGallery: vi.fn(),
        getGroupGallery: vi.fn(),
        getGalleryMenu: vi.fn(),
        getAvailableCourses: vi.fn(),
        getCourseGallery: vi.fn(),
        exportToWord: vi.fn(),
        exportToPDF: vi.fn(),
        downloadFile: vi.fn(),
    },
}))

// eslint-disable-next-line max-lines-per-function -- Test suite requires comprehensive coverage with multiple test cases
describe("photo-gallery-store", () => {
    beforeEach(() => {
        // Create a fresh pinia instance for each test
        setActivePinia(createPinia())
        // Reset all mocks
        vi.clearAllMocks()
    })

    describe("initialization", () => {
        it("should initialize with correct default state", () => {
            const store = usePhotoGalleryStore()

            expect(store.students).toEqual([])
            expect(store.selectedClassLevel).toBeNull()
            expect(store.selectedGroup).toBeNull()
            expect(store.selectedCourse).toBeNull()
            expect(store.availableCourses).toEqual([])
            expect(store.includeRossStudents).toBeFalsy()
            expect(store.exportInProgress).toBeFalsy()
            expect(store.galleryView).toBe("grid")
            expect(store.galleryMenu).toBeNull()
            expect(store.loading).toBeFalsy()
            expect(store.error).toBeNull()
        })

        it("should initialize with default group types", () => {
            const store = usePhotoGalleryStore()

            expect(store.groupTypes.eighths).toEqual(["1A1", "1A2", "1B1", "1B2", "2A1", "2A2", "2B1", "2B2"])
            expect(store.groupTypes.twentieths).toEqual([])
            expect(store.groupTypes.teams).toEqual([])
            expect(store.groupTypes.v3specialty).toEqual([])
        })

        it("should initialize with empty group counts", () => {
            const store = usePhotoGalleryStore()

            expect(store.groupCounts.eighths).toEqual({})
            expect(store.groupCounts.twentieths).toEqual({})
            expect(store.groupCounts.teams).toEqual({})
            expect(store.groupCounts.v3specialty).toEqual({})
        })
    })

    describe("computed properties", () => {
        it("hasStudents should be false when students array is empty", () => {
            const store = usePhotoGalleryStore()

            expect(store.hasStudents).toBeFalsy()
        })

        it("hasStudents should be true when students array has items", () => {
            const store = usePhotoGalleryStore()
            store.students = [
                {
                    mailId: "test1",
                    firstName: "John",
                    lastName: "Doe",
                    displayName: "John Doe",
                    photoUrl: "/test.jpg",
                    groupAssignment: null,
                    eighthsGroup: "1A1",
                    twentiethsGroup: null,
                    teamNumber: null,
                    v3SpecialtyGroup: null,
                    classLevel: null,
                    hasPhoto: true,
                    isRossStudent: false,
                    fullName: "John Doe",
                    secondaryTextLines: [],
                },
            ]

            expect(store.hasStudents).toBeTruthy()
        })

        it("studentCount should match array length", () => {
            const store = usePhotoGalleryStore()

            expect(store.studentCount).toBe(0)

            store.students = [
                {
                    mailId: "test1",
                    firstName: "John",
                    lastName: "Doe",
                    displayName: "John Doe",
                    photoUrl: "/test.jpg",
                    groupAssignment: null,
                    eighthsGroup: null,
                    twentiethsGroup: null,
                    teamNumber: null,
                    v3SpecialtyGroup: null,
                    classLevel: null,
                    hasPhoto: true,
                    isRossStudent: false,
                    fullName: "John Doe",
                    secondaryTextLines: [],
                },
                {
                    mailId: "test2",
                    firstName: "Jane",
                    lastName: "Smith",
                    displayName: "Jane Smith",
                    photoUrl: "/test2.jpg",
                    groupAssignment: null,
                    eighthsGroup: null,
                    twentiethsGroup: null,
                    teamNumber: null,
                    v3SpecialtyGroup: null,
                    classLevel: null,
                    hasPhoto: true,
                    isRossStudent: false,
                    fullName: "Jane Smith",
                    secondaryTextLines: [],
                },
            ]

            expect(store.studentCount).toBe(2)
        })

        it("exportParams should build correct parameters for class", () => {
            const store = usePhotoGalleryStore()
            store.selectedClassLevel = "V4"

            expect(store.exportParams).toEqual({
                classLevel: "V4",
                groupType: undefined,
                groupId: undefined,
                termCode: undefined,
                crn: undefined,
                includeRossStudents: false,
            })
        })

        it("exportParams should build correct parameters for group", () => {
            const store = usePhotoGalleryStore()
            store.selectedGroup = {
                groupType: "eighths",
                groupId: "1A1",
                availableGroups: [],
            }

            expect(store.exportParams).toEqual({
                classLevel: undefined,
                groupType: "eighths",
                groupId: "1A1",
                termCode: undefined,
                crn: undefined,
                includeRossStudents: false,
            })
        })

        it("exportParams should build correct parameters for course", () => {
            const store = usePhotoGalleryStore()
            store.selectedCourse = {
                termCode: "202501",
                crn: "12345",
                subjectCode: "VMD",
                courseNumber: "101",
                title: "Test Course",
                termDescription: "Fall 2025",
            }

            expect(store.exportParams).toEqual({
                classLevel: undefined,
                groupType: undefined,
                groupId: undefined,
                termCode: "202501",
                crn: "12345",
                includeRossStudents: false,
            })
        })

        it("exportParams should include Ross students flag when enabled", () => {
            const store = usePhotoGalleryStore()
            store.selectedClassLevel = "V4"
            store.includeRossStudents = true

            expect(store.exportParams.includeRossStudents).toBeTruthy()
        })
    })

    describe("fetchClassPhotos", () => {
        it("should fetch class photos and update state", async () => {
            const store = usePhotoGalleryStore()
            const mockResponse: PhotoGalleryViewModel = {
                classLevel: "V4",
                students: [
                    {
                        mailId: "test1",
                        firstName: "John",
                        lastName: "Doe",
                        displayName: "John Doe",
                        photoUrl: "/test.jpg",
                        groupAssignment: null,
                        eighthsGroup: "1A1",
                        twentiethsGroup: null,
                        teamNumber: null,
                        v3SpecialtyGroup: null,
                        classLevel: null,
                        hasPhoto: true,
                        isRossStudent: false,
                        fullName: "John Doe",
                        secondaryTextLines: [],
                    },
                ],
            }

            vi.mocked(photoGalleryService.getClassGallery).mockResolvedValue(mockResponse)

            await store.fetchClassPhotos("V4")

            expect(photoGalleryService.getClassGallery).toHaveBeenCalledWith("V4", false)
            expect(store.selectedClassLevel).toBe("V4")
            expect(store.students).toEqual(mockResponse.students)
            expect(store.selectedGroup).toBeNull()
            expect(store.selectedCourse).toBeNull()
            expect(store.error).toBeNull()
        })

        it("should calculate group counts after loading class", async () => {
            const store = usePhotoGalleryStore()
            const mockResponse: PhotoGalleryViewModel = {
                classLevel: "V4",
                students: [
                    {
                        mailId: "test1",
                        firstName: "John",
                        lastName: "Doe",
                        displayName: "John Doe",
                        photoUrl: "/test.jpg",
                        groupAssignment: null,
                        eighthsGroup: "1A1",
                        twentiethsGroup: "T1",
                        teamNumber: null,
                        v3SpecialtyGroup: null,
                        classLevel: null,
                        hasPhoto: true,
                        isRossStudent: false,
                        fullName: "John Doe",
                        secondaryTextLines: [],
                    },
                    {
                        mailId: "test2",
                        firstName: "Jane",
                        lastName: "Smith",
                        displayName: "Jane Smith",
                        photoUrl: "/test2.jpg",
                        groupAssignment: null,
                        eighthsGroup: "1A1",
                        twentiethsGroup: "T2",
                        teamNumber: null,
                        v3SpecialtyGroup: null,
                        classLevel: null,
                        hasPhoto: true,
                        isRossStudent: false,
                        fullName: "Jane Smith",
                        secondaryTextLines: [],
                    },
                ],
            }

            vi.mocked(photoGalleryService.getClassGallery).mockResolvedValue(mockResponse)

            await store.fetchClassPhotos("V4")

            expect(store.groupCounts.eighths["1A1"]).toBe(2)
            expect(store.groupCounts.twentieths["T1"]).toBe(1)
            expect(store.groupCounts.twentieths["T2"]).toBe(1)
        })

        it("should include Ross students when flag is enabled", async () => {
            const store = usePhotoGalleryStore()
            store.includeRossStudents = true
            const mockResponse: PhotoGalleryViewModel = {
                classLevel: "V4",
                students: [],
            }

            vi.mocked(photoGalleryService.getClassGallery).mockResolvedValue(mockResponse)

            await store.fetchClassPhotos("V4")

            expect(photoGalleryService.getClassGallery).toHaveBeenCalledWith("V4", true)
        })

        it("should handle API errors gracefully", async () => {
            const store = usePhotoGalleryStore()
            const errorMessage = "Failed to fetch class photos"

            vi.mocked(photoGalleryService.getClassGallery).mockRejectedValue(new Error(errorMessage))

            await store.fetchClassPhotos("V4")

            expect(store.error).toBe(errorMessage)
            expect(store.loading).toBeFalsy()
        })

        it("should set loading state correctly", async () => {
            const store = usePhotoGalleryStore()
            const mockResponse: PhotoGalleryViewModel = {
                classLevel: "V4",
                students: [],
            }

            vi.mocked(photoGalleryService.getClassGallery).mockResolvedValue(mockResponse)

            const fetchPromise = store.fetchClassPhotos("V4")
            expect(store.loading).toBeTruthy()

            await fetchPromise
            expect(store.loading).toBeFalsy()
        })
    })

    describe("fetchCoursePhotos", () => {
        it("should fetch course photos and update state", async () => {
            const store = usePhotoGalleryStore()
            const mockResponse: PhotoGalleryViewModel = {
                students: [],
                courseInfo: {
                    termCode: "202501",
                    crn: "12345",
                    subjectCode: "VMD",
                    courseNumber: "101",
                    title: "Test Course",
                    termDescription: "Fall 2025",
                },
            }

            vi.mocked(photoGalleryService.getCourseGallery).mockResolvedValue(mockResponse)

            await store.fetchCoursePhotos("202501", "12345")

            expect(photoGalleryService.getCourseGallery).toHaveBeenCalledWith("202501", "12345", {
                includeRossStudents: false,
            })
            expect(store.selectedCourse).toEqual(mockResponse.courseInfo)
            expect(store.selectedClassLevel).toBeNull()
            expect(store.selectedGroup).toBeNull()
        })

        it("should clear class level when loading course", async () => {
            const store = usePhotoGalleryStore()
            store.selectedClassLevel = "V4"

            const mockResponse: PhotoGalleryViewModel = {
                students: [],
                courseInfo: {
                    termCode: "202501",
                    crn: "12345",
                    subjectCode: "VMD",
                    courseNumber: "101",
                    title: "Test Course",
                    termDescription: "Fall 2025",
                },
            }

            vi.mocked(photoGalleryService.getCourseGallery).mockResolvedValue(mockResponse)

            await store.fetchCoursePhotos("202501", "12345")

            expect(store.selectedClassLevel).toBeNull()
        })

        it("should calculate group counts for course students", async () => {
            const store = usePhotoGalleryStore()
            const mockResponse: PhotoGalleryViewModel = {
                students: [
                    {
                        mailId: "test1",
                        firstName: "John",
                        lastName: "Doe",
                        displayName: "John Doe",
                        photoUrl: "/test.jpg",
                        groupAssignment: null,
                        eighthsGroup: "1A1",
                        twentiethsGroup: null,
                        teamNumber: "Team 1",
                        v3SpecialtyGroup: "Companion Animal",
                        classLevel: null,
                        hasPhoto: true,
                        isRossStudent: false,
                        fullName: "John Doe",
                        secondaryTextLines: [],
                    },
                ],
                courseInfo: {
                    termCode: "202501",
                    crn: "12345",
                    subjectCode: "VMD",
                    courseNumber: "101",
                    title: "Test Course",
                    termDescription: "Fall 2025",
                },
            }

            vi.mocked(photoGalleryService.getCourseGallery).mockResolvedValue(mockResponse)

            await store.fetchCoursePhotos("202501", "12345")

            expect(store.groupCounts.eighths["1A1"]).toBe(1)
            expect(store.groupCounts.teams["Team 1"]).toBe(1)
            expect(store.groupCounts.v3specialty["Companion Animal"]).toBe(1)
        })
    })

    describe("fetchGroupPhotos", () => {
        it("should fetch group photos and update state", async () => {
            const store = usePhotoGalleryStore()
            const mockResponse: PhotoGalleryViewModel = {
                students: [],
                groupInfo: {
                    groupType: "eighths",
                    groupId: "1A1",
                    availableGroups: ["1A1", "1A2"],
                },
            }

            vi.mocked(photoGalleryService.getGroupGallery).mockResolvedValue(mockResponse)

            await store.fetchGroupPhotos("eighths", "1A1")

            expect(photoGalleryService.getGroupGallery).toHaveBeenCalledWith("eighths", "1A1", undefined)
            expect(store.selectedGroup).toEqual(mockResponse.groupInfo)
        })

        it("should pass class level parameter when provided", async () => {
            const store = usePhotoGalleryStore()
            const mockResponse: PhotoGalleryViewModel = {
                students: [],
                groupInfo: {
                    groupType: "eighths",
                    groupId: "1A1",
                    availableGroups: [],
                },
            }

            vi.mocked(photoGalleryService.getGroupGallery).mockResolvedValue(mockResponse)

            await store.fetchGroupPhotos("eighths", "1A1", "V4")

            expect(photoGalleryService.getGroupGallery).toHaveBeenCalledWith("eighths", "1A1", "V4")
        })
    })

    describe("fetchGalleryMenu", () => {
        it("should fetch gallery menu and update group types", async () => {
            const store = usePhotoGalleryStore()
            const mockMenu: GalleryMenu = {
                classLevels: ["V4", "V3"],
                groupTypes: [
                    { type: "eighths", label: "Eighths", groups: ["1A1", "1A2"] },
                    { type: "twentieths", label: "Twentieths", groups: ["T1", "T2"] },
                    { type: "teams", label: "Teams", groups: ["Team 1", "Team 2"] },
                    { type: "v3specialty", label: "Streams", groups: ["Companion", "Equine"] },
                ],
            }

            vi.mocked(photoGalleryService.getGalleryMenu).mockResolvedValue(mockMenu)

            await store.fetchGalleryMenu()

            expect(store.galleryMenu).toEqual(mockMenu)
            expect(store.groupTypes.eighths).toEqual(["1A1", "1A2"])
            expect(store.groupTypes.twentieths).toEqual(["T1", "T2"])
            expect(store.groupTypes.teams).toEqual(["Team 1", "Team 2"])
            expect(store.groupTypes.v3specialty).toEqual(["Companion", "Equine"])
        })
    })

    describe("fetchAvailableCourses", () => {
        it("should fetch available courses", async () => {
            const store = usePhotoGalleryStore()
            const mockCourses: CourseInfo[] = [
                {
                    termCode: "202501",
                    crn: "12345",
                    subjectCode: "VMD",
                    courseNumber: "101",
                    title: "Test Course",
                    termDescription: "Fall 2025",
                },
            ]

            vi.mocked(photoGalleryService.getAvailableCourses).mockResolvedValue(mockCourses)

            await store.fetchAvailableCourses()

            expect(store.availableCourses).toEqual(mockCourses)
        })
    })

    describe("export functionality", () => {
        it("should export to Word with correct parameters", async () => {
            const store = usePhotoGalleryStore()
            store.selectedClassLevel = "V4"
            const mockBlob = new Blob(["test"], {
                type: "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            })

            vi.mocked(photoGalleryService.exportToWord).mockResolvedValue(mockBlob)
            vi.mocked(photoGalleryService.downloadFile).mockImplementation(() => {
                // Mock implementation - no action needed
            })

            await store.exportToWord()

            expect(photoGalleryService.exportToWord).toHaveBeenCalledWith(
                expect.objectContaining({
                    classLevel: "V4",
                    exportFormat: "word",
                }),
            )
            expect(photoGalleryService.downloadFile).toHaveBeenCalledWith(
                mockBlob,
                expect.stringMatching(/^StudentPhotos_\d{4}-\d{2}-\d{2}\.docx$/),
            )
        })

        it("should export to PDF with correct parameters", async () => {
            const store = usePhotoGalleryStore()
            store.selectedClassLevel = "V4"
            const mockBlob = new Blob(["test"], { type: "application/pdf" })

            vi.mocked(photoGalleryService.exportToPDF).mockResolvedValue(mockBlob)
            vi.mocked(photoGalleryService.downloadFile).mockImplementation(() => {
                // Mock implementation - no action needed
            })

            await store.exportToPDF()

            expect(photoGalleryService.exportToPDF).toHaveBeenCalledWith(
                expect.objectContaining({
                    classLevel: "V4",
                    exportFormat: "pdf",
                }),
            )
            expect(photoGalleryService.downloadFile).toHaveBeenCalledWith(
                mockBlob,
                expect.stringMatching(/^StudentPhotos_\d{4}-\d{2}-\d{2}\.pdf$/),
            )
        })

        it("should set exportInProgress flag during export", async () => {
            const store = usePhotoGalleryStore()
            const mockBlob = new Blob(["test"])

            vi.mocked(photoGalleryService.exportToWord).mockResolvedValue(mockBlob)
            vi.mocked(photoGalleryService.downloadFile).mockImplementation(() => {
                // Mock implementation - no action needed
            })

            const exportPromise = store.exportToWord()
            expect(store.exportInProgress).toBeTruthy()

            await exportPromise
            expect(store.exportInProgress).toBeFalsy()
        })
    })

    describe("utility functions", () => {
        it("setGalleryView should update gallery view", () => {
            const store = usePhotoGalleryStore()

            store.setGalleryView("sheet")
            expect(store.galleryView).toBe("sheet")

            store.setGalleryView("list")
            expect(store.galleryView).toBe("list")
        })

        it("toggleRossStudents should refetch class when class is selected", async () => {
            const store = usePhotoGalleryStore()
            store.selectedClassLevel = "V4"

            const mockResponse: PhotoGalleryViewModel = {
                classLevel: "V4",
                students: [],
            }
            vi.mocked(photoGalleryService.getClassGallery).mockResolvedValue(mockResponse)

            await store.toggleRossStudents()

            expect(photoGalleryService.getClassGallery).toHaveBeenCalled()
        })

        it("clearSelection should reset all selections", () => {
            const store = usePhotoGalleryStore()
            store.students = [
                {
                    mailId: "test1",
                    firstName: "John",
                    lastName: "Doe",
                    displayName: "John Doe",
                    photoUrl: "/test.jpg",
                    groupAssignment: null,
                    eighthsGroup: null,
                    twentiethsGroup: null,
                    teamNumber: null,
                    v3SpecialtyGroup: null,
                    classLevel: null,
                    hasPhoto: true,
                    isRossStudent: false,
                    fullName: "John Doe",
                    secondaryTextLines: [],
                },
            ]
            store.selectedClassLevel = "V4"
            store.selectedGroup = { groupType: "eighths", groupId: "1A1", availableGroups: [] }
            store.error = "Some error"

            store.clearSelection()

            expect(store.students).toEqual([])
            expect(store.selectedClassLevel).toBeNull()
            expect(store.selectedGroup).toBeNull()
            expect(store.selectedCourse).toBeNull()
            expect(store.error).toBeNull()
        })
    })
})
