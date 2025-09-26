using MockQueryable.Moq;
using Moq;
using Viper.Classes.SQLContext;
using Viper.Models.ClinicalScheduler;

namespace Viper.test.ClinicalScheduler.Setup
{
    /// <summary>
    /// Test data setup for Person entities in Clinical Scheduler tests
    /// </summary>
    internal static class SetupClinicalSchedulerPeople
    {
        public static readonly List<Person> TestPeople = new()
        {
            new Person
            {
                IdsMothraId = "test123",
                PersonDisplayFirstName = "Test",
                PersonDisplayLastName = "User",
                PersonDisplayFullName = "User, Test",
                IdsMailId = "test@example.com"
            },
            new Person
            {
                IdsMothraId = "currentuser",
                PersonDisplayFirstName = "Current",
                PersonDisplayLastName = "User",
                PersonDisplayFullName = "User, Current",
                IdsMailId = "current@example.com"
            },
            new Person
            {
                IdsMothraId = "otheruser",
                PersonDisplayFirstName = "Other",
                PersonDisplayLastName = "User",
                PersonDisplayFullName = "User, Other",
                IdsMailId = "other@example.com"
            },
            new Person
            {
                IdsMothraId = "primaryeval",
                PersonDisplayFirstName = "Primary",
                PersonDisplayLastName = "Evaluator",
                PersonDisplayFullName = "Evaluator, Primary",
                IdsMailId = "primary@example.com"
            },
            new Person
            {
                IdsMothraId = "secondaryeval",
                PersonDisplayFirstName = "Secondary",
                PersonDisplayLastName = "Evaluator",
                PersonDisplayFullName = "Evaluator, Secondary",
                IdsMailId = "secondary@example.com"
            }
        };

        /// <summary>
        /// Gets test people as queryable for mocking
        /// </summary>
        public static IQueryable<Person> GetTestPeople()
        {
            return TestPeople.AsQueryable();
        }

        /// <summary>
        /// Sets up the Persons DbSet mock with test data
        /// </summary>
        public static void SetupPersonsTable(Mock<ClinicalSchedulerContext> mockContext)
        {
            var mockDbSet = TestPeople.AsQueryable().BuildMockDbSet();
            mockContext.Setup(c => c.Persons).Returns(mockDbSet.Object);
        }

        /// <summary>
        /// Sets up the Persons DbSet mock with custom test data
        /// </summary>
        public static void SetupPersonsTable(Mock<ClinicalSchedulerContext> mockContext, List<Person> customPeople)
        {
            var mockDbSet = customPeople.AsQueryable().BuildMockDbSet();
            mockContext.Setup(c => c.Persons).Returns(mockDbSet.Object);
        }

        /// <summary>
        /// Creates a test person with specified MothraId
        /// </summary>
        public static Person CreateTestPerson(string mothraId, string firstName = "Test", string lastName = "User")
        {
            return new Person
            {
                IdsMothraId = mothraId,
                PersonDisplayFirstName = firstName,
                PersonDisplayLastName = lastName,
                PersonDisplayFullName = $"{lastName}, {firstName}",
                IdsMailId = $"{mothraId}@example.com"
            };
        }
    }
}
