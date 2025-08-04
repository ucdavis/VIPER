using Viper.Areas.CTS.Models;
using Viper.Models.CTS;

namespace Viper.test.CTS
{
    public class CompetencyBundleAssociationDtoTest
    {
        [Fact]
        public void Constructor_MapsAllPropertiesFromCompetency()
        {
            // Arrange
            var domain = new Domain { DomainId = 1, Name = "Test Domain", Order = 5 };
            var competency = new Competency
            {
                CompetencyId = 123,
                DomainId = 1,
                ParentId = 100,
                Number = "1.2.3",
                Name = "Test Competency",
                Description = "Test Description",
                CanLinkToStudent = true,
                Domain = domain,
                BundleCompetencies = new List<BundleCompetency>()
            };

            // Act
            var dto = new CompetencyBundleAssociationDto(competency);

            // Assert
            Assert.Equal(123, dto.CompetencyId);
            Assert.Equal(1, dto.DomainId);
            Assert.Equal(100, dto.ParentId);
            Assert.Equal("1.2.3", dto.Number);
            Assert.Equal("Test Competency", dto.Name);
            Assert.Equal("Test Description", dto.Description);
            Assert.True(dto.CanLinkToStudent);
            Assert.Equal("Test Domain", dto.DomainName);
            Assert.Equal(5, dto.DomainOrder);
            Assert.Empty(dto.Bundles);
        }

        [Fact]
        public void Constructor_HandlesNullDomain()
        {
            // Arrange
            var competency = new Competency
            {
                CompetencyId = 1,
                Number = "1",
                Name = "Test",
                Domain = null!,
                BundleCompetencies = new List<BundleCompetency>()
            };

            // Act
            var dto = new CompetencyBundleAssociationDto(competency);

            // Assert
            Assert.Null(dto.DomainName);
            Assert.Null(dto.DomainOrder);
        }

        [Fact]
        public void Constructor_HandlesNullBundleCompetencies()
        {
            // Arrange
            var competency = new Competency
            {
                CompetencyId = 1,
                Number = "1",
                Name = "Test",
                BundleCompetencies = null!
            };

            // Act
            var dto = new CompetencyBundleAssociationDto(competency);

            // Assert
            Assert.NotNull(dto.Bundles);
            Assert.Empty(dto.Bundles);
        }

        [Fact]
        public void Constructor_MapsBundlesCorrectly()
        {
            // Arrange
            var bundles = new List<Bundle>
            {
                new Bundle { BundleId = 1, Name = "Bundle 1", Clinical = true, Assessment = false, Milestone = true },
                new Bundle { BundleId = 2, Name = "Bundle 2", Clinical = false, Assessment = true, Milestone = false }
            };

            var competency = new Competency
            {
                CompetencyId = 1,
                Number = "1",
                Name = "Test",
                BundleCompetencies = new List<BundleCompetency>
                {
                    new BundleCompetency { BundleId = 1, Bundle = bundles[0] },
                    new BundleCompetency { BundleId = 2, Bundle = bundles[1] }
                }
            };

            // Act
            var dto = new CompetencyBundleAssociationDto(competency);

            // Assert
            Assert.Equal(2, dto.Bundles.Count);
            
            var bundle1 = dto.Bundles.Find(b => b.BundleId == 1);
            Assert.NotNull(bundle1);
            Assert.Equal("Bundle 1", bundle1.Name);
            Assert.True(bundle1.Clinical);
            Assert.False(bundle1.Assessment);
            Assert.True(bundle1.Milestone);

            var bundle2 = dto.Bundles.Find(b => b.BundleId == 2);
            Assert.NotNull(bundle2);
            Assert.Equal("Bundle 2", bundle2.Name);
            Assert.False(bundle2.Clinical);
            Assert.True(bundle2.Assessment);
            Assert.False(bundle2.Milestone);
        }

        [Fact]
        public void Constructor_FiltersOutNullBundles()
        {
            // Arrange
            var bundle = new Bundle { BundleId = 1, Name = "Valid Bundle", Clinical = true };
            var competency = new Competency
            {
                CompetencyId = 1,
                Number = "1",
                Name = "Test",
                BundleCompetencies = new List<BundleCompetency>
                {
                    new BundleCompetency { BundleId = 1, Bundle = bundle },
                    new BundleCompetency { BundleId = 2, Bundle = null! }, // Null bundle should be filtered
                }
            };

            // Act
            var dto = new CompetencyBundleAssociationDto(competency);

            // Assert
            Assert.Single(dto.Bundles); // Should only have one bundle (nulls filtered)
            Assert.Equal("Valid Bundle", dto.Bundles[0].Name);
        }

        [Fact]
        public void Constructor_HandlesDuplicateBundleReferences()
        {
            // Arrange
            var bundle1 = new Bundle { BundleId = 1, Name = "Bundle", Clinical = true };
            var bundle2 = new Bundle { BundleId = 2, Name = "Different Bundle", Clinical = false };
            var competency = new Competency
            {
                CompetencyId = 1,
                Number = "1",
                Name = "Test",
                BundleCompetencies = new List<BundleCompetency>
                {
                    new BundleCompetency { BundleId = 1, Bundle = bundle1 },
                    new BundleCompetency { BundleId = 2, Bundle = bundle2 }
                }
            };

            // Act
            var dto = new CompetencyBundleAssociationDto(competency);

            // Assert
            Assert.Equal(2, dto.Bundles.Count); // Should have both bundles
            Assert.Contains(dto.Bundles, b => b.Name == "Bundle");
            Assert.Contains(dto.Bundles, b => b.Name == "Different Bundle");
        }

        [Fact]
        public void BundleInfoDto_HasCorrectDefaultValues()
        {
            // Arrange & Act
            var bundleInfo = new BundleInfoDto();

            // Assert
            Assert.Equal(0, bundleInfo.BundleId);
            Assert.Null(bundleInfo.Name);
            Assert.False(bundleInfo.Clinical);
            Assert.False(bundleInfo.Assessment);
            Assert.False(bundleInfo.Milestone);
        }
    }
}