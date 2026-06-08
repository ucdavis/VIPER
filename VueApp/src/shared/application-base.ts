import { stripTrailingSlashes } from "./strip-trailing-slashes"

/**
 * The path VIPER 2 is served from, normalized for string concatenation: no trailing slash, so
 * callers always supply their own separator (`${applicationBase()}/welcome`). TEST/PROD run under a
 * "/2" PathBase; at the domain root this is the empty string. Tolerates a missing env var (tests,
 * misconfigured builds).
 */
export function applicationBase(): string {
    return stripTrailingSlashes(import.meta.env.VITE_VIPER_HOME ?? "/")
}
