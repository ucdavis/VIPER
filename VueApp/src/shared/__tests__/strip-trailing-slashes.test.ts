import { stripTrailingSlashes } from "../strip-trailing-slashes"

describe("stripTrailingSlashes()", () => {
    it("strips a single trailing slash", () => {
        expect(stripTrailingSlashes("/2/")).toBe("/2")
    })

    it("strips multiple trailing slashes", () => {
        expect(stripTrailingSlashes("/2///")).toBe("/2")
    })

    it("leaves a path with no trailing slash unchanged", () => {
        expect(stripTrailingSlashes("/2")).toBe("/2")
    })

    it("reduces a root path of only slashes to an empty string", () => {
        expect(stripTrailingSlashes("/")).toBe("")
    })
})
