import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import { setActivePinia, createPinia } from "pinia"
import ProfilePic from "@/layouts/ProfilePic.vue"
import { useUserStore } from "@/store/UserStore"

const TOKEN = "csrf-token"

async function openMenu(): Promise<HTMLFormElement> {
    // TEST and PROD serve VIPER 2 under a /2 PathBase, so the action has to come from the
    // configured home rather than a bare "/logout" that would escape to the legacy site.
    vi.stubEnv("VITE_VIPER_HOME", "/2/")
    setActivePinia(createPinia())
    useUserStore().loadUser({
        firstName: "Ada",
        lastName: "Lovelace",
        mailId: "alovelace",
        loginId: "alovelace",
        mothraId: "1",
        userId: 1,
        token: TOKEN,
        emulating: false,
        permissions: [],
    })

    const wrapper = mount(ProfilePic, { global: { plugins: [[Quasar, {}]] } })
    // QBtnDropdown teleports its menu to the body, so the entries only exist once it is open.
    await wrapper.get("button").trigger("click")

    const form = document.body.querySelector("form")
    if (form === null) {
        throw new Error("logout form was not rendered")
    }
    return form
}

// Logout is POST-only, so the menu entry submits a form with an antiforgery token: a plain link
// would 405, and relaxing the verb to make one work would reopen the CSRF hole.
describe("profilePic logout", () => {
    afterEach(() => {
        document.body.innerHTML = ""
        vi.unstubAllEnvs()
    })

    it("posts to the logout URL rather than linking to it", async () => {
        expect.hasAssertions()
        const form = await openMenu()

        expect(form.getAttribute("method")).toBe("post")
        expect(form.getAttribute("action")).toBe("/2/logout")
    })

    it("carries the antiforgery token as the form field ASP.NET Core reads on a post", async () => {
        // Every other caller sends this token in the X-CSRF-TOKEN header; a form post is the one
        // place the default field name is what gets validated.
        expect.hasAssertions()
        const form = await openMenu()
        const token = form.querySelector<HTMLInputElement>('input[name="__RequestVerificationToken"]')

        expect(token?.type).toBe("hidden")
        expect(token?.value).toBe(TOKEN)
    })

    it("renders the menu entry as a button inside the form so the click submits it", async () => {
        expect.hasAssertions()
        const form = await openMenu()
        const button = form.querySelector("button.q-item")

        expect(button?.textContent).toContain("Logout")
    })
})
