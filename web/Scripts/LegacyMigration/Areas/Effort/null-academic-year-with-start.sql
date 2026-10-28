-- Percentages with a start date but no academic year, which the legacy procedures' filter on the
-- academic year silently skips. One row per percentage.
SELECT p.percent_ID, p.percent_modifiedOn
FROM dbo.tblPercent AS p
WHERE (p.percent_AcademicYear IS NULL OR p.percent_AcademicYear = '')
  AND p.percent_start IS NOT NULL
