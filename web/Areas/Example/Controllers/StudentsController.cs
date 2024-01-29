using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System.Configuration;
using Viper.Areas.Example.Models;
using Viper.Areas.RAPS.Models;
using Viper.Areas.RAPS.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Models.RAPS;

namespace Viper.Areas.Example.Controllers
{
    [Route("example/[controller]")]
    [Authorize(Roles = "VMDO SVM-IT", Policy = "2faAuthentication")]
    public class StudentsController : ApiController
    {
        private readonly AAUDContext _context;
        public static List<string> ValidGroups { get; set; } = new List<string>()
        {
            "1A2","1B1","1B2","2A1","2A2","2B1","2B2"
        };
        public static List<string> Valid20thGroups { get; set; } = new List<string>()
        {
            "1AB","1AC","1AD","1AE","1BA","1BB","1BC","1BD","1BE","2AA","2AB","2AC","2AD","2AE","2BA","2BB","2BC","2BD","2BE"
        };
        public static List<string> ValidV3Groups { get; set; } = new List<string>()
        {
            "LA","EQ","LIVE","ZOO"
        };

        public StudentsController(AAUDContext context)
        {
            _context = context;
        }

        // GET: Student info
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StudentClassLevelGroup>>> GetStudents(string? classLevel)
        {
            if (_context.AaudUsers == null)
            {
                return NotFound();
            }

            List<string> validClassLevels = new() { "V1", "V2", "V3", "V4" };
            if (classLevel != null && !validClassLevels.Contains(classLevel))
            {
                classLevel = null;
            }
            classLevel ??= "V1";
            return await _context.AaudUsers
                .Where(s => (s.CurrentStudent || s.FutureStudent))
                .Select(s => new StudentClassLevelGroup()
                {
                    IamId = s.IamId,
                    Pidm = s.Pidm,
                    MailId = s.MailId,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    MiddleName = s.MiddleName,
                    Student = _context.Students
                        .Where(student => student.StudentsClientid == s.SpridenId && student.StudentsTermCode == s.StudentTerm.ToString())
                        .FirstOrDefault(),
                    Studentgrp = _context.Studentgrps
                        .Where(studentgrp => studentgrp.StudentgrpPidm == s.Pidm)
                        .FirstOrDefault()
                })
                .Where(s => s.Student != null && s.Student.StudentsClassLevel == classLevel)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();
        }

        // PUT: Students/12345678/Groups
        [HttpPut("{pidm}/Groups")]
        public async Task<IActionResult> SaveGroups(int pidm, Studentgrp studentGrp)
        {
            var existingRecord = _context.Studentgrps.Find(pidm.ToString());
            ModelStateDictionary modelErrors = ValidateStudentGroup(studentGrp);
            if(modelErrors.Count > 0)
            {
                return ValidationProblem(modelErrors);
            }
            if(existingRecord != null)
            {
                _context.Entry(existingRecord).CurrentValues.SetValues(studentGrp);
            }
            else
            {
                _context.Add(studentGrp);
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        public ModelStateDictionary ValidateStudentGroup(Studentgrp studentGrp)
        {
            var modelStateDictionary = new ModelStateDictionary();
            if (!string.IsNullOrEmpty(studentGrp.StudentgrpGrp) && !ValidGroups.Contains(studentGrp.StudentgrpGrp.Trim()))
            {
                modelStateDictionary.AddModelError("StudentgrpGrp", "Student Group must be valid");
            }
            if (!string.IsNullOrEmpty(studentGrp.Studentgrp20) && !Valid20thGroups.Contains(studentGrp.Studentgrp20.Trim()))
            {
                modelStateDictionary.AddModelError("Studentgrp20", "Student 20th Group must be valid");
            }
            if (!string.IsNullOrEmpty(studentGrp.StudentgrpV3grp) && !ValidV3Groups.Contains(studentGrp.StudentgrpV3grp.Trim()))
            {
                modelStateDictionary.AddModelError("StudentgrpV3grp", "Student V3 Group must be valid");
            }
            if(!string.IsNullOrEmpty(studentGrp.StudentgrpTeamno))
            {
                bool success = int.TryParse(studentGrp.StudentgrpTeamno, out int teamno);
                if (!success || teamno < 1 || teamno > 30)
                {
                    modelStateDictionary.AddModelError("StudentgrpTeamno", "Team must be a valid number between 1 and 30");
                }
            }
            return modelStateDictionary;
        }
    }
}
