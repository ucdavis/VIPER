-- Courses with no CRN that effort records use. A blank CRN on a course nothing uses does not matter
-- to the migration. One row per course.
SELECT c.course_id
FROM dbo.tblCourses AS c
WHERE (c.course_CRN IS NULL OR c.course_CRN = '')
  AND EXISTS (SELECT 1 FROM dbo.tblEffort AS e WHERE e.effort_CourseID = c.course_id)
