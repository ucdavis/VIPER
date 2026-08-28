import { useAddRecordDialog } from "../composables/use-add-record-dialog"
import type { SaveOutcome } from "../composables/use-add-record-dialog"

const { mockNotify } = vi.hoisted(() => ({
    mockNotify: vi.fn<(...args: unknown[]) => unknown>(),
}))

vi.mock("quasar", () => ({
    useQuasar: () => ({ notify: mockNotify }),
}))

type Form = { label: string }

// Match the real sendSave signature rather than a loose (...args: unknown[]) mock, so a change
// to what the composable passes its save callback is a compile error here instead of a silent
// mismatch that the mock happily absorbs.
type SendSave = (form: Form, isEdit: boolean) => Promise<SaveOutcome>

function makeSendSave(outcome?: SaveOutcome) {
    const mock = vi.fn<SendSave>()
    if (outcome) {
        mock.mockResolvedValue(outcome)
    }
    return mock
}

type Deferred<T> = { promise: Promise<T>; resolve: (value: T) => void }

// A controllable pending promise, so a save can be held open mid-flight. The `as` cast lets
// `resolve` be filled in by the executor without a separate uninitialized declaration.
function createDeferred<T>(): Deferred<T> {
    const deferred = {} as Deferred<T>
    // eslint-disable-next-line avoid-new -- a controllable pending promise is the point of this helper
    deferred.promise = new Promise<T>((resolve) => {
        deferred.resolve = resolve
    })
    return deferred
}

function buildOptions(overrides: Partial<Parameters<typeof useAddRecordDialog<Form, Form>>[0]> = {}) {
    return {
        editData: () => null,
        emptyForm: () => ({ label: "" }),
        formFromEditData: () => ({ label: "" }),
        validate: () => null,
        sendSave: makeSendSave({ success: true, result: { id: 1 }, errors: null }),
        onSaved: vi.fn<(result: unknown) => void>(),
        onClose: vi.fn<() => void>(),
        ...overrides,
    }
}

describe("useAddRecordDialog()", () => {
    it("surfaces the validation error and never calls sendSave when the form is invalid", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        const sendSave = makeSendSave()
        const { save, formError } = useAddRecordDialog(buildOptions({ validate: () => "Label is required.", sendSave }))

        await save()

        expect(sendSave).not.toHaveBeenCalled()
        expect(formError.value).toBe("Label is required.")
    })

    it("notifies, saves the result, and closes the dialog on a successful save", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        const onSaved = vi.fn<(result: unknown) => void>()
        const onClose = vi.fn<() => void>()
        const sendSave = makeSendSave({ success: true, result: { id: 42 }, errors: null })
        const { save } = useAddRecordDialog(buildOptions({ sendSave, onSaved, onClose }))

        await save()

        expect(onSaved).toHaveBeenCalledWith({ id: 42 })
        expect(onClose).toHaveBeenCalledWith()
        expect(mockNotify).toHaveBeenCalledWith(expect.objectContaining({ type: "positive" }))
    })

    it("surfaces the server error and leaves the dialog open on a failed save", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        const onSaved = vi.fn<(result: unknown) => void>()
        const onClose = vi.fn<() => void>()
        const sendSave = makeSendSave({ success: false, errors: ["Phone number is already in use."] })
        const { save, formError } = useAddRecordDialog(buildOptions({ sendSave, onSaved, onClose }))

        await save()

        expect(formError.value).toBe("Phone number is already in use.")
        expect(onSaved).not.toHaveBeenCalled()
        expect(onClose).not.toHaveBeenCalled()
    })

    it("ignores a second save call while the first is still in flight", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        const deferredSave = createDeferred<SaveOutcome>()
        const sendSave = vi.fn<SendSave>(() => deferredSave.promise)
        const { save } = useAddRecordDialog(buildOptions({ sendSave }))

        const firstSave = save()
        await save()

        expect(sendSave).toHaveBeenCalledOnce()

        deferredSave.resolve({ success: true, result: null, errors: null })
        await firstSave
    })
})
