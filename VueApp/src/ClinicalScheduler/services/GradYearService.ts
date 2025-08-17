// Grad Year Service for Clinical Scheduler
import { useFetch } from '../../composables/ViperFetch'

export interface GradYearData {
    currentGradYear: number
    availableGradYears: number[]
}

export class GradYearService {
    private static readonly YEARS_URL = '/api/clinicalscheduler/rotations/years'

    /**
     * Gets the current grad year and available years from the backend
     */
    static async getGradYearData(): Promise<GradYearData> {
        const { get } = useFetch()
        try {
            // Add cache busting parameter to force fresh data
            const url = `${this.YEARS_URL}?_t=${Date.now()}`
            const data = await get(url)
            // ViperFetch wraps response in {result, errors, success} structure
            // Backend returns: { currentGradYear, availableGradYears, defaultYear }
            const result = {
                currentGradYear: data.result.currentGradYear,
                availableGradYears: data.result.availableGradYears
            }
            return result
        } catch (error) {
            console.error('Error fetching grad year data:', error)
            return {
                currentGradYear: new Date().getFullYear(),
                availableGradYears: []
            }
        }
    }

    /**
     * Gets just the current grad year
     */
    static async getCurrentGradYear(): Promise<number> {
        const result = await this.getGradYearData()
        return result.currentGradYear ?? new Date().getFullYear()
    }

    /**
     * Gets available grad years for dropdowns
     */
    static async getAvailableYears(): Promise<number[]> {
        const result = await this.getGradYearData()
        if (Array.isArray(result.availableGradYears) && result.availableGradYears.length > 0) {
            return result.availableGradYears
        }
        const thisYear = new Date().getFullYear()
        return Array.from({ length: 6 }, (_, i) => thisYear - i)
    }
}
