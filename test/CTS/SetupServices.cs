using MockQueryable.NSubstitute;
using NSubstitute;
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

        public static void SetupServicesTable(VIPERContext context)
        {
            var mockSet = Services.BuildMockDbSet();
            context.Services.Returns(mockSet);

            var mockSet2 = ServiceChiefs.BuildMockDbSet();
            context.ServiceChiefs.Returns(mockSet2);
        }
    }
}
