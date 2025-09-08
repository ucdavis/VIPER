/**
 * Cache management utilities for the schedule store
 */

const CACHE_DURATION_MS = 300_000 // 5 minutes in milliseconds

class ScheduleCache {
    private cacheTimestamps = new Map<string, number>()

    /**
     * Check if a cache entry is stale
     */
    isStale(key: string): boolean {
        const timestamp = this.cacheTimestamps.get(key)
        if (!timestamp) {
            return true
        }
        return Date.now() - timestamp > CACHE_DURATION_MS
    }

    /**
     * Mark a cache entry as fresh
     */
    markFresh(key: string): void {
        this.cacheTimestamps.set(key, Date.now())
    }

    /**
     * Invalidate a specific cache entry
     */
    invalidate(key: string): void {
        this.cacheTimestamps.delete(key)
    }

    /**
     * Clear all cache timestamps
     */
    invalidateAll(): void {
        this.cacheTimestamps.clear()
    }

    /**
     * Get cache key for rotation schedule
     */
    static getRotationKey(rotationId: number, year: number): string {
        return `${rotationId}-${year}`
    }

    /**
     * Get cache key for clinician schedule
     */
    static getClinicianKey(mothraId: string, year: number): string {
        return `${mothraId}-${year}`
    }
}

const scheduleCache = new ScheduleCache()

// Single export declaration
export { ScheduleCache, scheduleCache }
