// Page Data Service for Clinical Scheduler
import { useFetch } from "../../composables/ViperFetch"

export interface PageInitialData {
    currentGradYear: number
    availableGradYears: number[]
}

export class PageDataService {
    private static readonly PAGE_DATA_URL = `${import.meta.env.VITE_API_URL}clinicalscheduler/rotations/page-data`
    
    // Cache to prevent multiple API calls within the same session
    private static cachedData: PageInitialData | null = null
    private static cachePromise: Promise<PageInitialData> | null = null
    private static cacheTime: number | null = null
    private static readonly MAX_AGE_MS = 10 * 60 * 1000 // 10 minutes TTL

    /**
     * Gets initial page data for Clinical Scheduler (with caching)
     * This replaces multiple individual API calls with a single optimized call
     */
    static async getPageData(): Promise<PageInitialData> {
        // Return cached data if available and not expired
        if (this.cachedData && this.cacheTime) {
            if (Date.now() - this.cacheTime < this.MAX_AGE_MS) {
                return this.cachedData
            }
            // Cache expired, clear it
            this.clearCache()
        }

        // If a request is already in progress, return the same promise to avoid duplicate requests
        if (this.cachePromise) {
            return this.cachePromise
        }

        // Create the promise and cache it immediately to prevent duplicate requests
        this.cachePromise = this.fetchPageData()
        
        try {
            const result = await this.cachePromise
            this.cachedData = result
            this.cacheTime = Date.now()
            return result
        } catch (error) {
            // Clear the promise cache on error so retries can occur
            this.cachePromise = null
            throw error
        }
    }

    /**
     * Internal method to fetch data from API
     */
    private static async fetchPageData(): Promise<PageInitialData> {
        const { get } = useFetch()
        try {
            const data = await get(this.PAGE_DATA_URL)
            // ViperFetch wraps response in {result, errors, success} structure
            // Backend returns: { currentGradYear, availableGradYears }
            const result = {
                currentGradYear: data.result.currentGradYear,
                availableGradYears: data.result.availableGradYears,
            }
            return result
        } catch {
            // Critical error - page data is required for the scheduler to function
            throw new Error("Unable to load page configuration. Please contact support.")
        }
    }

    /**
     * Clear the cache (useful for testing or if data needs to be refreshed)
     */
    static clearCache(): void {
        this.cachedData = null
        this.cachePromise = null
        this.cacheTime = null
    }

    /**
     * Gets just the current grad year from cached data
     */
    static async getCurrentGradYear(): Promise<number> {
        const result = await this.getPageData()
        if (!result.currentGradYear) {
            throw new Error("Current grad year not configured. Please contact support.")
        }
        return result.currentGradYear
    }

    /**
     * Gets available grad years from cached data
     */
    static async getAvailableYears(): Promise<number[]> {
        const result = await this.getPageData()
        if (!Array.isArray(result.availableGradYears) || result.availableGradYears.length === 0) {
            throw new Error("Available grad years not configured. Please contact support.")
        }
        return result.availableGradYears
    }
}