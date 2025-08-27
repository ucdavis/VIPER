using Microsoft.EntityFrameworkCore;
using Viper.Classes.SQLContext;
using Viper.Models.ClinicalScheduler;

namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Service for handling rotation and service data from Clinical Scheduler context
    /// Provides centralized access to rotation data with proper Entity Framework patterns
    /// </summary>
    public class RotationService : BaseClinicalSchedulerService, IRotationService
    {
        private readonly ILogger<RotationService> _logger;

        public RotationService(ILogger<RotationService> logger, ClinicalSchedulerContext context) : base(context)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets all rotations with service information
        /// Replaces direct context access in controllers with centralized service logic
        /// </summary>
        /// <param name="activeOnly">If true, filters to only active rotations (placeholder for future filtering)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of rotations with their associated service data</returns>
        public async Task<List<Rotation>> GetRotationsAsync(bool activeOnly = true, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting rotations from Clinical Scheduler (activeOnly: {ActiveOnly})", activeOnly);

                var query = _context.Rotations
                    .AsNoTracking()
                    .Include(r => r.Service);

                // Note: activeOnly parameter is kept for future implementation
                // Currently no active/inactive flag exists in the rotation data
                if (activeOnly)
                {
                    _logger.LogDebug("ActiveOnly filtering requested but no active flag available in rotation data");
                }

                var rotations = await query
                    .ToListAsync(cancellationToken);

                // Deduplicate rotations by RotId to avoid duplicates from database
                var uniqueRotations = rotations
                    .GroupBy(r => r.RotId)
                    .Select(g => g.First()) // Take the first occurrence of each unique RotId
                    .OrderBy(r => r.Service?.ServiceName ?? r.Name)
                    .ThenBy(r => r.Name)
                    .ToList();

                _logger.LogInformation("Retrieved {TotalCount} rotations from Clinical Scheduler, deduplicated to {UniqueCount} unique rotations",
                    rotations.Count, uniqueRotations.Count);
                return uniqueRotations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving rotations from Clinical Scheduler");
                throw new InvalidOperationException("Failed to retrieve rotations from Clinical Scheduler database", ex);
            }
        }

        /// <summary>
        /// Gets rotation by ID with service information
        /// </summary>
        /// <param name="rotationId">The rotation ID to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rotation with service data or null if not found</returns>
        public async Task<Rotation?> GetRotationAsync(int rotationId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting rotation by ID: {RotationId}", rotationId);

                var rotation = await _context.Rotations
                    .AsNoTracking()
                    .Include(r => r.Service)
                    .Where(r => r.RotId == rotationId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (rotation == null)
                {
                    _logger.LogWarning("Rotation not found for ID: {RotationId}", rotationId);
                }
                else
                {
                    _logger.LogInformation("Found rotation: {RotationName} (Service: {ServiceName})",
                        rotation.Name, rotation.Service?.ServiceName ?? "Unknown");
                }

                return rotation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving rotation by ID: {RotationId}", rotationId);
                throw new InvalidOperationException($"Failed to retrieve rotation with ID {rotationId}", ex);
            }
        }

        /// <summary>
        /// Gets rotations by course number and subject code
        /// Replaces legacy course-based lookup functionality
        /// </summary>
        /// <param name="courseNumber">Course number to filter by</param>
        /// <param name="subjectCode">Optional subject code to filter by</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of matching rotations</returns>
        public async Task<List<Rotation>> GetRotationsByCourseAsync(string courseNumber, string? subjectCode = null, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting rotations by course - CourseNumber: {CourseNumber}, SubjectCode: {SubjectCode}",
                    courseNumber, subjectCode ?? "any");

                var query = _context.Rotations
                    .AsNoTracking()
                    .Include(r => r.Service)
                    .Where(r => r.CourseNumber == courseNumber);

                if (!string.IsNullOrEmpty(subjectCode))
                {
                    query = query.Where(r => r.SubjectCode == subjectCode);
                }

                var rotations = await query
                    .OrderBy(r => r.Service.ServiceName ?? r.Name)
                    .ThenBy(r => r.Name)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {Count} rotations for course {CourseNumber}, subject {SubjectCode}",
                    rotations.Count, courseNumber, subjectCode ?? "any");

                return rotations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving rotations by course - CourseNumber: {CourseNumber}, SubjectCode: {SubjectCode}",
                    courseNumber, subjectCode);
                throw new InvalidOperationException($"Failed to retrieve rotations for course {subjectCode} {courseNumber}", ex);
            }
        }

        /// <summary>
        /// Gets rotations by service ID
        /// </summary>
        /// <param name="serviceId">Service ID to filter by</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of rotations for the specified service</returns>
        public async Task<List<Rotation>> GetRotationsByServiceAsync(int serviceId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting rotations by service ID: {ServiceId}", serviceId);

                var rotations = await _context.Rotations
                    .AsNoTracking()
                    .Include(r => r.Service)
                    .Where(r => r.ServiceId == serviceId)
                    .OrderBy(r => r.Name)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {Count} rotations for service ID: {ServiceId}", rotations.Count, serviceId);
                return rotations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving rotations by service ID: {ServiceId}", serviceId);
                throw new InvalidOperationException($"Failed to retrieve rotations for service ID {serviceId}", ex);
            }
        }

        /// <summary>
        /// Gets all services available in Clinical Scheduler
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of all services</returns>
        public async Task<List<Service>> GetServicesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting all services from Clinical Scheduler");

                var services = await _context.Services
                    .AsNoTracking()
                    .OrderBy(s => s.ServiceName)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {Count} services from Clinical Scheduler", services.Count);
                return services;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving services from Clinical Scheduler");
                throw new InvalidOperationException("Failed to retrieve services from Clinical Scheduler database", ex);
            }
        }

        /// <summary>
        /// Gets service by ID
        /// </summary>
        /// <param name="serviceId">Service ID to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Service or null if not found</returns>
        public async Task<Service?> GetServiceAsync(int serviceId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting service by ID: {ServiceId}", serviceId);

                var service = await _context.Services
                    .AsNoTracking()
                    .Where(s => s.ServiceId == serviceId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (service == null)
                {
                    _logger.LogWarning("Service not found for ID: {ServiceId}", serviceId);
                }
                else
                {
                    _logger.LogInformation("Found service: {ServiceName}", service.ServiceName);
                }

                return service;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving service by ID: {ServiceId}", serviceId);
                throw new InvalidOperationException($"Failed to retrieve service with ID {serviceId}", ex);
            }
        }

        /// <summary>
        /// Gets instructor schedules for a specific rotation
        /// Useful for understanding rotation utilization
        /// </summary>
        /// <param name="rotationId">Rotation ID to get schedules for</param>
        /// <param name="startDate">Optional start date filter</param>
        /// <param name="endDate">Optional end date filter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of instructor schedules for the rotation</returns>
        public async Task<List<InstructorSchedule>> GetInstructorSchedulesByRotationAsync(int rotationId,
            DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting instructor schedules for rotation ID: {RotationId} (DateRange: {StartDate} - {EndDate})",
                    rotationId, startDate?.ToString("yyyy-MM-dd") ?? "any", endDate?.ToString("yyyy-MM-dd") ?? "any");

                var query = _context.InstructorSchedules
                    .AsNoTracking()
                    .Include(i => i.Rotation)
                    .Include(i => i.Week)
                    .Where(i => i.RotationId == rotationId);

                if (startDate.HasValue)
                {
                    query = query.Where(i => i.Week.DateStart >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(i => i.Week.DateEnd <= endDate.Value);
                }

                var schedules = await query
                    .OrderBy(i => i.Week.DateStart)
                    .ThenBy(i => i.MothraId) // Order by MothraId instead of person name for now
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {Count} instructor schedules for rotation ID: {RotationId}",
                    schedules.Count, rotationId);

                return schedules;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving instructor schedules for rotation ID: {RotationId}", rotationId);
                throw new InvalidOperationException($"Failed to retrieve instructor schedules for rotation ID {rotationId}", ex);
            }
        }
    }
}