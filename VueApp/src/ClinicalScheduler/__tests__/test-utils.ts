import { vi } from "vitest"
import { mount } from "@vue/test-utils"
import { createPinia, setActivePinia } from "pinia"
import { createRouter, createWebHistory } from "vue-router"
import { Quasar } from "quasar"
import { nextTick, computed } from "vue"
import { usePermissionsStore } from "../stores/permissions"
import { permissionService } from "../services/permission-service"
import ClinicalSchedulerHome from "../pages/ClinicalSchedulerHome.vue"
import type { UserPermissions } from "../types"

/**
 * Shared test utilities for Clinical Scheduler tests
 */

// Mock localStorage for tests that need it
const mockLocalStorage = {
    getItem: vi.fn(),
    setItem: vi.fn(),
    removeItem: vi.fn(),
    clear: vi.fn(),
}

// Setup localStorage mock
function setupLocalStorageMock() {
    Object.defineProperty(globalThis, "localStorage", {
        value: mockLocalStorage,
        writable: true,
    })
}

// Test router factory
function createTestRouter() {
    return createRouter({
        history: createWebHistory(),
        routes: [
            { path: "/", component: { template: "<div>Home</div>" } },
            { path: "/ClinicalScheduler/rotation", component: { template: "<div>Rotation</div>" } },
            { path: "/ClinicalScheduler/clinician", component: { template: "<div>Clinician</div>" } },
        ],
    })
}

// Pinia setup helper
function setupTestPinia() {
    setActivePinia(createPinia())
}

// Mock permissions store factory
function createMockPermissionsStore() {
    return {
        hasAnyEditPermission: false,
        hasFullAccessPermission: false,
        hasOnlyServiceSpecificPermissions: false,
        hasOnlyOwnSchedulePermission: false,
        canAccessClinicianView: false,
        canAccessRotationView: false,
        clinicianViewLabel: "Schedule by Clinician", // Default to generic label
        userPermissions: null,
        isLoading: false,
        editableServiceCount: 0,
        getEditableServicesDisplay: vi.fn().mockReturnValue("None"),
        initialize: vi.fn().mockResolvedValue(),
    }
}

// Mock component factories
const mockComponents = {
    AccessDeniedCard: {
        name: "AccessDeniedCard",
        props: ["message", "subtitle"],
        template: '<div class="no-access-card"><div>Access Denied</div><div>{{message}}</div></div>',
    },
}

// Note: Component mocks need to be done at the test file level, not in a function
// Each test file should include these mocks at the top level:
// vi.mock("../components/AccessDeniedCard.vue", () => ({ default: mockComponents.AccessDeniedCard }))

// Generic wrapper factory
interface WrapperOptions {
    component: any
    props?: Record<string, any>
    router?: any
    plugins?: any[]
}

function createTestWrapper({ component, props = {}, router, plugins = [] }: WrapperOptions) {
    const testRouter = router || createTestRouter()
    const defaultPlugins = [[Quasar, {}], testRouter, ...plugins]

    return mount(component, {
        props,
        global: {
            plugins: defaultPlugins,
        },
    })
}

// Async helper to replace new Promise pattern
async function waitForAsync() {
    await nextTick()
}

// Test setup helper that combines common setup steps
function setupTest() {
    setupTestPinia()
    vi.clearAllMocks()
    return {
        router: createTestRouter(),
        mockStore: createMockPermissionsStore(),
    }
}

// Helper to spy on router push
function spyOnRouterPush(router: any) {
    return vi.spyOn(router, "push")
}

// Mock UserPermissions factory
const createMockUserPermissions = (overrides: Partial<UserPermissions> = {}): UserPermissions => ({
    user: {
        mothraId: "test123",
        displayName: "Test User",
    },
    permissions: {
        hasAdminPermission: false,
        hasManagePermission: false,
        hasEditClnSchedulesPermission: false,
        hasEditOwnSchedulePermission: false,
        servicePermissions: {},
        editableServiceCount: 0,
    },
    editableServices: [],
    ...overrides,
})

// Export all utilities at the end
export {
    // Vue Test Utils
    mount,
    // Pinia utilities
    createPinia,
    setActivePinia,
    // Router utilities
    createRouter,
    createWebHistory,
    // Vue utilities
    nextTick,
    computed,
    // Quasar
    Quasar,
    // Components
    ClinicalSchedulerHome,
    // Stores
    usePermissionsStore,
    // Services
    permissionService,
    // Types
    type UserPermissions,
    // Test utilities
    mockLocalStorage,
    setupLocalStorageMock,
    createTestRouter,
    setupTestPinia,
    createMockPermissionsStore,
    createMockUserPermissions,
    mockComponents,
    createTestWrapper,
    waitForAsync,
    setupTest,
    spyOnRouterPush,
}
