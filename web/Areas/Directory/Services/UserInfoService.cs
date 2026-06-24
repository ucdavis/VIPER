using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Viper.Classes.SQLContext;
using Viper.Areas.Directory.Models;
using Viper.Models.AAUD;
using Viper.Models.PPS;
using Viper.Models.IAM;
using Viper.Classes.Utilities;
using System.Text.Json;
using System.Text.Json.Serialization;
using Viper.Areas.RAPS.Services;
using Viper.Areas.RAPS.Models.Uinform;

namespace Viper.Areas.Directory.Services
{
    public class UserInfoService
    {
        private readonly AAUDContext _aaudContext;
        private readonly RAPSContext _rapsContext;
        private readonly CoursesContext _coursesContext;
        private readonly EquipmentLoanContext _equipmentLoanContext;
        private readonly PPSContext _ppsContext;
        private readonly IDCardsContext _idCardsContext;
        private readonly KeysContext _keysContext;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _memoryCache;

        public UserInfoService(
            AAUDContext aaudContext, 
            RAPSContext rapsContext,
            CoursesContext coursesContext,
            EquipmentLoanContext equipmentLoanContext,
            PPSContext ppsContext,
            IDCardsContext idCardsContext,
            KeysContext keysContext,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IMemoryCache memoryCache)
        {
            _aaudContext = aaudContext;
            _rapsContext = rapsContext;
            _coursesContext = coursesContext;
            _equipmentLoanContext = equipmentLoanContext;
            _ppsContext = ppsContext;
            _idCardsContext = idCardsContext;
            _keysContext = keysContext;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// Get user information by iamid or mothraid
        /// </summary>
        public async Task<UserInfoResult?> GetUserInfoAsync(string? iamId, string? mothraId)
        {
            UserInfoResult? result = null;

            // Try to get user by IAM ID first
            if (!string.IsNullOrEmpty(iamId))
            {
                result = await GetUserByIamIdAsync(iamId);
            }

            // Fall back to Mothra ID if IAM ID didn't work
            if ((result == null || !result.IsValid) && !string.IsNullOrEmpty(mothraId))
            {
                result = await GetUserByMothraIdAsync(mothraId);
            }

            if (result == null || !result.IsValid)
            {
                return null;
            }

            Console.WriteLine($"[INSTINCT SERVICE] mothraId: '{mothraId}', iamId: '{iamId}', result.MothraId: '{result.MothraId}'");
            var individual = await _aaudContext.AaudUsers.Where(u => (u.MothraId == result.MothraId)).FirstOrDefaultAsync();
            Console.WriteLine($"[INSTINCT SERVICE] individual is null: {individual == null}");
            if (individual != null)
            {
                Console.WriteLine($"[INSTINCT SERVICE] individual: '{individual.DisplayFullName}', LastName: '{individual.LastName}', FirstName: '{individual.FirstName}'");
            }

            // Populate additional information
            await PopulateDirectoryInfoAsync(result);
            await PopulateEmployeeInfoAsync(result);
            await PopulateStudentInfoAsync(result);
            await PopulateIamInfoAsync(result);
            await PopulateSystemRolesAsync(result);
            await PopulateUCPathInfoAsync(result);
            await PopulateIDCardsAsync(result);
            await PopulateKeysAsync(result);
            await PopulateLoansAsync(result);
            await PopulateInstinctInfoAsync(result,individual);
            await PopulateActiveDirectoryInfoAsync(result);

            return result;
        }

        /// <summary>
        /// Get user by iamid
        /// </summary>
        private async Task<UserInfoResult?> GetUserByIamIdAsync(string iamId)
        {
            try
            {
                // Get current terms
                var currentTerms = await GetCurrentTermsAsync();
                
                var user = await _aaudContext.AaudUsers
                    .Where(u => u.IamId == iamId)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return null;
                }

                return await MapToUserInfoResultAsync(user, currentTerms);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: GetUserByIamIdAsync failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get user by mothraid
        /// </summary>
        private async Task<UserInfoResult?> GetUserByMothraIdAsync(string mothraId)
        {
            try
            {
                var currentTerms = await GetCurrentTermsAsync();
                
                var user = await _aaudContext.AaudUsers
                    .Where(u => u.MothraId == mothraId)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return null;
                }

                return await MapToUserInfoResultAsync(user, currentTerms);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: GetUserByMothraIdAsync failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get current academic terms
        /// </summary>
        private async Task<List<string>> GetCurrentTermsAsync()
        {
            try
            {
                var terms = await _coursesContext.Terminfos
                    .Where(t => t.TermCurrentTermMulti == true)
                    .Select(t => t.TermCode)
                    .ToListAsync();
                
                return terms;
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// get data form aaudUser
        /// </summary>
        private async Task<UserInfoResult> MapToUserInfoResultAsync(AaudUser user, List<string> currentTerms)
        {
            var result = new UserInfoResult
            {
                IamId = user.IamId,
                MothraId = user.MothraId,
                MailId = user.MailId,
                DisplayFullName = user.DisplayFullName,
                LoginId = user.LoginId,
                EmployeeId = user.EmployeeId,
                Pidm = user.Pidm,
                MivId = user.MivId?.ToString(),
                IsValid = true,
                CurrentAffiliate = user.Current == 1
            };

            // Check if employee or student
            var hasEmployee = await _aaudContext.Employees
                .AnyAsync(e => e.EmpPKey == user.EmployeePKey && currentTerms.Contains(e.EmpTermCode));
            
            var hasStudent = await _aaudContext.Students
                .AnyAsync(s => s.StudentsPKey == user.StudentPKey && 
                              s.StudentsLevelCode1 == "VM" && 
                              currentTerms.Contains(s.StudentsTermCode));

            result.IsEmployee = hasEmployee;
            result.IsStudent = hasStudent;

            return result;
        }

        /// <summary>
        /// get data from LDAP/VMACS
        /// </summary>
        private async Task PopulateDirectoryInfoAsync(UserInfoResult result)
        {
            try
            {
                var ldapUser = LdapService.GetUserByID(result.IamId);
                if (ldapUser != null)
                {
                    result.Title = ldapUser.Title;
                    result.Email = ldapUser.Mail;
                    result.Phone = ldapUser.TelephoneNumber;
                    result.Mobile = ldapUser.Mobile;
                    result.PostalAddress = ldapUser.PostalAddress;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: PopulateDirectoryInfoAsync LDAP failed: {ex.Message}");
            }

            try
            {
                // Get VMACS information
                var vmacs = await VMACSService.Search(result.LoginId);
                if (vmacs?.item != null)
                {
                    if (vmacs.item.Nextel?.Length > 0) result.Pager = vmacs.item.Nextel[0];
                    if (vmacs.item.Unit?.Length > 0) result.Department = vmacs.item.Unit[0];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: PopulateDirectoryInfoAsync VMACS failed: {ex.Message}");
            }
        }

        /// <summary>
        /// get employee data
        /// </summary>
        private async Task PopulateEmployeeInfoAsync(UserInfoResult result)
        {
            if (!result.IsEmployee || string.IsNullOrEmpty(result.EmployeeId))
                return;

            try
            {
                var currentTerms = await GetCurrentTermsAsync();
                var aaudUser = await _aaudContext.AaudUsers
                    .Where(u => u.EmployeeId == result.EmployeeId)
                    .FirstOrDefaultAsync();

                if (aaudUser?.EmployeePKey != null)
                {
                    var employee = await _aaudContext.Employees
                        .Where(e => e.EmpPKey == aaudUser.EmployeePKey && currentTerms.Contains(e.EmpTermCode))
                        .FirstOrDefaultAsync();

                    if (employee != null)
                    {
                        result.EmployeePrimaryTitle = employee.EmpPrimaryTitle;
                        result.EmployeeSchoolDivision = employee.EmpSchoolDivision;
                        result.EmployeeStatus = employee.EmpStatus;
                        result.EmployeeTerm = employee.EmpTermCode;
                        result.EmployeeHomeDepartment = employee.EmpHomeDept;
                        result.EmployeeEffortHomeDepartment = employee.EmpEffortHomeDept;
                        result.EmployeeTeachingHomeDepartment = employee.EmpTeachingHomeDept;
                        result.EmployeeTeachingPercentFulltime = employee.EmpTeachingPercentFulltime?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: PopulateEmployeeInfoAsync failed: {ex.Message}");
            }
        }

        /// <summary>
        /// get student data
        /// </summary>
        private async Task PopulateStudentInfoAsync(UserInfoResult result)
        {
            if (!result.IsStudent || string.IsNullOrEmpty(result.Pidm))
                return;

            try
            {
                // Get current term for the student
                var currentTerm = await GetCurrentOrFutureTermForStudentAsync(result.Pidm);
                result.StudentTerm = currentTerm;

                // Get basic student information (non-term dependent)
                result.StudentPriorName = await GetStudentPriorNamesAsync(result.Pidm);
                result.StudentBannerId = await GetStudentBannerIdAsync(result.Pidm);
                result.StudentConfidential = await IsStudentConfidentialAsync(result.Pidm);
                result.StudentConfidentialScope = await GetStudentConfidentialScopeAsync(result.Pidm);
                result.StudentBirthDate = await GetStudentBirthDateAsync(result.Pidm);
                result.StudentAge = await GetStudentAgeAsync(result.Pidm);
                result.StudentAcademicStanding = await GetStudentAcademicStandingAsync(result.Pidm);
                result.StudentAdmitClassYear = await GetStudentAdmitClassYearAsync(result.Pidm);
                result.StudentGender = await GetStudentGenderAsync(result.Pidm);
                result.StudentEthnicity = await GetStudentEthnicityAsync(result.Pidm);
                result.StudentNewEthnicity = await GetStudentNewEthnicityAsync(result.Pidm);
                result.StudentIsEmployed = await IsStudentEmployedAsync(result.Pidm);

                if (result.StudentIsEmployed)
                {
                    result.StudentEmployeeId = await GetStudentEmployeeIdAsync(result.Pidm);
                    result.StudentEmployer = await GetStudentEmployerAsync(result.Pidm);
                }

                // Get address information
                result.StudentPermanentAddress = await GetStudentAddressAsync(result.Pidm, "PR");
                result.StudentMailingAddress = await GetStudentAddressAsync(result.Pidm, "MA");
                result.StudentBillingAddress = await GetStudentAddressAsync(result.Pidm, "BI");

                // Get phone information
                result.StudentPermanentPhone = await GetStudentPhoneAsync(result.Pidm, "PR");
                result.StudentMailingPhone = await GetStudentPhoneAsync(result.Pidm, "MA");
                result.StudentBillingPhone = await GetStudentPhoneAsync(result.Pidm, "BI");
                
                if (!string.IsNullOrEmpty(currentTerm))
                {
                    // Get term-dependent information
                    result.StudentTermDescription = await GetStudentTermDescriptionAsync(currentTerm);
                    result.StudentStatus = await GetStudentStatusAsync(currentTerm, result.Pidm);
                    result.StudentRegistrationStatus = await GetStudentRegistrationStatusAsync(currentTerm, result.Pidm);
                    result.StudentPrimaryMajor = await GetStudentMajorAsync(currentTerm, result.Pidm);
                    result.StudentAllMajors = await GetStudentAllMajorsAsync(currentTerm, result.Pidm);
                    result.StudentClassLevel = await GetStudentClassLevelAsync(currentTerm, result.Pidm);
                    result.StudentClassOf = await GetStudentClassOfAsync(currentTerm, result.Pidm);
                    result.StudentDegreeSought = await GetStudentDegreeSoughtAsync(currentTerm, result.Pidm);
                    result.StudentIsDualDegree = await IsStudentDualDegreeAsync(currentTerm, result.Pidm);
                    result.StudentIsDVM = await IsStudentDVMAsync(currentTerm, result.Pidm);
                    result.StudentIsMPVM = await IsStudentMPVMAsync(currentTerm, result.Pidm);
                    result.StudentIsCAResident = await IsStudentCAResidentAsync(currentTerm, result.Pidm);
                    result.StudentIsUSCitizen = await IsStudentUSCitizenAsync(result.Pidm);

                    // Get admit term for the primary major
                    if (!string.IsNullOrEmpty(result.StudentPrimaryMajor))
                    {
                        result.StudentAdmitTerm = await GetStudentAdmitTermAsync(result.Pidm, result.StudentPrimaryMajor);
                    }

                    // Get GPA and class rank for the primary major
                    if (!string.IsNullOrEmpty(result.StudentPrimaryMajor))
                    {
                        result.StudentCumulativeGPA = await GetStudentCumulativeGPAAsync(result.Pidm, currentTerm, result.StudentPrimaryMajor);
                        result.StudentClassRank = await GetStudentClassRankAsync(result.Pidm, result.StudentPrimaryMajor);
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Get current or future term for student - equivalent to getCurrentOrFutureTermForUser in SIS.cfc
        /// </summary>
        private async Task<string?> GetCurrentOrFutureTermForStudentAsync(string pidm)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<TermResult>("EXEC AAUD.dbo.usp_get_CurrentOrFutureTermForUser @pidm = {0}, @loginID = NULL", pidm)
                    .FirstOrDefaultAsync();
                
                return result?.TermCode;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get student prior names - equivalent to getPriorName in SIS.cfc
        /// </summary>
        private async Task<string?> GetStudentPriorNamesAsync(string pidm)
        {
            try
            {
                var nameList = await _aaudContext.Database
                    .SqlQueryRaw<PriorNameResult>("EXEC usp_sis_getPriorName @thisPidm = {0}", pidm)
                    .ToListAsync();

                if (nameList.Any())
                {
                    var names = nameList
                        .Where(name => !string.IsNullOrEmpty(name.StudentName) && name.ActivityDate.HasValue)
                        .Select(name => $"{name.StudentName} ({name.ActivityDate:MM/dd/yyyy})")
                        .ToList();
                    return string.Join(", ", names);
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get student Banner ID - equivalent to getBannerID in SIS.cfc
        /// </summary>
        private async Task<string?> GetStudentBannerIdAsync(string pidm)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<BannerIdResult>("EXEC usp_sis_getBannerID @pidm = {0}", pidm)
                    .FirstOrDefaultAsync();
                
                return result?.SpridenId;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Check if student is confidential - equivalent to isConfidential in SIS.cfc
        /// </summary>
        private async Task<bool> IsStudentConfidentialAsync(string pidm)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<ConfidentialResult>("EXEC usp_sis_isConfidential @pidm = {0}", pidm)
                    .FirstOrDefaultAsync();
                
                return !string.IsNullOrEmpty(result?.SpbpersConfidInd);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get student status for term - equivalent to getStudentStatus in SIS.cfc
        /// </summary>
        private async Task<string?> GetStudentStatusAsync(string termCode, string pidm)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<StudentStatusResult>("EXEC usp_sis_getStudentStatus @thisTermCode = {0}, @thispidm = {1}", termCode, pidm)
                    .FirstOrDefaultAsync();
                
                return result?.RegStatus;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get student registration status - equivalent to getRegStatus in SIS.cfc
        /// </summary>
        private async Task<string?> GetStudentRegistrationStatusAsync(string termCode, string pidm)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<RegistrationStatusResult>("EXEC usp_sis_getCurrentRegStatus @termCode = {0}, @pidm = {1}", termCode, pidm)
                    .ToListAsync();
                
                return result.Any() ? "Yes" : "No";
            }
            catch
            {
                return "No";
            }
        }

        /// <summary>
        /// Get student primary major - equivalent to getMajor in SIS.cfc
        /// </summary>
        private async Task<string?> GetStudentMajorAsync(string termCode, string pidm)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<MajorResult>("EXEC usp_sis_getMajor @termCode = {0}, @pidm = {1}", termCode, pidm)
                    .FirstOrDefaultAsync();
                
                return result?.SgbstdnMajrCode1;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get all student majors - equivalent to getAllMajors in SIS.cfc
        /// </summary>
        private async Task<string?> GetStudentAllMajorsAsync(string termCode, string pidm)
        {
            var result = await _aaudContext.Database
                .SqlQueryRaw<AllMajorsResult>("EXEC usp_sis_getAllMajors @termCode = {0}, @pidm = {1}", termCode, pidm)
                .FirstOrDefaultAsync();

            if (result != null)
            {
                var majors = new List<string>();
                if (!string.IsNullOrEmpty(result.SgbstdnMajrCode1))
                    majors.Add(result.SgbstdnMajrCode1);
                if (!string.IsNullOrEmpty(result.SgbstdnMajrCode2))
                    majors.Add(result.SgbstdnMajrCode2);
                    
                return string.Join(", ", majors);
            }
                
            return null;
        }

        /// <summary>
        /// Get student class level - equivalent to getClassLevel in SIS.cfc
        /// </summary>
        private async Task<string?> GetStudentClassLevelAsync(string termCode, string pidm)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<ClassLevelResult>("EXEC usp_sis_getClassLevel @thisTermCode = {0}, @thisPidm = {1}", termCode, pidm)
                    .FirstOrDefaultAsync();
                
                return result?.SgvclssClasCode;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get student class of year - equivalent to getClassOf in SIS.cfc
        /// </summary>
        private async Task<string?> GetStudentClassOfAsync(string termCode, string pidm)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<ClassOfResult>("EXEC usp_sis_getClassOf @thisTermCode = {0}, @thisPidm = {1}", termCode, pidm)
                    .FirstOrDefaultAsync();
                
                return result?.ClassOf;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get student confidential scope - equivalent to getConfidentialScope in SIS.cfc
        /// </summary>
        private async Task<string?> GetStudentConfidentialScopeAsync(string pidm)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<ConfidentialScopeResult>("EXEC usp_sis_getConfidentialScope @Pidm = {0}", pidm)
                    .FirstOrDefaultAsync();
                
                return result?.ZtvconfDesc;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get student birth date - equivalent to getBirthDate in SIS.cfc
        /// </summary>
        private async Task<string?> GetStudentBirthDateAsync(string pidm)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<BirthDateResult>("EXEC usp_sis_getBirthDate @pidm = {0}", pidm)
                    .FirstOrDefaultAsync();
                
                return result?.BirthDate;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get student age - equivalent to getAge in SIS.cfc
        /// </summary>
        private async Task<string?> GetStudentAgeAsync(string pidm)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<AgeResult>("EXEC usp_sis_getAge @pidm = {0}", pidm)
                    .FirstOrDefaultAsync();
                
                return result?.Age;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get term description - equivalent to getTermDesc in SIS.cfc
        /// </summary>
        private async Task<string?> GetStudentTermDescriptionAsync(string termCode)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<TermDescResult>("EXEC usp_sis_getTermDescription @thisTermCode = {0}", termCode)
                    .FirstOrDefaultAsync();
                
                return result?.TermDesc;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get degree sought - equivalent to getDegreeSought in SIS.cfc
        /// </summary>
        private async Task<string?> GetStudentDegreeSoughtAsync(string termCode, string pidm)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<DegreeSoughtResult>("EXEC usp_sis_getDegreeSought @termCode = {0}, @pidm = {1}", termCode, pidm)
                    .FirstOrDefaultAsync();

                if (result != null)
                {
                    var degrees = new List<string>();
                    if (!string.IsNullOrEmpty(result.Degree1))
                        degrees.Add(result.Degree1);
                    if (!string.IsNullOrEmpty(result.Degree2))
                        degrees.Add(result.Degree2);
                    
                    return string.Join(", ", degrees);
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get academic standing - equivalent to getAcademicStanding in SIS.cfc
        /// </summary>
        private async Task<string?> GetStudentAcademicStandingAsync(string pidm)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<AcademicStandingResult>("EXEC usp_sis_getCurrentacademicStanding @pidm = {0}", pidm)
                    .FirstOrDefaultAsync();
                
                return result?.SgvstdnAstdDesc;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get cumulative GPA - equivalent to getCumulativeGPA in SIS.cfc
        /// </summary>
        private async Task<string?> GetStudentCumulativeGPAAsync(string pidm, string termCode, string majorCode)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<GPAResult>("EXEC usp_sis_getCumulativeGPA @pidm = {0}, @termCode = {1}, @majorCode = {2}", pidm, termCode, majorCode)
                    .FirstOrDefaultAsync();
                
                return result?.Gpa;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get class rank - equivalent to getClassRank in SIS.cfc
        /// </summary>
        private async Task<string?> GetStudentClassRankAsync(string pidm, string majorCode)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<ClassRankResult>("EXEC usp_sis_getClassRank @Pidm = {0}, @majorCode = {1}", pidm, majorCode)
                    .FirstOrDefaultAsync();
                
                return result?.ClassRank;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get admit class year - equivalent to getAdmitClassYear in SIS.cfc
        /// </summary>
        private async Task<string?> GetStudentAdmitClassYearAsync(string pidm)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<AdmitClassYearResult>("EXEC usp_sis_getAdmitClassYear @pidm = {0}", pidm)
                    .FirstOrDefaultAsync();
                
                return result?.AdmitClassYear;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get admit term - equivalent to getAdmitTerm in SIS.cfc
        /// </summary>
        private async Task<string?> GetStudentAdmitTermAsync(string pidm, string major)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<AdmitTermResult>("EXEC usp_sis_getAdmitTerm @pidm = {0}, @major = {1}", pidm, major)
                    .FirstOrDefaultAsync();
                
                return result?.AdmitTerm;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Check if dual degree student - equivalent to isDualDegreeStudent in SIS.cfc
        /// </summary>
        private async Task<bool> IsStudentDualDegreeAsync(string termCode, string pidm)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<DualDegreeResult>("EXEC usp_sis_isDualDegreeStudent @thisTermCode = {0}, @thisPidm = {1}", termCode, pidm)
                    .FirstOrDefaultAsync();
                
                return result?.IsDualDegree == "Yes";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if DVM student - equivalent to isDVMStudent in SIS.cfc
        /// </summary>
        private async Task<bool> IsStudentDVMAsync(string termCode, string pidm)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<DVMStudentResult>("EXEC usp_sis_isDVMStudent @thisTermCode = {0}, @thisPidm = {1}", termCode, pidm)
                    .FirstOrDefaultAsync();
                
                return result?.IsDVMStudent == "Yes";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if MPVM student - equivalent to isMPVMStudent in SIS.cfc
        /// </summary>
        private async Task<bool> IsStudentMPVMAsync(string termCode, string pidm)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<MPVMStudentResult>("EXEC usp_sis_isMPVMStudent @thisTermCode = {0}, @thisPidm = {1}", termCode, pidm)
                    .FirstOrDefaultAsync();
                
                return result?.IsMPVMStudent == "Yes";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if student is employed - equivalent to isEmployed in SIS.cfc
        /// </summary>
        private async Task<bool> IsStudentEmployedAsync(string pidm)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<EmployedResult>("EXEC usp_sis_isEmployed @pidm = {0}", pidm)
                    .FirstOrDefaultAsync();
                
                return !string.IsNullOrEmpty(result?.WobeucePidm);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get student employee ID - equivalent to getEmployeeID in SIS.cfc
        /// </summary>
        private async Task<string?> GetStudentEmployeeIdAsync(string pidm)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<StudentEmployeeIdResult>("EXEC usp_sis_getEmployeeID @thisPidm = {0}", pidm)
                    .FirstOrDefaultAsync();
                
                return result?.EmployeeId;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get student employer - equivalent to getEmployer in SIS.cfc
        /// </summary>
        private async Task<string?> GetStudentEmployerAsync(string pidm)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<EmployerResult>("EXEC usp_sis_getEmployer @pidm = {0}", pidm)
                    .FirstOrDefaultAsync();
                
                return result?.WobeuceDeptName;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get student gender - equivalent to getGender in SIS.cfc
        /// </summary>
        private async Task<string?> GetStudentGenderAsync(string pidm)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<GenderResult>("EXEC usp_sis_getGender @pidm = {0}", pidm)
                    .FirstOrDefaultAsync();
                
                return result?.Gender;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get student ethnicity - equivalent to getEthnicity in SIS.cfc
        /// </summary>
        private async Task<string?> GetStudentEthnicityAsync(string pidm)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<EthnicityResult>("EXEC usp_sis_getEthnicity @pidm = {0}", pidm)
                    .FirstOrDefaultAsync();
                
                return result?.Ethnicity;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get student new ethnicity - equivalent to getNewEthnicity in SIS.cfc
        /// </summary>
        private async Task<string?> GetStudentNewEthnicityAsync(string pidm)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<NewEthnicityResult>("EXEC usp_sis_getNewEthnicity @pidm = {0}", pidm)
                    .FirstOrDefaultAsync();
                
                return result?.NewEthnicity;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Check if CA resident - equivalent to isCAResident in SIS.cfc
        /// </summary>
        private async Task<bool> IsStudentCAResidentAsync(string termCode, string pidm)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<CAResidentResult>("EXEC usp_sis_isCAResident @TermCode = {0}, @Pidm = {1}", termCode, pidm)
                    .FirstOrDefaultAsync();
                
                return result?.ResidentFlag == "Y";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if US citizen - equivalent to isUSCitizen in SIS.cfc
        /// </summary>
        private async Task<bool> IsStudentUSCitizenAsync(string pidm)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<USCitizenResult>("EXEC usp_sis_isUSCitizen @Pidm = {0}", pidm)
                    .FirstOrDefaultAsync();
                
                return result?.CitizenFlag == "Y";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get student address - equivalent to getAddress in SIS.cfc
        /// </summary>
        private async Task<string?> GetStudentAddressAsync(string pidm, string type)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<AddressResult>("EXEC usp_sis_getAddress @pidm = {0}, @type = {1}", pidm, type)
                    .FirstOrDefaultAsync();
                
                return result?.Address;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get student phone - equivalent to getPhone in SIS.cfc
        /// </summary>
        private async Task<string?> GetStudentPhoneAsync(string pidm, string type)
        {
            try
            {
                var result = await _aaudContext.Database
                    .SqlQueryRaw<PhoneResult>("EXEC usp_sis_getPhone @pidm = {0}, @type = {1}", pidm, type)
                    .FirstOrDefaultAsync();
                
                return result?.Phone;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Populate iam information
        /// </summary>
        private async Task PopulateIamInfoAsync(UserInfoResult result)
        {
            if (string.IsNullOrEmpty(result.IamId))
            {
                Console.WriteLine("IAM API: result.IamId is null or empty");
                return;
            }

            try
            {
                Console.WriteLine($"IAM API Request for IamId: {result.IamId}");
                var iamApi = new IamApi(_httpClientFactory);

                // Get people information - equivalent to iamPeople.getById() in ColdFusion
                var peopleResponse = await iamApi.SearchForPerson(iamId: result.IamId);
                Console.WriteLine($"IAM People Response Data: {(peopleResponse.Data != null ? peopleResponse.Data.Count() : "null")}, Error: {peopleResponse.ErrorMessage ?? "none"}");
                if (peopleResponse.Data?.Any() == true)
                {
                    result.IamPeople = peopleResponse.Data.ToList();
                    var person = peopleResponse.Data.First();
                    result.PPSId = person.PpsId;
                    result.OFullName = person.OFullName;
                    result.IsHSEmployee = person.IsHSEmployee;
                    result.IsFaculty = person.IsFaculty;
                    result.IsStaff = person.IsStaff;
                    result.IsExternal = person.IsExternal;
                }

                // Get employee associations - equivalent to iamAssociations.getEmployeeAssociations() in ColdFusion
                var associationsResponse = await iamApi.GetEmployeeAssociations(result.IamId);
                Console.WriteLine($"IAM Associations Response Data: {(associationsResponse.Data != null ? associationsResponse.Data.Count() : "null")}, Error: {associationsResponse.ErrorMessage ?? "none"}");
                if (associationsResponse.Data?.Any() == true)
                {
                    result.IamAssociations = associationsResponse.Data.ToList();
                    var association = associationsResponse.Data.First(); // Get first/primary association
                    result.AssociationsTitle = association.TitleDisplayName;
                    result.AssociationsTitleCode = association.TitleCode;
                    result.AssociationsDepartment = association.DeptDisplayName;
                    result.AssociationsDepartmentCode = association.DeptCode;
                    result.AssociationsAdminDepartment = association.AdminDeptDisplayName;
                    result.AssociationsAdminDepartmentAbbrev = association.AdminDeptAbbrev;
                    result.AssociationsAdminDepartmentCode = association.AdminDeptCode;
                    result.AssociationsAppointmentDepartment = association.ApptDeptDisplayName;
                    result.AssociationsAppointmentDepartmentAbbrev = association.ApptDeptAbbrev;
                    result.AssociationsAppointmentDepartmentCode = association.ApptDeptCode;
                    result.AssociationsPositionType = association.PositionType;
                    result.AssociationsEmployeeClass = association.EmplClassDesc;
                    result.AssociationsPercentFulltime = association.PercentFullTime;
                    result.AssociationsStartDate = association.AssocStartDate;
                    result.AssociationsEndDate = association.AssocEndDate;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"IAM API EXCEPTION: {ex}");
                // Log exception but don't fail the entire request
            }
        }

        private async Task PopulateSystemRolesAsync(UserInfoResult result)
        {
            if (string.IsNullOrEmpty(result.MothraId))
                return;

            var systems = new[] { "VIPER", "VMACS.VMTH", "VMACS.VMLF", "VMACS.UCVMCSD" };

            // Query tblRoleMembers and join tblRoles for this user
            var roleMembers = await _rapsContext.TblRoleMembers
                .Include(rm => rm.Role)
                .Where(rm => rm.MemberId == result.MothraId && rm.ViewName == null)
                .ToListAsync();

            foreach (var system in systems)
            {
                // Filter roles belonging to the current system/instance
                var filtered = roleMembers
                    .Where(rm => rm.Role != null && RAPSSecurityService.RoleBelongsToInstance(system, rm.Role))
                    .OrderBy(rm => rm.Role.DisplayName ?? rm.Role.Role)
                    .ToList();

                result.SystemRoles.AddRange(filtered.Select(rm => new SystemRole
                {
                    System = system,
                    DisplayName = FormatPermissionName(rm.Role.DisplayName ?? rm.Role.Role)
                }));
            }

            var categories = new[] { "API", "RAPS", "SVMSecure", "VIPERForms", "VMACS" };
            foreach (var category in categories)
            {
                var categoryPerms = await GetUserPermissionsForSystemAsync(result.MothraId, category);
                var sysPerm = new SystemPermission
                {
                    Category = category,
                    Count = categoryPerms.Count,
                    Permissions = categoryPerms.Select(p => p.Permission).ToList()
                };
                result.SystemPermissions.Add(sysPerm);
            }
        }

        /// <summary>
        /// Get RSOP (Resultant Set of Permissions) for a user in a specific system
        /// </summary>
        private async Task<List<PermissionInfo>> GetUserPermissionsForSystemAsync(string memberId, string systemPrefix)
        {
            var permsViaRoles = await (
                from permission in _rapsContext.TblPermissions
                join rolePermissions in _rapsContext.TblRolePermissions
                    on permission.PermissionId equals rolePermissions.PermissionId
                join memberRole in _rapsContext.TblRoleMembers
                    on rolePermissions.RoleId equals memberRole.RoleId
                join role in _rapsContext.TblRoles
                    on memberRole.RoleId equals role.RoleId
                where memberRole.MemberId == memberId
                && (memberRole.StartDate == null || memberRole.StartDate <= DateTime.Today)
                && (memberRole.EndDate == null || memberRole.EndDate >= DateTime.Today)
                select new
                {
                    permission.PermissionId,
                    permission.Permission,
                    rolePermissions.Access,
                    role.Role
                }).ToListAsync();

            var permsAssigned = await (from permission in _rapsContext.TblPermissions
                                       join memberPermissions in _rapsContext.TblMemberPermissions
                                           on permission.PermissionId equals memberPermissions.PermissionId
                                       where memberPermissions.MemberId == memberId
                                       && (memberPermissions.StartDate == null || memberPermissions.StartDate <= DateTime.Today)
                                       && (memberPermissions.EndDate == null || memberPermissions.EndDate >= DateTime.Today)
                                       select new
                                       {
                                           permission.PermissionId,
                                           permission.Permission,
                                           memberPermissions.Access
                                       }).ToListAsync();

            var permissions = new Dictionary<int, PermissionInfo>();

            // Add permissions assigned via roles
            foreach (var p in permsViaRoles)
            {
                if (permissions.TryGetValue(p.PermissionId, out PermissionInfo? existing))
                {
                    // Record deny if this role is denying access
                    if (existing.Access == "1" && p.Access == 0)
                    {
                        existing.Access = "0";
                        existing.Source = p.Role;
                    }
                    else if (existing.Access == p.Access.ToString())
                    {
                        existing.Source += "," + p.Role;
                    }
                }
                else
                {
                    permissions[p.PermissionId] = new PermissionInfo
                    {
                        PermissionId = p.PermissionId,
                        Permission = p.Permission,
                        Source = p.Role,
                        SourceType = "Role",
                        Access = p.Access.ToString()
                    };
                }
            }

            // Add permissions assigned directly
            foreach (var p in permsAssigned)
            {
                if (permissions.TryGetValue(p.PermissionId, out PermissionInfo? existing))
                {
                    if (existing.Access == "1" && p.Access == 0)
                    {
                        existing.Access = "0";
                        existing.Source = "Member Permission";
                    }
                    else if (existing.Access == p.Access.ToString())
                    {
                        existing.Source += ",Member Permission";
                    }
                }
                else
                {
                    permissions[p.PermissionId] = new PermissionInfo
                    {
                        PermissionId = p.PermissionId,
                        Permission = p.Permission,
                        Source = "Member Permission",
                        SourceType = "Member",
                        Access = p.Access.ToString()
                    };
                }
            }

            // Filter to only allowed permissions starting with the systemPrefix, sorted by name
            return permissions.Values
                .Where(p => p.Access == "1" && p.Permission.StartsWith(systemPrefix))
                .OrderBy(p => p.Permission)
                .ToList();
        }

        private static string FormatPermissionName(string val)
        {
            if (string.IsNullOrEmpty(val)) return "";
            string clean = val;
            clean = clean.Replace("CN=", "", StringComparison.OrdinalIgnoreCase);
            clean = clean.Replace("OU=", "", StringComparison.OrdinalIgnoreCase);
            clean = clean.Replace("DC=", "", StringComparison.OrdinalIgnoreCase);
            return clean.Replace(",", ".");
        }


        /// <summary>
        /// Populate UC Path information
        /// </summary>
        private async Task PopulateUCPathInfoAsync(UserInfoResult result)
        {
            if (string.IsNullOrEmpty(result.EmployeeId))
                return;

            try
            {
                // Get UC Path person information
                var person = await _ppsContext.VwPeople
                    .Where(p => p.Emplid == result.EmployeeId)
                    .FirstOrDefaultAsync();

                if (person != null)
                {
                    result.UCPathFlags = GetUCPathFlags(person);
                }

                // Get UC Path position information
                var position = await _ppsContext.VwPersonJobPositions
                    .Where(p => p.Emplid == result.EmployeeId)
                    .OrderByDescending(p => p.Effdt)
                    .FirstOrDefaultAsync();

                if (position != null)
                {
                    result.UCPathJobCode = position.Jobcode;
                    result.UCPathJobDescription = position.JobcodeDesc;
                    result.UCPathDepartmentId = position.Deptid;
                    result.UCPathDepartmentDescription = position.DeptDesc;
                    result.UCPathJobStatus = position.JobStatus;
                    result.UCPathEmployeeStatus = position.EmplStatus;
                    result.UCPathJobStatusDescription = position.JobStatusDesc;
                    result.UCPathPositionEffectiveDate = position.PositionEffdt;
                    result.UCPathExpectedEndDate = position.ExpectedEndDate;
                    result.UCPathFTE = position.Fte;
                    result.UCPathUnion = position.UnionCd;

                    // Get reports to information
                    if (!string.IsNullOrEmpty(position.ReportsTo))
                    {
                        var reportsTo = await _ppsContext.VwPersonJobPositions
                            .Where(r => r.PositionNbr == position.ReportsTo)
                            .FirstOrDefaultAsync();

                        if (reportsTo != null)
                        {
                            result.UCPathReportsToName = $"{reportsTo.FirstName} {reportsTo.LastName}".Trim();
                            result.UCPathReportsToPosition = reportsTo.JobcodeDesc;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: PopulateUCPathInfoAsync failed: {ex.Message}");
            }

            // Get UC Path History from the VwPersonJobPositionAll view
            await PopulateUCPathHistoryAsync(result);
        }

        /// <summary>
        /// Populate UC Path History information - equivalent to get_ucpath_history in userinfo.cfc
        /// </summary>
        private async Task PopulateUCPathHistoryAsync(UserInfoResult result)
        {
            if (string.IsNullOrEmpty(result.EmployeeId))
                return;

            try
            {
                var historyData = await _ppsContext.VwPersonJobPositionAlls
                    .Where(p => p.Emplid == result.EmployeeId)
                    .OrderByDescending(p => p.PositionEffdt)
                    .ThenByDescending(p => p.Effdt)
                    .ToListAsync();

                result.UCPathHistory.AddRange(historyData.Select(history => new UCPathResult
                {
                    JobCode = history.Jobcode,
                    JobCodeDescription = history.JobcodeDesc,
                    DepartmentId = history.Deptid,
                    DepartmentDescription = history.DeptDesc,
                    ActionDescription = history.ActionDescr,
                    PositionEffectiveDate = history.PositionEffdt.HasValue ? DateOnly.FromDateTime(history.PositionEffdt.Value) : null,
                    ReportsTo = GetReportsToName(history),
                    ReportsToPosition = GetReportsToPosition(history)
                }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: PopulateUCPathHistoryAsync failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Get reports to name from UC Path history record
        /// </summary>
        private string GetReportsToName(VwPersonJobPositionAll history)
        {
            if (string.IsNullOrEmpty(history.ReportsTo))
                return string.Empty;

            try
            {
                // Try to find the reports to person in the same view
                var reportsTo = _ppsContext.VwPersonJobPositionAlls
                    .Where(r => r.PositionNbr == history.ReportsTo)
                    .FirstOrDefault();

                if (reportsTo != null)
                {
                    return $"{reportsTo.FirstName} {reportsTo.LastName}".Trim();
                }

                // Fallback to current positions view
                var currentReportsTo = _ppsContext.VwPersonJobPositions
                    .Where(r => r.PositionNbr == history.ReportsTo)
                    .FirstOrDefault();

                if (currentReportsTo != null)
                {
                    return $"{currentReportsTo.FirstName} {currentReportsTo.LastName}".Trim();
                }
            }
            catch
            {
                // Return empty string on any error
            }

            return string.Empty;
        }

        /// <summary>
        /// Get reports to position from UC Path history record
        /// </summary>
        private string GetReportsToPosition(VwPersonJobPositionAll history)
        {
            if (string.IsNullOrEmpty(history.ReportsTo))
                return string.Empty;

            try
            {
                // Try to find the reports to position in the same view
                var reportsTo = _ppsContext.VwPersonJobPositionAlls
                    .Where(r => r.PositionNbr == history.ReportsTo)
                    .FirstOrDefault();

                if (reportsTo != null)
                {
                    return reportsTo.JobcodeDesc ?? string.Empty;
                }

                // Fallback to current positions view
                var currentReportsTo = _ppsContext.VwPersonJobPositions
                    .Where(r => r.PositionNbr == history.ReportsTo)
                    .FirstOrDefault();

                if (currentReportsTo != null)
                {
                    return currentReportsTo.JobcodeDesc ?? string.Empty;
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        /// <summary>
        /// Populate ID Cards information
        /// </summary>
        private async Task PopulateIDCardsAsync(UserInfoResult result)
        {
            try
            {
                var cards = await (from card in _idCardsContext.IdCards
                                   join status in _idCardsContext.DvtCardStatuses
                                       on card.IdCardCurrentStatus equals status.DvtStatusCode into statusJoin
                                   from status in statusJoin.DefaultIfEmpty()
                                   join reason in _idCardsContext.DvtReasons
                                       on card.IdcardDeactivatedReason equals reason.DvtReasonCode into reasonJoin
                                   from reason in reasonJoin.DefaultIfEmpty()
                                   where card.IdCardLoginId == result.LoginId
                                   orderby card.IdCardAppliedDate descending
                                   select new
                                   {
                                       Card = card,
                                       StatusDescription = status != null ? status.DvtStatusDesc : "",
                                       DeactivatedReasonDescription = reason != null ? reason.DvtReasonDesc : ""
                                   }).ToListAsync();

                foreach (var item in cards)
                {
                    var card = item.Card;
                    result.IDCards.Add(new IDCardResult
                    {
                        Number = card.IdCardNumber?.ToString(),
                        DisplayName = card.IdCardDisplayName,
                        LastName = card.IdCardLastName,
                        Line2 = card.IdCardLine2,
                        StatusDescription = item.StatusDescription,
                        DeactivatedReason = item.DeactivatedReasonDescription,
                        Applied = card.IdCardAppliedDate,
                        Issued = card.IdCardIssueDate,
                        Deactivated = card.IdcardDeactivatedDate
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: PopulateIDCardsAsync failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Populate Keys information
        /// </summary>
        private async Task PopulateKeysAsync(UserInfoResult result)
        {
            try
            {
                var keyAssignments = await (from ka in _keysContext.KeyAssignments
                                            join k in _keysContext.Keys on ka.KeyId equals k.KeyId
                                            where ka.AssignedTo == result.MothraId && ka.Deleted == null
                                            orderby ka.IssuedDate descending, ka.KeyId
                                            select new { Assignment = ka, Key = k })
                                            .ToListAsync();

                foreach (var item in keyAssignments)
                {
                    // Get issuer information from AAUD
                    var issuer = await _aaudContext.AaudUsers
                        .Where(u => u.MothraId == item.Assignment.IssuedBy)
                        .FirstOrDefaultAsync();

                    result.Keys.Add(new KeyResult
                    {
                        AccessDescription = item.Key.AccessDescription,
                        KeyNumber = item.Key.KeyNumber,
                        CutNumber = item.Assignment.CutNumber,
                        IssuedDate = item.Assignment.IssuedDate,
                        IssuedBy = issuer?.DisplayFullName
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: PopulateKeysAsync failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Populate Loans information
        /// </summary>
        private async Task PopulateLoansAsync(UserInfoResult result)
        {
            try
            {
                var loans = await _equipmentLoanContext.Loans
                    .Where(l => l.LoanPidm == result.Pidm)
                    .Include(l => l.LoanItems)
                    .ThenInclude(li => li.LoanitemAsset)
                    .OrderByDescending(l => l.LoanDate)
                    .ToListAsync();

                foreach (var loan in loans)
                {
                    foreach (var loanItem in loan.LoanItems)
                    {
                        result.Loans.Add(new LoanResult
                        {
                            AssetName = loanItem.LoanitemAsset?.AssetName,
                            LoanDate = loan.LoanDate,
                            DueDate = loan.LoanDueDate,
                            Comments = loan.LoanComments
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: PopulateLoansAsync failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Populate Instinct information
        /// </summary>
        private async Task PopulateInstinctInfoAsync(UserInfoResult result, AaudUser user)
        {
            try
            {
                var instinctResult = await GetInstinctUserAsync(user.LastName, user.FirstName, user.MiddleName);
                result.InstinctInfo = instinctResult;
                
                if (instinctResult.Valid)
                {
                    result.InstinctId = instinctResult.InstinctId;
                    result.InstinctUsername = instinctResult.Username;
                    result.InstinctRoles = instinctResult.Roles;
                    result.InstinctStatus = instinctResult.Status;
                    result.InstinctIsActive = instinctResult.IsActive;
                    
                    if (!string.IsNullOrEmpty(instinctResult.PasswordExpiresAt) && 
                        DateTime.TryParse(instinctResult.PasswordExpiresAt, out var expireDate))
                    {
                        result.InstinctPasswordExpiresAt = expireDate;
                    }
                }
            }
            catch (Exception ex)
            {
                result.InstinctInfo = new InstinctResult { ErrorMessage = $"Populate Exception: {ex.Message}" };
            }
        }

        private static string AdClean(string a)
        {
            if (string.IsNullOrEmpty(a)) return "";
            var toReturn = a;
            toReturn = toReturn.Replace("CN=", "", StringComparison.OrdinalIgnoreCase);
            toReturn = toReturn.Replace("OU=", "", StringComparison.OrdinalIgnoreCase);
            toReturn = toReturn.Replace("DC=", "", StringComparison.OrdinalIgnoreCase);
            return toReturn;
        }

        private static string PermFormat(string a)
        {
            return AdClean(a).Replace(",", ".");
        }

        private static string AdFormat(string a, string[] domains)
        {
            var toReturn = AdClean(a);
            foreach (var d in domains)
            {
                var domainWithCommas = d.Replace(".", ",");
                toReturn = toReturn.Replace(domainWithCommas, d, StringComparison.OrdinalIgnoreCase);
            }
            var parts = toReturn.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(p => p.Trim())
                                .Reverse()
                                .ToList();
            return string.Join("/", parts);
        }

        /// <summary>
        /// Populate Active Directory information
        /// </summary>
        private async Task PopulateActiveDirectoryInfoAsync(UserInfoResult result)
        {
            if (string.IsNullOrEmpty(result.LoginId))
            {
                return;
            }

            try
            {
                var uinformService = new UinformService();
                var adUser = await uinformService.GetUser(samAccountName: result.LoginId);
                if (adUser != null && !string.IsNullOrEmpty(adUser.SamAccountName))
                {
                    result.ADDisplayName = adUser.DisplayName;
                    result.ADMail = adUser.Mail;
                    result.ADSamAccountName = adUser.SamAccountName;
                    result.ADUserPrincipalName = adUser.UserPrincipalName;
                    result.ADDistinguishedName = PermFormat(adUser.DistinguishedName ?? "");

                    var allGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (adUser.MemberOf != null)
                    {
                        foreach (var g in adUser.MemberOf)
                        {
                            allGroups.Add(g);
                        }
                    }
                    if (adUser.MemberOfAll != null)
                    {
                        foreach (var g in adUser.MemberOfAll)
                        {
                            allGroups.Add(g);
                        }
                    }

                    var isProd = HttpHelper.Environment?.IsProduction() ?? false;
                    var domains = isProd
                        ? new[] { "ad3.ucdavis.edu", "ou.ad3.ucdavis.edu", "ucsvm.ucdavis.edu", "ad.vmth.ucdavis.edu", "vetmed.ucdavis.edu", "svm.ucdavis.edu" }
                        : new[] { "t3.ucdavis.edu" };
                    result.ADMemberOf.AddRange(allGroups
                        .Select(groupDn => AdFormat(groupDn, domains))
                        .Where(formattedGroup => !string.IsNullOrEmpty(formattedGroup)));

                    // Sort the groups
                    result.ADMemberOf = result.ADMemberOf.OrderBy(g => g, StringComparer.OrdinalIgnoreCase).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error populating AD info: {ex.Message}");
            }
        }

        /// <summary>
        /// Get user photo data
        /// </summary>
        public async Task<byte[]?> GetUserPhotoAsync(string mailId, bool useAltPhoto = false)
        {
            // stubbed
            return null;
        }




        /// <summary>
        /// Get Instinct user information via GraphQL API
        /// </summary>
        private async Task<InstinctResult> GetInstinctUserAsync(string lastName, string firstName, string? middleName)
        {
            var result = new InstinctResult();
            
            // Get access token
            var accessToken = await GetInstinctAccessTokenAsync(result);
            if (string.IsNullOrEmpty(accessToken))
            {
                return result;
            }

            // Build name variations for matching
            var nameVariations = new List<string> { firstName };
            
            var firstNameParts = firstName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var first = firstNameParts.FirstOrDefault() ?? "";
            
            if (firstNameParts.Length > 0 && !nameVariations.Contains(first))
            {
                nameVariations.Add(first);
            }
            
            if (firstNameParts.Length > 1)
            {
                var accum = first;
                for (int i = 1; i < firstNameParts.Length; i++)
                {
                    if (firstNameParts[i].Length > 0)
                    {
                        accum += " " + firstNameParts[i][0];
                        if (!nameVariations.Contains(accum))
                        {
                            nameVariations.Add(accum);
                        }
                    }
                }
            }
            
            var temp = nameVariations.ToList();
            if (!string.IsNullOrEmpty(middleName))
            {
                var middleParts = middleName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var name in temp)
                {
                    foreach (var middlePart in middleParts.Where(middlePart => middlePart.Length > 0))
                    {
                        var variation = $"{name} {middlePart[0]}";
                        if (!nameVariations.Contains(variation))
                        {
                            nameVariations.Add(variation);
                        }
                    }
                }
            }

            // Create GraphQL query
            var query = $@"
            query {{
                searchUsers(name: ""{lastName}"") {{
                    id
                    initials
                    instinctId
                    isActive
                    isProtected
                    nameFirst
                    nameMiddle
                    nameLast
                    passwordExpiresAt
                    status
                    username
                    roles {{
                        description
                        label
                    }}
                }}
            }}";

            // Execute GraphQL query
            var apiUrl = _configuration["Instinct:ApiUrl"] ?? "https://uc-davis.api.instinctvet.com/";
            var httpClient = _httpClientFactory.CreateClient();
                
            var queryUrl = $"{apiUrl}?query={Uri.EscapeDataString(query)}";
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                
            using var response = await httpClient.GetAsync(queryUrl);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var graphqlResponse = JsonSerializer.Deserialize<InstinctGraphQLResponse>(responseContent);
                    
                if (graphqlResponse?.Data?.SearchUsers != null)
                {
                    // Find matching user by first name
                    var matchedUser = graphqlResponse.Data.SearchUsers.FirstOrDefault(user =>
                        nameVariations.Any(name => string.Equals(name, user.NameFirst, StringComparison.OrdinalIgnoreCase)));

                    if (matchedUser != null)
                    {
                        result.Valid = true;
                        result.Id = matchedUser.Id;
                        result.Initials = matchedUser.Initials;
                        result.InstinctId = matchedUser.InstinctId;
                        result.IsActive = matchedUser.IsActive;
                        result.IsProtected = matchedUser.IsProtected;
                        result.PasswordExpiresAt = matchedUser.PasswordExpiresAt;
                        result.Status = matchedUser.Status;
                        result.Username = matchedUser.Username;
                        result.Roles = matchedUser.Roles?.Select(r => r.Label).ToList() ?? new List<string>();
                    }
                    else
                    {
                        result.ErrorMessage = $"User found in API but no name match. Variations tried: {string.Join(", ", nameVariations)}. API users: {string.Join(", ", graphqlResponse.Data.SearchUsers.Select(u => $"{u.NameFirst} {u.NameLast}"))}";
                    }
                }
                else
                {
                    result.ErrorMessage = "GraphQL response contained no searchUsers data.";
                }
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                result.ErrorMessage = $"GraphQL query failed (Status: {response.StatusCode}): {responseContent}";
            }
            return result;
        }

        private static void AppendError(InstinctResult result, string msg)
        {
            result.ErrorMessage = string.IsNullOrEmpty(result.ErrorMessage)
                ? msg
                : $"{result.ErrorMessage} | {msg}";
        }

        /// <summary>
        /// OAuth access token for Instinct
        /// </summary>
        private async Task<string?> GetInstinctAccessTokenAsync(InstinctResult result)
        {
            const string cacheKey = "instinct_access_token";
            
            // Check cache first
            if (_memoryCache.TryGetValue(cacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
            {
                return cachedToken;
            }

            try
            {
                var apiUrl = _configuration["Instinct:ApiUrl"] ?? "https://uc-davis.api.instinctvet.com/";
                if (!apiUrl.EndsWith("/"))
                {
                    apiUrl += "/";
                }
                var tokenUrl = apiUrl + "auth/token";
                var username = "ucdavisapi";
                var password = HttpHelper.GetSetting<string>("Credentials", "InstinctApi") ?? "";
                
                if (string.IsNullOrEmpty(password))
                {
                    string errMsg = "Password is null or empty in configuration";
                    Console.WriteLine($"[INSTINCT AUTH] {errMsg}");
                    AppendError(result, errMsg);
                    return null;
                }

                var httpClient = _httpClientFactory.CreateClient();
                var formParams = new List<KeyValuePair<string, string>>
                {
                    new("username", username),
                    new("password", password),
                    new("grant_type", "password"),
                    new("scope", "api_access")
                };

                using var formContent = new FormUrlEncodedContent(formParams);
                Console.WriteLine("[INSTINCT AUTH] Sending token POST request...");
                using var response = await httpClient.PostAsync(tokenUrl, formContent);
                Console.WriteLine($"[INSTINCT AUTH] Response Status Code: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonSerializer.Deserialize<InstinctTokenResponse>(responseContent);
                    Console.WriteLine($"[INSTINCT AUTH] Deserialized Token Length: {tokenResponse?.AccessToken?.Length ?? 0}");
                    
                    if (tokenResponse?.AccessToken != null)
                    {
                        // Cache token for slightly less than expiry time (subtract 2 hours as in CF code)
                        var cacheExpiry = TimeSpan.FromSeconds(tokenResponse.ExpiresIn - 7200); // 2 hours buffer
                        _memoryCache.Set(cacheKey, tokenResponse.AccessToken, cacheExpiry);
                        
                        return tokenResponse.AccessToken;
                    }
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    string errMsg = $"Token POST request failed (Status: {response.StatusCode}): {responseContent}";
                    Console.WriteLine($"[INSTINCT AUTH] {errMsg}");
                    AppendError(result, errMsg);
                }
            }
            catch (Exception ex)
            {
                string errMsg = $"Token request exception: {ex.Message}";
                Console.WriteLine($"[INSTINCT AUTH] {errMsg}");
                AppendError(result, errMsg);
            }

            return null;
        }

        /// <summary>
        /// Extract UC Path flags from VwPerson entity
        /// </summary>
        private static List<string> GetUCPathFlags(dynamic person)
        {
            var flags = new List<string>();
            var flagDefinitions = new Dictionary<string, string>
            {
                {"EmpAcdmcFederationFlg", "Academic Federation"},
                {"EmpAcdmcFlg", "ACDMC"},
                {"EmpAcdmcSenateFlg", "ACDMC Senate"},
                {"EmpAcdmcStdtFlg", "ACDMC Stdt"},
                {"EmpFacultyFlg", "Faculty"},
                {"EmpLadderRankFlg", "Ladder Rank"},
                {"EmpMgrFlg", "Manager"},
                {"EmpMspCareerFlg", "MSP Career"},
                {"EmpMspCareerPartialyrFlg", "MSP Career Partial Year"},
                {"EmpMspCasualFlg", "MSP Casual"},
                {"EmpMspCntrctFlg", "MSP Contract"},
                {"EmpMspFlg", "MSP"},
                {"EmpMspSeniorMgmtFlg", "MSP Senior Management"},
                {"EmpSspCareerFlg", "SSP Career"},
                {"EmpSspCareerPartialyrFlg", "SSP Career Partial Year"},
                {"EmpSspCasualFlg", "SSP Casual"},
                {"EmpSspCasualRestrictedFlg", "SSP Casual Restricted"},
                {"EmpSspCntrctFlg", "SSP Contract"},
                {"EmpSspFlg", "SSP"},
                {"EmpSspFloaterFlg", "SSP Floater"},
                {"EmpSspPerDiemFlg", "SSP Per diem"},
                {"EmpSupvrFlg", "Supervisor"},
                {"EmpTeachingFacultyFlg", "Teaching Faculty"},
                {"EmpWosempFlg", "WOSEMP"}
            };

            var personType = person.GetType();
            foreach (var flag in flagDefinitions)
            {
                var property = personType.GetProperty(flag.Key);
                if (property != null)
                {
                    var value = property.GetValue(person)?.ToString();
                    if (value == "Y")
                    {
                        flags.Add(flag.Value);
                    }
                }
            }
            return flags;
        }
    }

    // JSON for Instinct API
    public class InstinctTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
        
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
        
        [JsonPropertyName("scope")]
        public string Scope { get; set; } = string.Empty;
    }

    public class InstinctGraphQLResponse
    {
        [JsonPropertyName("data")]
        public InstinctGraphQLData? Data { get; set; }
    }

    public class InstinctGraphQLData
    {
        [JsonPropertyName("searchUsers")]
        public List<InstinctUser>? SearchUsers { get; set; }
    }

    public class InstinctUser
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        
        [JsonPropertyName("initials")]
        public string? Initials { get; set; }
        
        [JsonPropertyName("instinctId")]
        public string? InstinctId { get; set; }
        
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }
        
        [JsonPropertyName("isProtected")]
        public bool IsProtected { get; set; }
        
        [JsonPropertyName("nameFirst")]
        public string? NameFirst { get; set; }
        
        [JsonPropertyName("nameMiddle")]
        public string? NameMiddle { get; set; }
        
        [JsonPropertyName("nameLast")]
        public string? NameLast { get; set; }
        
        [JsonPropertyName("passwordExpiresAt")]
        public string? PasswordExpiresAt { get; set; }
        
        [JsonPropertyName("status")]
        public string? Status { get; set; }
        
        [JsonPropertyName("username")]
        public string? Username { get; set; }
        
        [JsonPropertyName("roles")]
        public List<InstinctRole>? Roles { get; set; }
    }

    public class InstinctRole
    {
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("label")]
        public string? Label { get; set; }
    }

    // Helper class for permission processing
    public class PermissionInfo
    {
        public int PermissionId { get; set; }
        public string Permission { get; set; } = string.Empty;
        public string Access { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
    }

    // Result classes for student stored procedures
    public class TermResult
    {
        public string? TermCode { get; set; }
    }

    public class PriorNameResult
    {
        public string? StudentName { get; set; }
        public DateTime? ActivityDate { get; set; }
    }

    public class BannerIdResult
    {
        public string? SpridenId { get; set; }
    }

    public class ConfidentialResult
    {
        public string? SpbpersConfidInd { get; set; }
    }

    public class StudentStatusResult
    {
        public string? RegStatus { get; set; }
    }

    public class RegistrationStatusResult
    {
        public string? Status { get; set; }
    }

    public class MajorResult
    {
        public string? SgbstdnMajrCode1 { get; set; }
    }

    public class AllMajorsResult
    {
        public string? SgbstdnMajrCode1 { get; set; }
        public string? SgbstdnMajrCode2 { get; set; }
    }

    public class ClassLevelResult
    {
        public string? SgvclssClasCode { get; set; }
    }

    public class ClassOfResult
    {
        public string? ClassOf { get; set; }
    }

    // Additional result classes for comprehensive student information
    public class ConfidentialScopeResult
    {
        public string? ZtvconfDesc { get; set; }
    }

    public class BirthDateResult
    {
        public string? BirthDate { get; set; }
    }

    public class AgeResult
    {
        public string? Age { get; set; }
    }

    public class TermDescResult
    {
        public string? TermDesc { get; set; }
    }

    public class DegreeSoughtResult
    {
        public string? Degree1 { get; set; }
        public string? Degree2 { get; set; }
    }

    public class AcademicStandingResult
    {
        public string? SgvstdnAstdDesc { get; set; }
    }

    public class GPAResult
    {
        public string? Gpa { get; set; }
    }

    public class ClassRankResult
    {
        public string? ClassRank { get; set; }
    }

    public class AdmitClassYearResult
    {
        public string? AdmitClassYear { get; set; }
    }

    public class AdmitTermResult
    {
        public string? AdmitTerm { get; set; }
    }

    public class DualDegreeResult
    {
        public string? IsDualDegree { get; set; }
    }

    public class DVMStudentResult
    {
        public string? IsDVMStudent { get; set; }
    }

    public class MPVMStudentResult
    {
        public string? IsMPVMStudent { get; set; }
    }

    public class EmployedResult
    {
        public string? WobeucePidm { get; set; }
    }

    public class StudentEmployeeIdResult
    {
        public string? EmployeeId { get; set; }
    }

    public class EmployerResult
    {
        public string? WobeuceDeptName { get; set; }
    }

    public class GenderResult
    {
        public string? Gender { get; set; }
    }

    public class EthnicityResult
    {
        public string? Ethnicity { get; set; }
    }

    public class NewEthnicityResult
    {
        public string? NewEthnicity { get; set; }
    }

    public class CAResidentResult
    {
        public string? ResidentFlag { get; set; }
    }

    public class USCitizenResult
    {
        public string? CitizenFlag { get; set; }
    }

    public class AddressResult
    {
        public string? Address { get; set; }
    }

    public class PhoneResult
    {
        public string? Phone { get; set; }
    }

    public class AuthDbRecord
    {
        public string Credential { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string EncryptedString { get; set; } = string.Empty;
    }
}