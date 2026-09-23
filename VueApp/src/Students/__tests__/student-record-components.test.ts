import { mount, type GlobalMountOptions } from "@vue/test-utils"
import { Quasar } from "quasar"
import StudentEmail from "../components/StudentEmail.vue"
import StudentRecordLink from "../components/StudentRecordLink.vue"
import StudentRecordPageShell from "../components/StudentRecordPageShell.vue"

/**
 * Tests for the small shared pieces of a student record page: the email cell, the name link, and
 * the page frame the view and edit pages share.
 */

const quasar: GlobalMountOptions = { plugins: [[Quasar, {}]] }
const routerLinkStub = { props: ["to"], template: "<a :data-to='JSON.stringify(to)'><slot /></a>" }

function student(overrides = {}) {
    return { personId: 100, fullName: "Student, Test", hasDetailRoute: true, ...overrides }
}

describe("student email", () => {
    it("links an address for mailing", () => {
        expect.hasAssertions()
        const wrapper = mount(StudentEmail, { props: { email: "tstudent@ucdavis.edu" }, global: quasar })

        expect(wrapper.find("a").attributes("href")).toBe("mailto:tstudent@ucdavis.edu")
        expect(wrapper.text()).toBe("tstudent@ucdavis.edu")
    })

    it("renders nothing when the roster has no address", () => {
        expect.hasAssertions()
        expect(
            mount(StudentEmail, { props: { email: null }, global: quasar })
                .find("a")
                .exists(),
        ).toBeFalsy()
        expect(
            mount(StudentEmail, { props: { email: "" }, global: quasar })
                .find("a")
                .exists(),
        ).toBeFalsy()
    })
})

describe("student record link", () => {
    it("links an editor to the edit page", () => {
        expect.hasAssertions()
        const wrapper = mount(StudentRecordLink, {
            props: {
                student: student(),
                editRoute: "CareerSelectionEdit",
                viewRoute: "CareerSelectionView",
                canEdit: true,
            },
            global: { ...quasar, stubs: { RouterLink: routerLinkStub } },
        })

        expect(wrapper.find("a").attributes("data-to")).toContain("CareerSelectionEdit")
        expect(wrapper.find("a").attributes("aria-label")).toBe("Edit Student, Test")
    })

    it("links a reader to the read-only page", () => {
        expect.hasAssertions()
        const wrapper = mount(StudentRecordLink, {
            props: {
                student: student(),
                editRoute: "CareerSelectionEdit",
                viewRoute: "CareerSelectionView",
                canEdit: false,
            },
            global: { ...quasar, stubs: { RouterLink: routerLinkStub } },
        })

        expect(wrapper.find("a").attributes("data-to")).toContain("CareerSelectionView")
        expect(wrapper.find("a").attributes("aria-label")).toBe("View Student, Test")
    })

    it("explains an unlinked name rather than dropping the student", () => {
        expect.hasAssertions()
        // A student missing from AAUD has no PersonId to route on, but still belongs on the roster.
        const wrapper = mount(StudentRecordLink, {
            props: {
                student: student({ personId: 0, hasDetailRoute: false }),
                editRoute: "CareerSelectionEdit",
                viewRoute: "CareerSelectionView",
                canEdit: true,
            },
            global: { ...quasar, stubs: { RouterLink: routerLinkStub } },
        })

        expect(wrapper.find("a").exists()).toBeFalsy()
        expect(wrapper.text()).toContain("Student, Test")
        // Named by sr-only text rather than the icon: Quasar's q-icon is always aria-hidden.
        expect(wrapper.find(".sr-only").text()).toBe("No AAUD mapping, record cannot be opened")
        expect(wrapper.find(".sr-only").attributes("aria-hidden")).toBeUndefined()
    })
})

describe("student record page shell", () => {
    const shellProps = {
        appLabel: "Career Selection",
        listRouteName: "CareerSelectionList",
        recordLabel: "career selection",
    }

    function mountShell(props: { loading: boolean; detail: { fullName: string } | null; canViewList: boolean }) {
        return mount(StudentRecordPageShell, {
            props: { ...shellProps, ...props },
            slots: { default: "<p>Record body</p>" },
            global: {
                ...quasar,
                stubs: {
                    RouterLink: routerLinkStub,
                    StatusBanner: { template: "<div class='banner'><slot /></div>" },
                },
            },
        })
    }

    it("shows a spinner while the record loads", () => {
        expect.hasAssertions()
        const wrapper = mountShell({ loading: true, detail: null, canViewList: true })

        const spinner = wrapper.findComponent({ name: "QSpinner" })
        expect(spinner.exists()).toBeTruthy()
        expect(spinner.attributes("aria-label")).toBe("Loading career selection information")
        expect(wrapper.text()).not.toContain("Record body")
    })

    it("shows the record once it is in", () => {
        expect.hasAssertions()
        const wrapper = mountShell({ loading: false, detail: { fullName: "Student, Test" }, canViewList: true })

        expect(wrapper.text()).toContain("Record body")
        expect(wrapper.text()).toContain("Student, Test")
    })

    it("says so when there is no such record", () => {
        expect.hasAssertions()
        const wrapper = mountShell({ loading: false, detail: null, canViewList: false })

        expect(wrapper.find(".banner").text()).toBe("Student career selection record not found.")
        expect(wrapper.text()).not.toContain("Record body")
    })

    it("links the breadcrumb to the roster for a list viewer", () => {
        expect.hasAssertions()
        const wrapper = mountShell({ loading: false, detail: { fullName: "Student, Test" }, canViewList: true })

        const [appCrumb] = wrapper.findAllComponents({ name: "QBreadcrumbsEl" })
        expect(appCrumb.props("to")).toStrictEqual({ name: "CareerSelectionList" })
    })

    it("leaves the breadcrumb unlinked for a student", () => {
        expect.hasAssertions()
        // A student has a record but no roster to go back to.
        const wrapper = mountShell({ loading: false, detail: { fullName: "Student, Test" }, canViewList: false })

        const [appCrumb] = wrapper.findAllComponents({ name: "QBreadcrumbsEl" })
        expect(appCrumb.props("to")).toBeUndefined()
        expect(wrapper.text()).toContain("Career Selection")
    })
})
