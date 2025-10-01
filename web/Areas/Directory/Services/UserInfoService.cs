using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Viper.Classes.SQLContext;
using Viper.Areas.Directory.Models;
using Viper.Models.AAUD;
using Viper.Models.Courses;
using Viper.Models.EquipmentLoan;
using Viper.Models.PPS;
using Viper.Models.IDCards;
using Viper.Models.Keys;
using Viper.Models.RAPS;
using Viper.Areas.RAPS.Services;
using Viper.Classes.Utilities;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;

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

            var individual = await _aaudContext.AaudUsers.Where(u => (u.MothraId == mothraId)).FirstOrDefaultAsync();

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

            // Populate all additional information
            await PopulateDirectoryInfoAsync(result);
            await PopulateEmployeeInfoAsync(result);
            await PopulateStudentInfoAsync(result);
            await PopulateIamInfoAsync(result);
            await PopulateSystemRolesAsync(result);
            await PopulateUCPathInfoAsync(result);
            await PopulateIDCardsAsync(result);
            await PopulateKeysAsync(result);
            await PopulateLoansAsync(result);
            await PopulateInstinctInfoAsync(result, user);
            await PopulateActiveDirectoryInfoAsync(result);

            return result;
        }

        /// <summary>
        /// get data from LDAP/VMACS
        /// </summary>
        private async Task PopulateDirectoryInfoAsync(UserInfoResult result)
        {
            var ldapUser = new LdapService().GetUserByID(result.IamId);
            if (ldapUser != null)
            {
                result.Title = ldapUser.Title;
                result.Email = ldapUser.Mail;
                result.Phone = ldapUser.TelephoneNumber;
                result.Mobile = ldapUser.Mobile;
                result.PostalAddress = ldapUser.PostalAddress;
            }

            // Get VMACS information
            var vmacs = await VMACSService.Search(result.LoginId);
            if (vmacs?.item != null)
            {
                if (vmacs.item.Nextel?.Length > 0) result.Pager = vmacs.item.Nextel[0];
                if (vmacs.item.Unit?.Length > 0) result.Department = vmacs.item.Unit[0];
            }
        }

        /// <summary>
        /// get employee data
        /// </summary>
        private async Task PopulateEmployeeInfoAsync(UserInfoResult result)
        {
            if (!result.IsEmployee || string.IsNullOrEmpty(result.EmployeeId))
                return;

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

        /// <summary>
        /// get student data
        /// </summary>
        private async Task PopulateStudentInfoAsync(UserInfoResult result)
        {
            if (!result.IsStudent || string.IsNullOrEmpty(result.Pidm))
                return;

            // Stubbed for now
        }

        /// <summary>
        /// Populate iam information
        /// </summary>
        private async Task PopulateIamInfoAsync(UserInfoResult result)
        {
            // For now, leaving as placeholder for the IAM API integration
        }

        /// <summary>
        /// Populate system roles and permissions
        /// </summary>
        private async Task PopulateSystemRolesAsync(UserInfoResult result)
        {
            if (string.IsNullOrEmpty(result.MothraId))
                return;
            // stubbed until I can figure out what's wrong
        }

        /// <summary>
        /// Get RSOP (Resultant Set of Permissions) for a user in a specific system
        /// </summary>
        private async Task<List<PermissionInfo>> GetUserPermissionsForSystemAsync(string memberId, string systemPrefix)
        {
            var permissions = new List<PermissionInfo>();

            // stubbed until I can figure out what's wrong

            return permissions;
        }


        /// <summary>
        /// Populate UC Path information
        /// </summary>
        private async Task PopulateUCPathInfoAsync(UserInfoResult result)
        {
            if (string.IsNullOrEmpty(result.EmployeeId))
                return;

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

        /// <summary>
        /// Populate ID Cards information
        /// </summary>
        private async Task PopulateIDCardsAsync(UserInfoResult result)
        {
            // Use the IdCard table directly since IdcardView doesn't seem to exist
            var cards = await _idCardsContext.IdCards
                .Where(c => c.IdCardLoginId == result.LoginId)
                .OrderByDescending(c => c.IdCardAppliedDate)
                .ToListAsync();

            foreach (var card in cards)
            {
                result.IDCards.Add(new IDCardResult
                {
                    Number = card.IdCardNumber?.ToString(),
                    DisplayName = card.IdCardDisplayName,
                    LastName = card.IdCardLastName,
                    Line2 = card.IdCardLine2,
                    StatusDescription = "", // Status would need to be looked up from status table if needed
                    DeactivatedReason = "", // Would need to be looked up if needed
                    Applied = card.IdCardAppliedDate,
                    Issued = card.IdCardIssueDate,
                    Deactivated = card.IdcardDeactivatedDate
                });
            }
        }

        /// <summary>
        /// Populate Keys information
        /// </summary>
        private async Task PopulateKeysAsync(UserInfoResult result)
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

        /// <summary>
        /// Populate Loans information
        /// </summary>
        private async Task PopulateLoansAsync(UserInfoResult result)
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

        /// <summary>
        /// Populate Instinct information
        /// </summary>
        private async Task PopulateInstinctInfoAsync(UserInfoResult result, AaudUser user)
        {
            try
            {
                var instinctResult = await GetInstinctUserAsync(user.LastName, user.FirstName, user.MiddleName);
                
                if (instinctResult.Valid)
                {
                    result.InstinctInfo = instinctResult;
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
                // Log exception - instinct integration should not fail the entire request
            }
        }

        /// <summary>
        /// Populate Active Directory information
        /// </summary>
        private async Task PopulateActiveDirectoryInfoAsync(UserInfoResult result)
        {
            // stubbed
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
            var accessToken = await GetInstinctAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                return result;
            }

            // Build name variations for matching
            var nameVariations = new List<string> { firstName };
                
            // Add first word of first name if it contains spaces
            var firstNameParts = firstName.Split(' ');
            if (firstNameParts.Length > 1)
            {
                nameVariations.Add(firstNameParts[0]);
            }

            // Add variations with middle initial if middle name exists
            if (!string.IsNullOrEmpty(middleName))
            {
                var middleParts = middleName.Split(' ');
                foreach (var name in nameVariations.ToList())
                {
                    foreach (var middlePart in middleParts)
                    {
                        if (!string.IsNullOrEmpty(middlePart))
                        {
                            nameVariations.Add($"{name} {middlePart[0]}");
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
            var apiUrl = _configuration["Instinct:ApiUrl"] ?? "https://uc-davis-sandbox.api02.instinctvet.io/";
            var httpClient = _httpClientFactory.CreateClient();
                
            var queryUrl = $"{apiUrl}?query={Uri.EscapeDataString(query)}";
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                
            var response = await httpClient.GetAsync(queryUrl);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var graphqlResponse = JsonSerializer.Deserialize<InstinctGraphQLResponse>(responseContent);
                    
                if (graphqlResponse?.Data?.SearchUsers != null)
                {
                    // Find matching user by first name
                    foreach (var user in graphqlResponse.Data.SearchUsers)
                    {
                        if (nameVariations.Any(name => string.Equals(name, user.NameFirst, StringComparison.OrdinalIgnoreCase)))
                        {
                            result.Valid = true;
                            result.Id = user.Id;
                            result.Initials = user.Initials;
                            result.InstinctId = user.InstinctId;
                            result.IsActive = user.IsActive;
                            result.IsProtected = user.IsProtected;
                            result.PasswordExpiresAt = user.PasswordExpiresAt;
                            result.Status = user.Status;
                            result.Username = user.Username;
                            result.Roles = user.Roles?.Select(r => r.Label).ToList() ?? new List<string>();
                            break;
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// OAuth access token for Instinct
        /// </summary>
        private async Task<string?> GetInstinctAccessTokenAsync()
        {
            const string cacheKey = "instinct_access_token";
            
            // Check cache first
            if (_memoryCache.TryGetValue(cacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
            {
                return cachedToken;
            }

            try
            {
                var tokenUrl = "https://uc-davis-sandbox.api02.instinctvet.io/auth/token";
                var username = "ucdavisapi";
                var password = HttpHelper.GetSetting<string>("Credentials", "InstinctApi") ?? ""; 
                
                if (string.IsNullOrEmpty(password))
                {
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

                var formContent = new FormUrlEncodedContent(formParams);
                var response = await httpClient.PostAsync(tokenUrl, formContent);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonSerializer.Deserialize<InstinctTokenResponse>(responseContent);
                    
                    if (tokenResponse?.AccessToken != null)
                    {
                        // Cache token for slightly less than expiry time (subtract 2 hours as in CF code)
                        var cacheExpiry = TimeSpan.FromSeconds(tokenResponse.ExpiresIn - 7200); // 2 hours buffer
                        _memoryCache.Set(cacheKey, tokenResponse.AccessToken, cacheExpiry);
                        
                        return tokenResponse.AccessToken;
                    }
                }
            }
            catch (Exception ex)
            {
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
}