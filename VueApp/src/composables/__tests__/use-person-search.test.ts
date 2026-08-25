import { usePersonSearch } from "../use-person-search"

type Person = { iamId: string; fullName: string }

type Deferred<T> = { promise: Promise<T>; resolve: (value: T) => void }

// A controllable pending promise, for simulating a search that hasn't resolved yet. The `as`
// cast lets `resolve` be filled in by the executor without a separate uninitialized declaration.
function createDeferred<T>(): Deferred<T> {
    const deferred = {} as Deferred<T>
    // eslint-disable-next-line avoid-new -- a controllable pending promise is the point of this helper
    deferred.promise = new Promise<T>((resolve) => {
        deferred.resolve = resolve
    })
    return deferred
}

function applyUpdate(fn: () => void): void {
    fn()
}

function runFilter(searchPeople: (val: string, update: (fn: () => void) => void) => Promise<void>, val: string) {
    return searchPeople(val, applyUpdate)
}

describe("usePersonSearch()", () => {
    it("clears options without calling search when the term is below two characters", async () => {
        expect.hasAssertions()
        const search = vi.fn<(value: string) => Promise<Person[] | null>>()
        const { searchPeople, options, loading } = usePersonSearch<Person>(search)
        options.value = [{ iamId: "a", fullName: "Existing Person" }]

        await runFilter(searchPeople, "a")

        expect(search).not.toHaveBeenCalled()
        expect(options.value).toStrictEqual([])
        expect(loading.value).toBeFalsy()
    })

    it("sets loading and populates options for a valid search", async () => {
        expect.hasAssertions()
        const results: Person[] = [{ iamId: "person01", fullName: "Amy Smith" }]
        const search = vi.fn<(value: string) => Promise<Person[] | null>>().mockResolvedValue(results)
        const { searchPeople, options } = usePersonSearch<Person>(search)

        await runFilter(searchPeople, " ab ")

        expect(search).toHaveBeenCalledWith("ab")
        expect(options.value).toStrictEqual(results)
    })

    it("falls back to an empty list when search resolves null", async () => {
        expect.hasAssertions()
        const search = vi.fn<(value: string) => Promise<Person[] | null>>().mockResolvedValue(null)
        const { searchPeople, options } = usePersonSearch<Person>(search)

        await runFilter(searchPeople, "ab")

        expect(options.value).toStrictEqual([])
    })

    it("discards a slower, earlier response that resolves after a newer search", async () => {
        expect.hasAssertions()
        const first = createDeferred<Person[]>()
        const second = createDeferred<Person[]>()
        const search = vi
            .fn<(value: string) => Promise<Person[] | null>>()
            .mockReturnValueOnce(first.promise)
            .mockReturnValueOnce(second.promise)
        const { searchPeople, options } = usePersonSearch<Person>(search)

        const firstFilter = runFilter(searchPeople, "first")
        const secondFilter = runFilter(searchPeople, "second")

        second.resolve([{ iamId: "second", fullName: "Second Result" }])
        await secondFilter
        first.resolve([{ iamId: "first", fullName: "First Result" }])
        await firstFilter

        expect(options.value).toStrictEqual([{ iamId: "second", fullName: "Second Result" }])
    })
})
