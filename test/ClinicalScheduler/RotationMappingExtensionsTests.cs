using Viper.Areas.ClinicalScheduler.Extensions;
using Viper.Models.ClinicalScheduler;

namespace Test.ClinicalScheduler
{
    public class RotationMappingExtensionsTests
    {
        [Fact]
        public void ToDto_MapsRotationPropertiesCorrectly()
        {
            // Arrange
            var rotation = new Rotation
            {
                RotId = 1,
                Name = "Surgery",
                Abbreviation = "SURG",
                SubjectCode = "VET",
                CourseNumber = "400",
                ServiceId = 10,
                Service = new Service
                {
                    ServiceId = 10,
                    ServiceName = "Surgery Service",
                    ShortName = "Surgery",
                    WeekSize = 2,
                    ScheduleEditPermission = "SVMSecure.ClnSched.EditSurgery"
                }
            };

            // Act
            var dto = rotation.ToDto();

            // Assert
            Assert.Equal(rotation.RotId, dto.RotId);
            Assert.Equal(rotation.Name, dto.Name);
            Assert.Equal(rotation.Abbreviation, dto.Abbreviation);
            Assert.Equal(rotation.SubjectCode, dto.SubjectCode);
            Assert.Equal(rotation.CourseNumber, dto.CourseNumber);
            Assert.Equal(rotation.ServiceId, dto.ServiceId);
            Assert.NotNull(dto.Service);
            Assert.Equal(rotation.Service.ServiceId, dto.Service.ServiceId);
            Assert.Equal(rotation.Service.ServiceName, dto.Service.ServiceName);
        }

        [Fact]
        public void ToDto_ExcludesServiceWhenRequested()
        {
            // Arrange
            var rotation = new Rotation
            {
                RotId = 1,
                Name = "Surgery",
                ServiceId = 10,
                Service = new Service { ServiceId = 10, ServiceName = "Surgery Service" }
            };

            // Act
            var dto = rotation.ToDto(false);

            // Assert
            Assert.Null(dto.Service);
            Assert.Equal(rotation.ServiceId, dto.ServiceId);
        }

        [Fact]
        public void ToDto_HandlesNullService()
        {
            // Arrange
            var rotation = new Rotation
            {
                RotId = 1,
                Name = "Surgery",
                ServiceId = 10,
                Service = null!
            };

            // Act
            var dto = rotation.ToDto();

            // Assert
            Assert.Null(dto.Service);
        }


        [Fact]
        public void ToDto_CollectionMapping_MapsAllRotations()
        {
            // Arrange
            var rotations = new List<Rotation>
            {
                new Rotation { RotId = 1, Name = "Surgery", ServiceId = 10 },
                new Rotation { RotId = 2, Name = "Medicine", ServiceId = 20 },
                new Rotation { RotId = 3, Name = "Anesthesia", ServiceId = 30 }
            };

            // Act
            var dtos = rotations.ToDto(false).ToList();

            // Assert
            Assert.Equal(3, dtos.Count);
            Assert.Equal(1, dtos[0].RotId);
            Assert.Equal(2, dtos[1].RotId);
            Assert.Equal(3, dtos[2].RotId);
        }


        [Fact]
        public void ToDto_EmptyCollection_ReturnsEmptyResult()
        {
            // Arrange
            var rotations = new List<Rotation>();

            // Act
            var dtos = rotations.ToDto().ToList();

            // Assert
            Assert.Empty(dtos);
        }
    }
}
