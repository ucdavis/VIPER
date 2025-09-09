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
            Assert.Equal(service.ShortName, dto.ShortName);
            Assert.Equal(service.WeekSize, dto.WeekSize);
            Assert.Equal(service.ScheduleEditPermission, dto.ScheduleEditPermission);
            Assert.Null(dto.UserCanEdit); // Should be null when not explicitly set
        }

        [Fact]
        public void ToDto_WithUserCanEdit_SetsPermissionFlag()
        {
            // Arrange
            var service = new Service
            {
                ServiceId = 10,
                ServiceName = "Surgery Service",
                ShortName = "Surgery"
            };

            // Act
            var dtoCanEdit = service.ToDto(userCanEdit: true);
            var dtoCannotEdit = service.ToDto(userCanEdit: false);

            // Assert
            Assert.True(dtoCanEdit.UserCanEdit);
            Assert.False(dtoCannotEdit.UserCanEdit);
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
            Assert.All(dtos, dto => Assert.Null(dto.UserCanEdit));
        }

        [Fact]
        public void ToDto_CollectionWithPermissionLookup_AppliesPermissionsCorrectly()
        {
            // Arrange
            var services = new List<Service>
            {
                new Service { ServiceId = 10, ServiceName = "Surgery", ShortName = "SURG" },
                new Service { ServiceId = 20, ServiceName = "Medicine", ShortName = "MED" },
                new Service { ServiceId = 30, ServiceName = "Anesthesia", ShortName = "ANES" }
            };

            // User can edit services 10 and 30, but not 20
            Func<int, bool> userCanEditLookup = serviceId => serviceId == 10 || serviceId == 30;

            // Act
            var dtos = services.ToDto(userCanEditLookup).ToList();

            // Assert
            Assert.Equal(3, dtos.Count);
            Assert.True(dtos[0].UserCanEdit);
            Assert.False(dtos[1].UserCanEdit);
            Assert.True(dtos[2].UserCanEdit);
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
