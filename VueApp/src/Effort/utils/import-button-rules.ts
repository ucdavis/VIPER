/**
 * Rules governing the Import / "Use Course" button in CourseImportDialog.
 *
 * The backend's `alreadyImported` flag is true for fixed-unit courses that
 * exist in Effort for the term AND for VET 4XX courses (any unit type) that
 * exist for the term — the latter are harvested during the Harvest period and
 * cannot be manually re-imported.
 *
 * Extracted so the component and its tests share one source of truth.
 */

type AlreadyImportedCourse = { alreadyImported: boolean }
type VariableUnitCourse = AlreadyImportedCourse & { isVariableUnits: boolean }

/** Whether the Import/Use Course button should render for this course row. */
function shouldShowImportButton(isSelfMode: boolean, course: AlreadyImportedCourse): boolean {
    return isSelfMode || !course.alreadyImported
}

/** The label to display on the button. */
function importButtonLabel(isSelfMode: boolean, course: AlreadyImportedCourse): string {
    return isSelfMode && course.alreadyImported ? "Use Course" : "Import"
}

/**
 * Whether `startImport` should bypass the units-selection dialog and import
 * immediately. Self mode + already-imported + fixed-unit lets the backend
 * return the existing row by CRN match. Variable-unit courses always open the
 * dialog so the user can pick the unit value.
 */
function shouldShortcutToImport(isSelfMode: boolean, course: VariableUnitCourse): boolean {
    return isSelfMode && course.alreadyImported && !course.isVariableUnits
}

export { shouldShowImportButton, importButtonLabel, shouldShortcutToImport }
