import { stripTrailingSlashes } from "./strip-trailing-slashes"

/**
 * The path VIPER 2 is served from, normalized for string concatenation: no trailing slash, so
 * callers always supply their own separator (`${applicationBase()}/login`). TEST/PROD run under a
 * "/2" PathBase; at the domain root this is the empty string.
 */
function applicationBase(): string {
    return stripTrailingSlashes(import.meta.env.VITE_VIPER_HOME ?? "/")
}

/**
 * The inverse: drops a leading application base so a browser path becomes router-relative. Routes
 * are declared without the base and the history layer adds it back, so passing a base-prefixed path
 * to the router would resolve to "/2/2/...".
 */
function stripApplicationBase(path: string): string {
    const base = applicationBase()
    if (!base) {
        return path
    }
    // The base on its own addresses the application root.
    if (path === base) {
        return "/"
    }
    return path.startsWith(`${base}/`) ? path.slice(base.length) : path
}

export { applicationBase, stripApplicationBase }
