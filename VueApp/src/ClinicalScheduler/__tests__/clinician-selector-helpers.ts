import { mount } from "@vue/test-utils"
import { setActivePinia, createPinia } from "pinia"
import { Quasar } from "quasar"

import ClinicianSelector from "../components/ClinicianSelector.vue"
import { ClinicianService } from "../services/clinician-service"

// Mock data
const mockClinicians = [
    {
        mothraId: "12345",
        fullName: "Smith, John",
        firstName: "John",
        lastName: "Smith",
    },
    {
        mothraId: "67890",
        fullName: "Doe, Jane",
        firstName: "Jane",
        lastName: "Doe",
    },
    {
        mothraId: "11111",
        fullName: "Johnson, Bob",
        firstName: "Bob",
        lastName: "Johnson",
    },
]

/**
 * Sets up Pinia and clears mocks for each test.
 */
function setupTest() {
    setActivePinia(createPinia())
    vi.clearAllMocks()

    // Default mock for getClinicians
    vi.mocked(ClinicianService.getClinicians).mockResolvedValue({
        success: true,
        result: mockClinicians,
        errors: [],
    })
}

/**
 * Mounts the ClinicianSelector component with given props.
 */
function createWrapper(props = {}): ReturnType<typeof mount> {
    return mount(ClinicianSelector, {
        props,
        global: {
            plugins: [[Quasar, {}]],
        },
    })
}

/**
 * Mocks an error response from ClinicianService.getClinicians.
 */
function mockErrorResponse(errorMessage: string) {
    vi.mocked(ClinicianService.getClinicians).mockResolvedValue({
        success: false,
        result: [],
        errors: [errorMessage],
    })
}

/**
 * Mocks a successful response from ClinicianService.getClinicians.
 */
function mockSuccessResponse(clinicians = mockClinicians) {
    vi.mocked(ClinicianService.getClinicians).mockResolvedValue({
        success: true,
        result: clinicians,
        errors: [],
    })
}

// Consolidated export
export { mockClinicians, setupTest, createWrapper, mockErrorResponse, mockSuccessResponse }
