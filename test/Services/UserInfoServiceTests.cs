using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;
using System.Text;
using Viper.Areas.Directory.Models;
using Viper.Areas.Directory.Services;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Models.Courses;

namespace Viper.test.Services
{
    [Collection("HttpHelper static state")]
    public class UserInfoServiceTests
    {
        private static readonly UserInfoViewPermissions AllPermissions = new()
        {
            IsOwnPage = true,
            CanViewDirectoryDetail = true,
            CanViewStudentID = true,
            CanViewIAM = true,
            CanViewRoles = true,
            CanViewUCPath = true,
            CanViewUCPathDetail = true,
            CanViewIDCards = true,
            CanViewKeys = true,
            CanViewLoans = true,
            CanViewInstinct = true,
            CanViewADGroups = true
        };

        private readonly ITestOutputHelper _output;

        public UserInfoServiceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static DbContextOptions<TContext> CreateInMemoryOptions<TContext>() where TContext : DbContext
        {
            return new DbContextOptionsBuilder<TContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task TestGetUserInfo_WithSyntheticData()
        {
            _output.WriteLine("[DEBUG] Starting TestGetUserInfo with fully synthetic data...");

            // 1. Arrange Config & Cache
            var configData = new Dictionary<string, string?>
            {
                { "Instinct:ApiUrl", "https://synthetic.instinctvet.com/" },
                { "Credentials:InstinctApi", "synthetic-password" }
            };
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            var memoryCache = new MemoryCache(new MemoryCacheOptions());

            // 2. Mock HttpClientFactory
            var mockHandler = new MockHttpMessageHandler(request =>
            {
                var uri = request.RequestUri?.ToString() ?? "";
                if (uri.Contains("auth/token"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"access_token\": \"synthetic-token\", \"expires_in\": 86400}", Encoding.UTF8, "application/json")
                    };
                }
                if (uri.Contains("query="))
                {
                    var searchJson = @"
                    {
                        ""data"": {
                            ""searchUsers"": [
                                {
                                    ""id"": ""inst-synthetic-id"",
                                    ""instinctId"": ""inst-synthetic-id"",
                                    ""status"": ""Active"",
                                    ""isActive"": true,
                                    ""username"": ""jsmith"",
                                    ""nameFirst"": ""John"",
                                    ""nameLast"": ""Smith"",
                                    ""roles"": [
                                        { ""label"": ""Staff"" }
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

            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(mockHandler));

            // 3. Setup DbContexts and Seed Synthetic Data
            var aaudOptions = CreateInMemoryOptions<AAUDContext>();
            var coursesOptions = CreateInMemoryOptions<CoursesContext>();

            using (var aaudSetup = new AAUDContext(aaudOptions))
            {
                var syntheticUser = new AaudUser
                {
                    IamId = "99999999",
                    MothraId = "88888888",
                    MailId = "jsmith",
                    LoginId = "jsmith",
                    FirstName = "John",
                    LastName = "Smith",
                    DisplayFirstName = "John",
                    DisplayLastName = "Smith",
                    DisplayFullName = "John Smith",
                    Current = 1,
                    EmployeeId = "emp-999",
                    EmployeePKey = "pkey-999",
                    ClientId = "1"
                };
                aaudSetup.AaudUsers.Add(syntheticUser);

                aaudSetup.Employees.Add(new Employee
                {
                    EmpPKey = "pkey-999",
                    EmpTermCode = "202610",
                    EmpPrimaryTitle = "Synthetic Analyst",
                    EmpSchoolDivision = "Synthetic Division",
                    EmpStatus = "A",
                    EmpHomeDept = "Synthetic Dept",
                    EmpClientid = "1",
                    EmpAltDeptCode = "",
                    EmpCbuc = ""
                });

                await aaudSetup.SaveChangesAsync(TestContext.Current.CancellationToken);
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
                await coursesSetup.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            using var aaud = new AAUDContext(aaudOptions);
            using var raps = new RAPSContext(CreateInMemoryOptions<RAPSContext>());
            using var courses = new CoursesContext(coursesOptions);
            using var loans = new EquipmentLoanContext(CreateInMemoryOptions<EquipmentLoanContext>());
            using var pps = new PPSContext(CreateInMemoryOptions<PPSContext>());
            using var idcards = new IDCardsContext(CreateInMemoryOptions<IDCardsContext>());
            using var keys = new KeysContext(CreateInMemoryOptions<KeysContext>());
            using var sis = new SISContext(CreateInMemoryOptions<SISContext>());

            var userInfoService = new UserInfoService(aaud, raps, courses, loans, pps, idcards, keys, sis, config, httpClientFactory, memoryCache, Substitute.For<ILogger<UserInfoService>>());

            // 4. Act
            // Temporary HttpHelper configuration inside the test context
            var mockEnv = Substitute.For<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            HttpHelper.Configure(memoryCache, config, mockEnv, null, null, null);

            UserInfoResult? result;
            try
            {
                result = await userInfoService.GetUserInfoAsync("99999999", "88888888", AllPermissions);
            }
            finally
            {
                HttpHelper.Configure(null, null, null!, null, null, null);
            }

            // 5. Assert
            Assert.NotNull(result);
            _output.WriteLine($"[DEBUG] Result IamId: {result.IamId}");
            _output.WriteLine($"[DEBUG] Result DisplayName: {result.DisplayFullName}");
            _output.WriteLine($"[DEBUG] Result InstinctId: {result.InstinctId}");
            _output.WriteLine($"[DEBUG] Result InstinctInfo.ErrorMessage: {result.InstinctInfo?.ErrorMessage}");
            _output.WriteLine($"[DEBUG] Result InstinctInfo.Valid: {result.InstinctInfo?.Valid}");

            Assert.Equal("99999999", result.IamId);
            Assert.Equal("88888888", result.MothraId);
            Assert.Equal("John Smith", result.DisplayFullName);
            Assert.True(result.IsEmployee);
            Assert.Equal("Synthetic Analyst", result.EmployeePrimaryTitle);
            Assert.Equal("inst-synthetic-id", result.InstinctId);
            Assert.Equal("jsmith", result.InstinctUsername);
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
