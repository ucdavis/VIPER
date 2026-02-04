using MockQueryable.Moq;
using Moq;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;

namespace Viper.test.CTS
{
    internal static class SetupServices
    {
        public static readonly List<Service> Services = new()
        {
            new()
            {
                ServiceId = 1,
                ServiceName = "Behavior",
                ShortName = "BEH",
            },
            new()
            {
                ServiceId = 2,
                ServiceName = "Cardiology",
                ShortName = "Cardio",
            },
        };

        public static readonly List<ServiceChief> ServiceChiefs = new()
        {
            new()
            {
                PersonId = SetupUsers.chief.AaudUserId,
                FirstName = SetupUsers.chief.FirstName,
                LastName = SetupUsers.chief.LastName,
                MothraId = "00000001",
                ServiceId = 1,
                ServiceChiefId = 1,
                ServiceName = "Behavior",
            }
        };

        public static void SetupServicesTable(Mock<VIPERContext> context)
        {
            var mockSet = Services.AsAsyncQueryable().BuildMockDbSet();
            context.Setup(c => c.Services).Returns(mockSet.Object);

            var mockSet2 = ServiceChiefs.AsAsyncQueryable().BuildMockDbSet();
            context.Setup(c => c.ServiceChiefs).Returns(mockSet2.Object);
        }
    }
}
