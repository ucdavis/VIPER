using Viper.Areas.ClinicalScheduler.Extensions;
using Viper.Models.ClinicalScheduler;

namespace Test.ClinicalScheduler
{
    public class ServiceMappingExtensionsTests
    {
        [Fact]
        public void ToDto_MapsServicePropertiesCorrectly()
        {
            // Arrange
            var service = new Service
            {
                ServiceId = 10,
                ServiceName = "Surgery Service",
                ShortName = "Surgery",
                WeekSize = 2,
                ScheduleEditPermission = "SVMSecure.ClnSched.EditSurgery"
            };

            // Act
            var dto = service.ToDto();

            // Assert
            Assert.Equal(service.ServiceId, dto.ServiceId);
            Assert.Equal(service.ServiceName, dto.ServiceName);
            Assert.Equal(service.WeekSize, dto.WeekSize);
            Assert.Equal(service.ScheduleEditPermission, dto.ScheduleEditPermission);
        }


        [Fact]
        public void ToDto_CollectionMapping_MapsAllServices()
        {
            // Arrange
            var services = new List<Service>
            {
                new Service { ServiceId = 10, ServiceName = "Surgery", ShortName = "SURG" },
                new Service { ServiceId = 20, ServiceName = "Medicine", ShortName = "MED" },
                new Service { ServiceId = 30, ServiceName = "Anesthesia", ShortName = "ANES" }
            };

            // Act
            var dtos = services.ToDto().ToList();

            // Assert
            Assert.Equal(3, dtos.Count);
            Assert.Equal(10, dtos[0].ServiceId);
            Assert.Equal(20, dtos[1].ServiceId);
            Assert.Equal(30, dtos[2].ServiceId);
        }


        [Fact]
        public void ToDto_EmptyCollection_ReturnsEmptyResult()
        {
            // Arrange
            var services = new List<Service>();

            // Act
            var dtos = services.ToDto().ToList();

            // Assert
            Assert.Empty(dtos);
        }

        [Fact]
        public void ToDto_HandlesNullScheduleEditPermission()
        {
            // Arrange
            var service = new Service
            {
                ServiceId = 10,
                ServiceName = "Surgery Service",
                ShortName = "Surgery",
                WeekSize = 2,
                ScheduleEditPermission = null
            };

            // Act
            var dto = service.ToDto();

            // Assert
            Assert.Null(dto.ScheduleEditPermission);
            Assert.Equal(service.ServiceId, dto.ServiceId);
        }

        [Fact]
        public void ToDto_HandlesZeroWeekSize()
        {
            // Arrange
            var service = new Service
            {
                ServiceId = 10,
                ServiceName = "Special Service",
                ShortName = "SPEC",
                WeekSize = 0
            };

            // Act
            var dto = service.ToDto();

            // Assert
            Assert.Equal(0, dto.WeekSize);
        }
    }
}
