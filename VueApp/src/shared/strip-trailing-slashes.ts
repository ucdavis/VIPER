/** Strips every trailing slash, not just one, so a misconfigured base path (e.g. "/2///") can't leave a stray slash behind. */
export function stripTrailingSlashes(path: string): string {
    return path.replace(/\/+$/u, "")
}
