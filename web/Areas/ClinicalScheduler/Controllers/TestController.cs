using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;
using Web.Authorization;

namespace Viper.Areas.ClinicalScheduler.Controllers
{
    [Route("/clinicalscheduler/test")]
    [Permission(Allow = "SVMSecure.ClnSched")]
    public class TestController : ApiController
    {
        private readonly VIPERContext _context;
        private readonly ClinicalSchedulerContext _clinicalSchedulerContext;

        public TestController(VIPERContext context, ClinicalSchedulerContext clinicalSchedulerContext)
        {
            _context = context;
            _clinicalSchedulerContext = clinicalSchedulerContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Rotation>>> GetRotations()
        {
            try
            {
                var rotations = await _context.Rotations
                    .Include(r => r.Service)
                    .Take(5) // Limit to 5 for testing
                    .ToListAsync();

                return Ok(rotations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Database connection failed", message = ex.Message });
            }
        }

        [HttpGet("connection")]
        public async Task<ActionResult> TestConnection()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                var rotationCount = await _context.Rotations.CountAsync();
                
                return Ok(new { 
                    canConnect = canConnect,
                    rotationCount = rotationCount,
                    message = "Database connection successful"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Database connection failed", message = ex.Message });
            }
        }

        [HttpGet("scheduler-context")]
        public async Task<ActionResult> TestClinicalSchedulerContext()
        {
            try
            {
                var canConnect = await _clinicalSchedulerContext.Database.CanConnectAsync();
                var rotationCount = await _clinicalSchedulerContext.Rotations.CountAsync();
                var serviceCount = await _clinicalSchedulerContext.Services.CountAsync();
                
                var sampleRotations = await _clinicalSchedulerContext.Rotations
                    .Include(r => r.Service)
                    .Take(3)
                    .Select(r => new 
                    {
                        r.RotId,
                        r.Name,
                        r.Abbreviation,
                        ServiceName = r.Service.ServiceName
                    })
                    .ToListAsync();
                
                return Ok(new { 
                    canConnect = canConnect,
                    rotationCount = rotationCount,
                    serviceCount = serviceCount,
                    sampleRotations = sampleRotations,
                    message = "ClinicalSchedulerContext working successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "ClinicalSchedulerContext failed", message = ex.Message });
            }
        }

        [HttpGet("test-all-models")]
        public async Task<ActionResult> TestAllModels()
        {
            try
            {
                var canConnect = await _clinicalSchedulerContext.Database.CanConnectAsync();
                
                // Test each model with count queries (safer than actual data queries for views that might not exist)
                var results = new
                {
                    canConnect = canConnect,
                    rotationCount = await SafeCountAsync("Rotations", () => _clinicalSchedulerContext.Rotations.CountAsync()),
                    serviceCount = await SafeCountAsync("Services", () => _clinicalSchedulerContext.Services.CountAsync()),
                    instructorScheduleCount = await SafeCountAsync("InstructorSchedules", () => _clinicalSchedulerContext.InstructorSchedules.CountAsync()),
                    weekCount = await SafeCountAsync("Weeks", () => _clinicalSchedulerContext.Weeks.CountAsync()),
                    weekGradYearCount = await SafeCountAsync("WeekGradYears", () => _clinicalSchedulerContext.WeekGradYears.CountAsync()),
                    // scheduleAuditCount temporarily removed - will be added back in Phase 7
                    // scheduleAuditCount = await SafeCountAsync("ScheduleAudits", () => _clinicalSchedulerContext.ScheduleAudits.CountAsync()),
                    statusCount = await SafeCountAsync("Statuses", () => _clinicalSchedulerContext.Statuses.CountAsync()),
                    message = "All models tested successfully (ScheduleAudit temporarily disabled)"
                };
                
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Model testing failed", message = ex.Message });
            }
        }

        private async Task<object> SafeCountAsync(string modelName, Func<Task<int>> countFunction)
        {
            try
            {
                var count = await countFunction();
                return new { success = true, count = count };
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message };
            }
        }
    }
}