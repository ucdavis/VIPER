import { mount } from "@vue/test-utils"
import type { GlobalMountOptions } from "@vue/test-utils"
import { Quasar } from "quasar"
import CareerRecordLink from "../components/CareerRecordLink.vue"
import CareerSelectionRowCard from "../components/CareerSelectionRowCard.vue"
import MentorSelector from "../components/MentorSelector.vue"
import StudentRecordLink from "@/Students/components/StudentRecordLink.vue"
import { careerSelectionService } from "../services/career-selection-service"

/**
 * Tests for the career selection roster's building blocks: the name link, the narrow-screen card,
 * and the admin-only mentor picker.
 */

const quasar: GlobalMountOptions = { plugins: [[Quasar, {}]] }

function student(overrides = {}) {
    return { personId: 100, fullName: "Student, Test", hasDetailRoute: true, classLevel: "V1", ...overrides }
}

describe("career record link", () => {
    function mountLink(canEdit: boolean, emphasized = false) {
        return mount(CareerRecordLink, {
            props: { student: student(), canEdit, emphasized },
            global: { ...quasar, stubs: { StudentRecordLink: true } },
        })
    }

    it("routes an editor to the career selection edit page", () => {
        expect.hasAssertions()
        const link = mountLink(true).findComponent(StudentRecordLink)

        expect(link.props("editRoute")).toBe("CareerSelectionEdit")
        expect(link.props("viewRoute")).toBe("CareerSelectionView")
        expect(link.props("canEdit")).toBeTruthy()
    })

    it("passes the reader's own flag through", () => {
        expect.hasAssertions()
        expect(mountLink(false).findComponent(StudentRecordLink).props("canEdit")).toBeFalsy()
    })

    it("passes the card's emphasis through", () => {
        expect.hasAssertions()
        expect(mountLink(true, true).findComponent(StudentRecordLink).props("emphasized")).toBeTruthy()
    })
})

describe("career selection row card", () => {
    function mountCard(overrides = {}, visibleColumns?: string[]) {
        return mount(CareerSelectionRowCard, {
            props: { student: student(overrides), canEdit: true, visibleColumns },
            slots: { default: "<p>Field lines</p>" },
            global: {
                ...quasar,
                stubs: { CareerRecordLink: { template: "<span>{{ student.fullName }}</span>", props: ["student"] } },
            },
        })
    }

    it("heads the card with the student and their class", () => {
        expect.hasAssertions()
        const wrapper = mountCard()

        expect(wrapper.text()).toContain("Student, Test")
        expect(wrapper.text()).toContain("V1")
    })

    it("shows the field lines it is given", () => {
        expect.hasAssertions()
        expect(mountCard().text()).toContain("Field lines")
    })

    it("dates the card when the record has been saved", () => {
        expect.hasAssertions()
        const wrapper = mountCard({ lastUpdated: "2026-04-17T10:00:00" })

        expect(wrapper.text()).toContain(`Updated ${new Date("2026-04-17T10:00:00").toLocaleDateString()}`)
    })

    it("says nothing about a record that has never been saved", () => {
        expect.hasAssertions()
        expect(mountCard({ lastUpdated: null }).text()).not.toContain("Updated")
    })

    it("links the student's email", () => {
        expect.hasAssertions()
        const link = mountCard({ email: "tstudent@ucdavis.edu" }).find("a[href='mailto:tstudent@ucdavis.edu']")

        expect(link.exists()).toBeTruthy()
        expect(link.text()).toBe("tstudent@ucdavis.edu")
    })

    it("drops the name, class, email and date when their columns are hidden", () => {
        expect.hasAssertions()
        const wrapper = mountCard({ email: "tstudent@ucdavis.edu", lastUpdated: "2026-04-17T10:00:00" }, ["direction"])

        expect(wrapper.text()).not.toContain("Student, Test")
        expect(wrapper.text()).not.toContain("V1")
        expect(wrapper.text()).not.toContain("tstudent@ucdavis.edu")
        expect(wrapper.text()).not.toContain("Updated")
    })

    it("keeps the columns still shown alongside the hidden ones", () => {
        expect.hasAssertions()
        const wrapper = mountCard({ lastUpdated: "2026-04-17T10:00:00" }, ["fullName", "lastUpdated"])

        expect(wrapper.text()).toContain("Student, Test")
        expect(wrapper.text()).not.toContain("V1")
        expect(wrapper.text()).toContain("Updated")
    })
})

describe("mentor selector", () => {
    const mentor = { personId: 500, iamId: "IAM500", fullName: "Vet, Ann", loginId: "avet", mailId: "avet" }

    function mountSelector(modelValue: typeof mentor | null) {
        return mount(MentorSelector, {
            props: { modelValue, label: "Mentor" },
            global: { ...quasar, stubs: { PersonSearchSelect: true } },
        })
    }

    it("searches affiliates through the career selection service", () => {
        expect.hasAssertions()
        const picker = mountSelector(null).findComponent({ name: "PersonSearchSelect" })

        expect(picker.props("search")).toBe(careerSelectionService.searchMentors)
        expect(picker.props("label")).toBe("Mentor")
    })

    it("shows the saved mentor", () => {
        expect.hasAssertions()
        const picker = mountSelector(mentor).findComponent({ name: "PersonSearchSelect" })

        expect(picker.props("modelValue")).toStrictEqual(mentor)
    })

    it("reports a chosen mentor to the form", async () => {
        expect.hasAssertions()
        const wrapper = mountSelector(null)

        await wrapper.findComponent({ name: "PersonSearchSelect" }).vm.$emit("update:modelValue", mentor)

        expect(wrapper.emitted("update:modelValue")).toStrictEqual([[mentor]])
    })

    it("reports a cleared mentor as nothing selected", () => {
        expect.hasAssertions()
        const wrapper = mountSelector(mentor)

        wrapper.findComponent({ name: "PersonSearchSelect" }).vm.$emit("update:modelValue", null)

        expect(wrapper.emitted("update:modelValue")).toStrictEqual([[null]])
    })
})
