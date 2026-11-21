/** A promise the test settles by hand, to hold a request in flight. */
function deferred<T>() {
    let settle: ((value: T) => void) | null = null
    // eslint-disable-next-line avoid-new -- a promise the test settles by hand
    const promise = new Promise<T>((resolve) => {
        settle = resolve
    })
    return { promise, resolve: (value: T) => settle?.(value) }
}

export { deferred }
