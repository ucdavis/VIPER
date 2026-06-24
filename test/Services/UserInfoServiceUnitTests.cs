using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viper.Areas.Directory.Models;
using Viper.Areas.Directory.Services;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;
using Viper.Models.AAUD;
using Viper.Models.EquipmentLoan;
using Viper.Models.IDCards;
using Viper.Models.Keys;
using Viper.Models.PPS;
using Viper.Models.RAPS;
using Viper.Models.Courses;
using Viper.Models.IAM;
using Xunit;

namespace Viper.test.Services
{
    public class UserInfoServiceUnitTests
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IConfiguration _configuration;

        public UserInfoServiceUnitTests()
        {
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            
            var configData = new Dictionary<string, string?>
            {
                { "Instinct:ApiUrl", "https://uc-davis.api.instinctvet.com/" },
                { "Credentials:InstinctApi", "dummy-password" }
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();
        }

        // Helper to construct DbContextOptions for InMemory providers
        private DbContextOptions<TContext> CreateInMemoryOptions<TContext>() where TContext : DbContext
        {
            return new DbContextOptionsBuilder<TContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        // Helper to construct a mocked IHttpClientFactory using custom handler
        private IHttpClientFactory CreateMockHttpClientFactory(Func<HttpRequestMessage, HttpResponseMessage> handlerFunc)
        {
            var factory = Substitute.For<IHttpClientFactory>();
            factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(new MockHttpMessageHandler(handlerFunc)));
            return factory;
        }

        private AaudUser CreateTestUser(
            string iamId, 
            string mothraId, 
            string loginId = "testuser", 
            string? employeeId = null, 
            string? employeePKey = null, 
            string? studentPKey = null, 
            string? firstName = "Jane", 
            string? lastName = "Doe", 
            string? middleName = null, 
            string? displayFullName = "Jane Doe",
            string? pidm = null)
        {
            return new AaudUser
            {
                IamId = iamId,
                MothraId = mothraId,
                MailId = loginId,
                LoginId = loginId,
                EmployeeId = employeeId,
                EmployeePKey = employeePKey,
                StudentPKey = studentPKey,
                Current = 1,
                ClientId = "1",
                FirstName = firstName ?? "Jane",
                LastName = lastName ?? "Doe",
                MiddleName = middleName,
                DisplayFirstName = firstName ?? "Jane",
                DisplayLastName = lastName ?? "Doe",
                DisplayFullName = displayFullName ?? "Jane Doe",
                Pidm = pidm
            };
        }

        [Fact]
        public async Task GetUserInfoAsync_UserNotFound_ReturnsNull()
        {
            // Arrange
            using var aaud = new AAUDContext(CreateInMemoryOptions<AAUDContext>());
            using var raps = new RAPSContext(CreateInMemoryOptions<RAPSContext>());
            using var courses = new CoursesContext(CreateInMemoryOptions<CoursesContext>());
            using var loans = new EquipmentLoanContext(CreateInMemoryOptions<EquipmentLoanContext>());
            using var pps = new PPSContext(CreateInMemoryOptions<PPSContext>());
            using var idcards = new IDCardsContext(CreateInMemoryOptions<IDCardsContext>());
            using var keys = new KeysContext(CreateInMemoryOptions<KeysContext>());

            var httpFactory = CreateMockHttpClientFactory(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

            var service = new UserInfoService(aaud, raps, courses, loans, pps, idcards, keys, _configuration, httpFactory, _memoryCache);

            // Act
            var result = await service.GetUserInfoAsync("non-existent-iam", "non-existent-mothra");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserInfoAsync_UserFoundByIamId_MapsProperties()
        {
            // Arrange
            var aaudOptions = CreateInMemoryOptions<AAUDContext>();
            using (var aaudSetup = new AAUDContext(aaudOptions))
            {
                aaudSetup.AaudUsers.Add(CreateTestUser("iam-123", "mothra-123", "testuser", employeeId: "emp-123", pidm: "pidm-123", displayFullName: "Test User"));
                await aaudSetup.SaveChangesAsync();
            }

            using var aaud = new AAUDContext(aaudOptions);
            using var raps = new RAPSContext(CreateInMemoryOptions<RAPSContext>());
            using var courses = new CoursesContext(CreateInMemoryOptions<CoursesContext>());
            using var loans = new EquipmentLoanContext(CreateInMemoryOptions<EquipmentLoanContext>());
            using var pps = new PPSContext(CreateInMemoryOptions<PPSContext>());
            using var idcards = new IDCardsContext(CreateInMemoryOptions<IDCardsContext>());
            using var keys = new KeysContext(CreateInMemoryOptions<KeysContext>());

            var httpFactory = CreateMockHttpClientFactory(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

            var service = new UserInfoService(aaud, raps, courses, loans, pps, idcards, keys, _configuration, httpFactory, _memoryCache);

            // Act
            var result = await service.GetUserInfoAsync("iam-123", null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("iam-123", result.IamId);
            Assert.Equal("mothra-123", result.MothraId);
            Assert.Equal("Test User", result.DisplayFullName);
            Assert.True(result.CurrentAffiliate);
        }

        [Fact]
        public async Task PopulateEmployeeInfoAsync_PopulatesDetails()
        {
            // Arrange
            var aaudOptions = CreateInMemoryOptions<AAUDContext>();
            var coursesOptions = CreateInMemoryOptions<CoursesContext>();

            using (var aaudSetup = new AAUDContext(aaudOptions))
            {
                aaudSetup.AaudUsers.Add(CreateTestUser("iam-emp", "mothra-emp", employeeId: "emp-789", employeePKey: "101"));
                aaudSetup.Employees.Add(new Employee
                {
                    EmpPKey = "101",
                    EmpTermCode = "202610",
                    EmpPrimaryTitle = "Senior Developer",
                    EmpSchoolDivision = "SVM Dean's Office",
                    EmpStatus = "A",
                    EmpHomeDept = "SVM Dean",
                    EmpEffortHomeDept = "SVM Effort Dept",
                    EmpTeachingHomeDept = "SVM Teaching Dept",
                    EmpTeachingPercentFulltime = 85,
                    EmpClientid = "1",
                    EmpAltDeptCode = "",
                    EmpCbuc = ""
                });
                await aaudSetup.SaveChangesAsync();
            }

            using (var coursesSetup = new CoursesContext(coursesOptions))
            {
                coursesSetup.Terminfos.Add(new Terminfo
                {
                    TermCode = "202610",
                    TermCurrentTermMulti = true,
                    TermAcademicYear = "",
                    TermDesc = "",
                    TermCollCode = "01",
                    TermStartDate = DateTime.Today,
                    TermEndDate = DateTime.Today,
                    TermCurrentTerm = true,
                    TermTermType = ""
                });
                await coursesSetup.SaveChangesAsync();
            }

            using var aaud = new AAUDContext(aaudOptions);
            using var raps = new RAPSContext(CreateInMemoryOptions<RAPSContext>());
            using var courses = new CoursesContext(coursesOptions);
            using var loans = new EquipmentLoanContext(CreateInMemoryOptions<EquipmentLoanContext>());
            using var pps = new PPSContext(CreateInMemoryOptions<PPSContext>());
            using var idcards = new IDCardsContext(CreateInMemoryOptions<IDCardsContext>());
            using var keys = new KeysContext(CreateInMemoryOptions<KeysContext>());

            var httpFactory = CreateMockHttpClientFactory(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
            var service = new UserInfoService(aaud, raps, courses, loans, pps, idcards, keys, _configuration, httpFactory, _memoryCache);

            // Act
            var result = await service.GetUserInfoAsync("iam-emp", null);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsEmployee);
            Assert.Equal("Senior Developer", result.EmployeePrimaryTitle);
            Assert.Equal("SVM Dean's Office", result.EmployeeSchoolDivision);
            Assert.Equal("A", result.EmployeeStatus);
            Assert.Equal("202610", result.EmployeeTerm);
            Assert.Equal("SVM Dean", result.EmployeeHomeDepartment);
            Assert.Equal("SVM Effort Dept", result.EmployeeEffortHomeDepartment);
            Assert.Equal("SVM Teaching Dept", result.EmployeeTeachingHomeDepartment);
            Assert.Equal("85", result.EmployeeTeachingPercentFulltime);
        }

        [Fact]
        public async Task PopulateIDCardsAsync_ExecutesStatusAndReasonJoins()
        {
            // Arrange
            var aaudOptions = CreateInMemoryOptions<AAUDContext>();
            using (var aaudSetup = new AAUDContext(aaudOptions))
            {
                aaudSetup.AaudUsers.Add(CreateTestUser("iam-cards", "mothra-cards", "carduser"));
                await aaudSetup.SaveChangesAsync();
            }

            var idcardsOptions = CreateInMemoryOptions<IDCardsContext>();
            using (var idcardsSetup = new IDCardsContext(idcardsOptions))
            {
                idcardsSetup.IdCards.Add(new IdCard
                {
                    IdCardLoginId = "carduser",
                    IdCardNumber = 987654321,
                    IdCardDisplayName = "CardDisplayName",
                    IdCardLastName = "CardLastName",
                    IdCardLine2 = "Line 2 Text",
                    IdCardCurrentStatus = "A",
                    IdcardDeactivatedReason = "L",
                    IdCardAppliedDate = new DateTime(2026, 1, 1),
                    IdCardIssueDate = new DateTime(2026, 1, 2),
                    IdcardDeactivatedDate = new DateTime(2026, 6, 1)
                });
                idcardsSetup.DvtCardStatuses.Add(new DvtCardStatus
                {
                    DvtStatusCode = "A",
                    DvtStatusDesc = "Active Card Status",
                    DvtStatusVoidable = "",
                    DvtStatusDupOk = ""
                });
                idcardsSetup.DvtReasons.Add(new DvtReason
                {
                    DvtReasonCode = "L",
                    DvtReasonDesc = "Lost Card",
                    DvtReasonVoidable = "",
                    DvtReasonDupOk = ""
                });
                await idcardsSetup.SaveChangesAsync();
            }

            using var aaud = new AAUDContext(aaudOptions);
            using var raps = new RAPSContext(CreateInMemoryOptions<RAPSContext>());
            using var courses = new CoursesContext(CreateInMemoryOptions<CoursesContext>());
            using var loans = new EquipmentLoanContext(CreateInMemoryOptions<EquipmentLoanContext>());
            using var pps = new PPSContext(CreateInMemoryOptions<PPSContext>());
            using var idcards = new IDCardsContext(idcardsOptions);
            using var keys = new KeysContext(CreateInMemoryOptions<KeysContext>());

            var httpFactory = CreateMockHttpClientFactory(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
            var service = new UserInfoService(aaud, raps, courses, loans, pps, idcards, keys, _configuration, httpFactory, _memoryCache);

            // Act
            var result = await service.GetUserInfoAsync("iam-cards", null);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.IDCards);
            var card = result.IDCards[0];
            Assert.Equal("987654321", card.Number);
            Assert.Equal("CardDisplayName", card.DisplayName);
            Assert.Equal("CardLastName", card.LastName);
            Assert.Equal("Line 2 Text", card.Line2);
            Assert.Equal("Active Card Status", card.StatusDescription);
            Assert.Equal("Lost Card", card.DeactivatedReason);
            Assert.Equal(new DateTime(2026, 1, 1), card.Applied);
            Assert.Equal(new DateTime(2026, 1, 2), card.Issued);
            Assert.Equal(new DateTime(2026, 6, 1), card.Deactivated);
        }

        [Fact]
        public async Task PopulateKeysAsync_MapsKeyDetails()
        {
            // Arrange
            var aaudOptions = CreateInMemoryOptions<AAUDContext>();
            using (var aaudSetup = new AAUDContext(aaudOptions))
            {
                aaudSetup.AaudUsers.Add(CreateTestUser("iam-keys", "mothra-keys"));
                aaudSetup.AaudUsers.Add(CreateTestUser("iam-issuer", "mothra-issuer", "issuer", displayFullName: "John KeyIssuer"));
                await aaudSetup.SaveChangesAsync();
            }

            var keysOptions = CreateInMemoryOptions<KeysContext>();
            using (var keysSetup = new KeysContext(keysOptions))
            {
                keysSetup.Keys.Add(new Key
                {
                    KeyId = 202,
                    KeyNumber = "K99",
                    AccessDescription = "Main Gate Access",
                    CreatedBy = "system"
                });
                keysSetup.KeyAssignments.Add(new KeyAssignment
                {
                    KeyId = 202,
                    AssignedTo = "mothra-keys",
                    CutNumber = "C1",
                    IssuedDate = new DateTime(2026, 3, 1),
                    IssuedBy = "mothra-issuer",
                    Deleted = null
                });
                await keysSetup.SaveChangesAsync();
            }

            using var aaud = new AAUDContext(aaudOptions);
            using var raps = new RAPSContext(CreateInMemoryOptions<RAPSContext>());
            using var courses = new CoursesContext(CreateInMemoryOptions<CoursesContext>());
            using var loans = new EquipmentLoanContext(CreateInMemoryOptions<EquipmentLoanContext>());
            using var pps = new PPSContext(CreateInMemoryOptions<PPSContext>());
            using var idcards = new IDCardsContext(CreateInMemoryOptions<IDCardsContext>());
            using var keys = new KeysContext(keysOptions);

            var httpFactory = CreateMockHttpClientFactory(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
            var service = new UserInfoService(aaud, raps, courses, loans, pps, idcards, keys, _configuration, httpFactory, _memoryCache);

            // Act
            var result = await service.GetUserInfoAsync("iam-keys", null);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Keys);
            var assignment = result.Keys[0];
            Assert.Equal("Main Gate Access", assignment.AccessDescription);
            Assert.Equal("K99", assignment.KeyNumber);
            Assert.Equal("C1", assignment.CutNumber);
            Assert.Equal(new DateTime(2026, 3, 1), assignment.IssuedDate);
            Assert.Equal("John KeyIssuer", assignment.IssuedBy);
        }

        [Fact]
        public async Task PopulateLoansAsync_MapsLoanDetails()
        {
            // Arrange
            var aaudOptions = CreateInMemoryOptions<AAUDContext>();
            using (var aaudSetup = new AAUDContext(aaudOptions))
            {
                aaudSetup.AaudUsers.Add(CreateTestUser("iam-loans", "mothra-loans", pidm: "pidm-loans"));
                await aaudSetup.SaveChangesAsync();
            }

            var loansOptions = CreateInMemoryOptions<EquipmentLoanContext>();
            using (var loansSetup = new EquipmentLoanContext(loansOptions))
            {
                var newLoan = new Loan
                {
                    LoanId = 505,
                    LoanPidm = "pidm-loans",
                    LoanTechPidm = "tech-pidm-loans",
                    LoanDate = new DateTime(2026, 5, 1),
                    LoanDueDate = new DateTime(2026, 5, 10),
                    LoanComments = "Projector Loan"
                };
                loansSetup.Loans.Add(newLoan);
                
                var asset = new Asset
                {
                    AssetId = 808,
                    AssetName = "Epson Projector 4K",
                    AssetStatus = "A"
                };
                loansSetup.Assets.Add(asset);

                loansSetup.LoanItems.Add(new LoanItem
                {
                    LoanitemLoanid = 505,
                    LoanitemAssetid = 808,
                    LoanitemAsset = asset,
                    LoanitemCheckout = DateTime.Today,
                    LoanitemCheckoutPidm = "checkout-pidm"
                });
                
                await loansSetup.SaveChangesAsync();
            }

            using var aaud = new AAUDContext(aaudOptions);
            using var raps = new RAPSContext(CreateInMemoryOptions<RAPSContext>());
            using var courses = new CoursesContext(CreateInMemoryOptions<CoursesContext>());
            using var loans = new EquipmentLoanContext(loansOptions);
            using var pps = new PPSContext(CreateInMemoryOptions<PPSContext>());
            using var idcards = new IDCardsContext(CreateInMemoryOptions<IDCardsContext>());
            using var keys = new KeysContext(CreateInMemoryOptions<KeysContext>());

            var httpFactory = CreateMockHttpClientFactory(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
            var service = new UserInfoService(aaud, raps, courses, loans, pps, idcards, keys, _configuration, httpFactory, _memoryCache);

            // Act
            var result = await service.GetUserInfoAsync("iam-loans", null);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Loans);
            var loanResult = result.Loans[0];
            Assert.Equal("Epson Projector 4K", loanResult.AssetName);
            Assert.Equal(new DateTime(2026, 5, 1), loanResult.LoanDate);
            Assert.Equal(new DateTime(2026, 5, 10), loanResult.DueDate);
            Assert.Equal("Projector Loan", loanResult.Comments);
        }

        [Fact]
        public async Task PopulateSystemRolesAndPermissions_QueriesCorrectly()
        {
            // Arrange
            var aaudOptions = CreateInMemoryOptions<AAUDContext>();
            using (var aaudSetup = new AAUDContext(aaudOptions))
            {
                aaudSetup.AaudUsers.Add(CreateTestUser("iam-raps", "mothra-raps"));
                await aaudSetup.SaveChangesAsync();
            }

            var rapsOptions = CreateInMemoryOptions<RAPSContext>();
            using (var rapsSetup = new RAPSContext(rapsOptions))
            {
                var role = new TblRole
                {
                    RoleId = 10,
                    Role = "CN=DirectoryAdmin,OU=Roles,DC=viper",
                    DisplayName = "DirectoryAdmin"
                };
                rapsSetup.TblRoles.Add(role);

                rapsSetup.TblRoleMembers.Add(new TblRoleMember
                {
                    RoleId = 10,
                    MemberId = "mothra-raps",
                    Role = role,
                    ViewName = null
                });

                var perm1 = new TblPermission
                {
                    PermissionId = 50,
                    Permission = "RAPS.User.View"
                };
                var perm2 = new TblPermission
                {
                    PermissionId = 60,
                    Permission = "API.Data.Read"
                };
                rapsSetup.TblPermissions.AddRange(perm1, perm2);

                // Assign perm1 via role
                rapsSetup.TblRolePermissions.Add(new TblRolePermission
                {
                    RoleId = 10,
                    PermissionId = 50,
                    Access = 1
                });

                // Assign perm2 directly
                rapsSetup.TblMemberPermissions.Add(new TblMemberPermission
                {
                    MemberId = "mothra-raps",
                    PermissionId = 60,
                    Access = 1,
                    StartDate = null,
                    EndDate = null
                });

                await rapsSetup.SaveChangesAsync();
            }

            using var aaud = new AAUDContext(aaudOptions);
            using var raps = new RAPSContext(rapsOptions);
            using var courses = new CoursesContext(CreateInMemoryOptions<CoursesContext>());
            using var loans = new EquipmentLoanContext(CreateInMemoryOptions<EquipmentLoanContext>());
            using var pps = new PPSContext(CreateInMemoryOptions<PPSContext>());
            using var idcards = new IDCardsContext(CreateInMemoryOptions<IDCardsContext>());
            using var keys = new KeysContext(CreateInMemoryOptions<KeysContext>());

            var httpFactory = CreateMockHttpClientFactory(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
            var service = new UserInfoService(aaud, raps, courses, loans, pps, idcards, keys, _configuration, httpFactory, _memoryCache);

            // Act
            var result = await service.GetUserInfoAsync("iam-raps", null);

            // Assert
            Assert.NotNull(result);
            
            // Check formatted Roles
            Assert.Single(result.SystemRoles);
            Assert.Equal("VIPER", result.SystemRoles[0].System);
            Assert.Equal("DirectoryAdmin", result.SystemRoles[0].DisplayName);

            // Check permissions categories
            var rapsPermCategory = result.SystemPermissions.FirstOrDefault(p => p.Category == "RAPS");
            Assert.NotNull(rapsPermCategory);
            Assert.Equal(1, rapsPermCategory.Count);
            Assert.Equal("RAPS.User.View", rapsPermCategory.Permissions[0]);

            var apiPermCategory = result.SystemPermissions.FirstOrDefault(p => p.Category == "API");
            Assert.NotNull(apiPermCategory);
            Assert.Equal(1, apiPermCategory.Count);
            Assert.Equal("API.Data.Read", apiPermCategory.Permissions[0]);
        }

        [Fact]
        public async Task PopulateIamInfoAsync_CallsApiAndMapsCollections()
        {
            // Arrange
            var aaudOptions = CreateInMemoryOptions<AAUDContext>();
            using (var aaudSetup = new AAUDContext(aaudOptions))
            {
                aaudSetup.AaudUsers.Add(CreateTestUser("iam-caller", "mothra-caller"));
                await aaudSetup.SaveChangesAsync();
            }

            // Mock responses for SearchForPerson (iam/people/search) and GetEmployeeAssociations (iam/associations/pps/{iamId})
            var httpFactory = CreateMockHttpClientFactory(request =>
            {
                var uri = request.RequestUri?.ToString() ?? "";
                if (uri.Contains("iam/people/search"))
                {
                    var json = @"
                    {
                        ""responseStatus"": 0,
                        ""responseData"": {
                            ""results"": [
                                {
                                    ""iamId"": ""iam-caller"",
                                    ""oFirstName"": ""Jane"",
                                    ""oLastName"": ""Doe"",
                                    ""oFullName"": ""Full Name From IAM"",
                                    ""ppsId"": ""pps-111"",
                                    ""isEmployee"": true,
                                    ""isStudent"": false
                                }
                            ]
                        }
                    }";
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }
                else if (uri.Contains("iam/associations/pps/iam-caller"))
                {
                    var json = @"
                    {
                        ""responseStatus"": 0,
                        ""responseData"": {
                            ""results"": [
                                {
                                    ""iamId"": ""iam-caller"",
                                    ""titleDisplayName"": ""Manager"",
                                    ""titleCode"": ""001100"",
                                    ""deptDisplayName"": ""VetMed Dean"",
                                    ""deptCode"": ""062000"",
                                    ""percentFullTime"": ""1.0"",
                                    ""assocStartDate"": ""2026-01-01""
                                }
                            ]
                        }
                    }";
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            using var aaud = new AAUDContext(aaudOptions);
            using var raps = new RAPSContext(CreateInMemoryOptions<RAPSContext>());
            using var courses = new CoursesContext(CreateInMemoryOptions<CoursesContext>());
            using var loans = new EquipmentLoanContext(CreateInMemoryOptions<EquipmentLoanContext>());
            using var pps = new PPSContext(CreateInMemoryOptions<PPSContext>());
            using var idcards = new IDCardsContext(CreateInMemoryOptions<IDCardsContext>());
            using var keys = new KeysContext(CreateInMemoryOptions<KeysContext>());

            var service = new UserInfoService(aaud, raps, courses, loans, pps, idcards, keys, _configuration, httpFactory, _memoryCache);

            // Act
            var result = await service.GetUserInfoAsync("iam-caller", null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("pps-111", result.PPSId);
            Assert.Equal("Full Name From IAM", result.OFullName);

            // Check populated collections
            Assert.Single(result.IamPeople);
            Assert.Equal("Full Name From IAM", result.IamPeople[0].OFullName);

            Assert.Single(result.IamAssociations);
            Assert.Equal("Manager", result.IamAssociations[0].TitleDisplayName);
            Assert.Equal("VetMed Dean", result.IamAssociations[0].DeptDisplayName);
        }

        [Fact]
        public async Task PopulateInstinctInfoAsync_ResolvesEndpointAndQueriesGraphQL()
        {
            // Arrange
            var aaudOptions = CreateInMemoryOptions<AAUDContext>();
            using (var aaudSetup = new AAUDContext(aaudOptions))
            {
                aaudSetup.AaudUsers.Add(CreateTestUser("iam-inst", "mothra-inst", firstName: "Jane", lastName: "Doe", middleName: "Alex"));
                await aaudSetup.SaveChangesAsync();
            }

            // HTTP Mock for token request and graphql user lookup
            var httpFactory = CreateMockHttpClientFactory(request =>
            {
                var uri = request.RequestUri?.ToString() ?? "";
                if (uri.Contains("auth/token"))
                {
                    var tokenJson = "{\"access_token\": \"jane-doe-token-key\", \"expires_in\": 86400}";
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(tokenJson, Encoding.UTF8, "application/json")
                    };
                }
                else if (uri.Contains("query="))
                {
                    var searchJson = @"
                    {
                        ""data"": {
                            ""searchUsers"": [
                                {
                                    ""id"": ""inst-jane-doe"",
                                    ""instinctId"": ""inst-jane-doe"",
                                    ""status"": ""Active"",
                                    ""isActive"": true,
                                    ""username"": ""jdoe"",
                                    ""nameFirst"": ""Jane"",
                                    ""nameLast"": ""Doe"",
                                    ""roles"": [
                                        { ""label"": ""Doctor"" }
                                    ]
                                }
                            ]
                        }
                    }";
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(searchJson, Encoding.UTF8, "application/json")
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            using var aaud = new AAUDContext(aaudOptions);
            using var raps = new RAPSContext(CreateInMemoryOptions<RAPSContext>());
            using var courses = new CoursesContext(CreateInMemoryOptions<CoursesContext>());
            using var loans = new EquipmentLoanContext(CreateInMemoryOptions<EquipmentLoanContext>());
            using var pps = new PPSContext(CreateInMemoryOptions<PPSContext>());
            using var idcards = new IDCardsContext(CreateInMemoryOptions<IDCardsContext>());
            using var keys = new KeysContext(CreateInMemoryOptions<KeysContext>());

            var service = new UserInfoService(aaud, raps, courses, loans, pps, idcards, keys, _configuration, httpFactory, _memoryCache);

            // Act & Assert with temporary HttpHelper configuration
            var mockEnv = Substitute.For<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            HttpHelper.Configure(_memoryCache, _configuration, mockEnv, null, null, null);
            try
            {
                var result = await service.GetUserInfoAsync("iam-inst", null);

                Assert.NotNull(result);
                Assert.Null(result.InstinctInfo?.ErrorMessage);
                Assert.Equal("inst-jane-doe", result.InstinctId);
                Assert.Equal("jdoe", result.InstinctUsername);
                Assert.Equal("Active", result.InstinctStatus);
                Assert.True(result.InstinctIsActive);
                Assert.Single(result.InstinctRoles);
                Assert.Equal("Doctor", result.InstinctRoles[0]);
            }
            finally
            {
                HttpHelper.Configure(null, null, null, null, null, null);
            }
        }
        [Fact]
        public async Task TestGetEmployeeAssociationsDirectly()
        {
            var httpFactory = CreateMockHttpClientFactory(request =>
            {
                var json = @"
                {
                    ""responseStatus"": 0,
                    ""responseData"": {
                        ""results"": [
                            {
                                ""iamId"": ""iam-caller"",
                                ""titleDisplayName"": ""Manager"",
                                ""titleCode"": ""001100"",
                                ""deptDisplayName"": ""VetMed Dean"",
                                ""deptCode"": ""062000"",
                                ""percentFullTime"": ""1.0"",
                                ""assocStartDate"": ""2026-01-01""
                            }
                        ]
                    }
                }";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            });

            var iamApi = new IamApi(httpFactory);
            var response = await iamApi.GetEmployeeAssociations("iam-caller");
            Assert.Null(response.ErrorMessage);
            Assert.NotNull(response.Data);
            Assert.Single(response.Data);
        }

        [Fact]
        public async Task TestDeserializeCorePersonDirectly()
        {
            var httpFactory = CreateMockHttpClientFactory(request =>
            {
                var json = @"
                {
                    ""responseStatus"": 0,
                    ""responseData"": {
                        ""results"": [
                            {
                                ""iamId"": ""iam-caller"",
                                ""oFirstName"": ""Jane"",
                                ""oLastName"": ""Doe"",
                                ""oFullName"": ""Full Name From IAM"",
                                ""ppsId"": ""pps-111"",
                                ""isEmployee"": true,
                                ""isStudent"": false
                            }
                        ]
                    }
                }";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            });

            var iamApi = new IamApi(httpFactory);
            var response = await iamApi.SearchForPerson(iamId: "iam-caller");
            Assert.Null(response.ErrorMessage);
            Assert.NotNull(response.Data);
            Assert.Single(response.Data);
        }

        private class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

            public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            {
                _handler = handler;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_handler(request));
            }
        }
    }
}
