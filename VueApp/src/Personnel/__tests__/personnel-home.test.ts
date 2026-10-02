import { mount } from "@vue/test-utils"
import { createMemoryHistory, createRouter } from "vue-router"
import Home from "../pages/Home.vue"
import { routes } from "../router/routes"

/**
 * The Personnel home page shows its CMS content and the Personnel report catalog, whose links
 * go to the PersonnelReport route.
 */

describe("personnel home page", () => {
    it("shows the CMS content and the Personnel reports", () => {
        const wrapper = mount(Home, {
            global: { stubs: { CmsContent: true, ReportCatalog: true } },
        })

        expect(wrapper.findComponent({ name: "CmsContent" }).props("contentName")).toBe("personnel-home")
        const catalog = wrapper.findComponent({ name: "ReportCatalog" })
        expect(catalog.props("area")).toBe("Personnel")
        expect(catalog.props("reportRoute")).toBe("PersonnelReport")
    })
})

describe("personnel report route", () => {
    it("resolves a report key to the PersonnelReport route", () => {
        const router = createRouter({ history: createMemoryHistory(), routes })
        const resolved = router.resolve({
            name: "PersonnelReport",
            params: { key: "personnel.faculty-profile" },
        })

        expect(resolved.path).toBe("/Personnel/Reports/personnel.faculty-profile")
        expect(resolved.meta.allowUnAuth).toBeFalsy()
    })
})
