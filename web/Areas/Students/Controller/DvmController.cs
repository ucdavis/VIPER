using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Viper.Areas.Students.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;
using Viper.Models.RAPS;
using Web.Authorization;

namespace Viper.Areas.Students.Controller
{
    [Route("/students/dvm")]
    [Permission(Allow = "SVMSecure.Students")]
    public class DvmStudentsController : ApiController
    {
        private readonly VIPERContext _context;

        public DvmStudentsController(VIPERContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DvmStudent>>> GetDvmStudents(int? classYear, string? classLevel)
        {
            var students = await _context.DvmStudent
                .Where(s => classLevel == null || s.ClassLevel == classLevel)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.LoginId)
                .ToListAsync();
            
            if(classYear != null)
            {
                students = students.Where(s => classYear == GradYearClassLevel.GetGradYear(s.ClassLevel, s.TermCode))
                    .ToList();
            }
            return students;
        }
    }
}
