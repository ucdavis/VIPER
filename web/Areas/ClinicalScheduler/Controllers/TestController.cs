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
    }
}