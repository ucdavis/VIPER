using Microsoft.AspNetCore.Mvc;
using MockQueryable.Moq;
using Moq;
using Viper.Areas.CTS.Controllers;
using Viper.Areas.CTS.Models;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;

namespace Viper.test.CTS
{
    public class CompetencyBundleAssociationControllerTest
    {
        private readonly Mock<VIPERContext> _mockContext;
        private readonly CompetencyBundleAssociationController _controller;
        private readonly List<Competency> _testCompetencies;
        private readonly List<Bundle> _testBundles;

        public CompetencyBundleAssociationControllerTest()
        {
            _mockContext = new Mock<VIPERContext>();
            _controller = new CompetencyBundleAssociationController(_mockContext.Object);

            // Setup test data
            _testBundles = new List<Bundle>
            {
                new Bundle { BundleId = 1, Name = "Clinical Bundle", Clinical = true, Assessment = false, Milestone = false },
                new Bundle { BundleId = 2, Name = "Assessment Bundle", Clinical = false, Assessment = true, Milestone = false },
                new Bundle { BundleId = 3, Name = "Milestone Bundle", Clinical = false, Assessment = false, Milestone = true },
                new Bundle { BundleId = 4, Name = "Mixed Bundle", Clinical = true, Assessment = true, Milestone = false }
            };

            var testDomain = new Domain { DomainId = 1, Name = "Test Domain", Order = 1 };

            _testCompetencies = new List<Competency>
            {
                // Competency with no bundles (unbundled)
                new Competency
                {
                    CompetencyId = 1,
                    Number = "1.1",
                    Name = "Unbundled Competency",
                    DomainId = 1,
                    Domain = testDomain,
                    Order = 1,
                    BundleCompetencies = new List<BundleCompetency>()
                },
                // Competency with clinical bundle
                new Competency
                {
                    CompetencyId = 2,
                    Number = "1.2",
                    Name = "Clinical Competency",
                    DomainId = 1,
                    Domain = testDomain,
                    Order = 2,
                    BundleCompetencies = new List<BundleCompetency>
                    {
                        new BundleCompetency { BundleId = 1, Bundle = _testBundles[0], CompetencyId = 2 }
                    }
                },
                // Competency with assessment bundle
                new Competency
                {
                    CompetencyId = 3,
                    Number = "1.3",
                    Name = "Assessment Competency",
                    DomainId = 1,
                    Domain = testDomain,
                    Order = 3,
                    BundleCompetencies = new List<BundleCompetency>
                    {
                        new BundleCompetency { BundleId = 2, Bundle = _testBundles[1], CompetencyId = 3 }
                    }
                },
                // Competency with milestone bundle
                new Competency
                {
                    CompetencyId = 4,
                    Number = "1.4",
                    Name = "Milestone Competency",
                    DomainId = 1,
                    Domain = testDomain,
                    Order = 4,
                    BundleCompetencies = new List<BundleCompetency>
                    {
                        new BundleCompetency { BundleId = 3, Bundle = _testBundles[2], CompetencyId = 4 }
                    }
                },
                // Competency with multiple bundles
                new Competency
                {
                    CompetencyId = 5,
                    Number = "1.5",
                    Name = "Multi-Bundle Competency",
                    DomainId = 1,
                    Domain = testDomain,
                    Order = 5,
                    BundleCompetencies = new List<BundleCompetency>
                    {
                        new BundleCompetency { BundleId = 1, Bundle = _testBundles[0], CompetencyId = 5 },
                        new BundleCompetency { BundleId = 4, Bundle = _testBundles[3], CompetencyId = 5 }
                    }
                }
            };

            // Setup mock DbSet using MockQueryable
            var mockDbSet = _testCompetencies.AsQueryable().BuildMockDbSet();
            _mockContext.Setup(c => c.Competencies).Returns(mockDbSet.Object);
        }

        [Fact]
        public async Task GetCompetencyBundleAssociations_NoFilters_ReturnsOnlyUnbundledCompetencies()
        {
            // Act
            var result = await _controller.GetCompetencyBundleAssociations(null, null, null);

            // Assert
            var okResult = Assert.IsType<ActionResult<List<CompetencyBundleAssociationDto>>>(result);
            var competencies = Assert.IsType<List<CompetencyBundleAssociationDto>>(okResult.Value);

            Assert.Single(competencies);
            Assert.Equal("Unbundled Competency", competencies[0].Name);
            Assert.Empty(competencies[0].Bundles);
        }

        [Fact]
        public async Task GetCompetencyBundleAssociations_ClinicalFilterTrue_ReturnsOnlyClinicalCompetencies()
        {
            // Act
            var result = await _controller.GetCompetencyBundleAssociations(clinical: true, assessment: null, milestone: null);

            // Assert
            var okResult = Assert.IsType<ActionResult<List<CompetencyBundleAssociationDto>>>(result);
            var competencies = Assert.IsType<List<CompetencyBundleAssociationDto>>(okResult.Value);

            Assert.Equal(2, competencies.Count);
            Assert.Contains(competencies, c => c.Name == "Clinical Competency");
            Assert.Contains(competencies, c => c.Name == "Multi-Bundle Competency");
            Assert.All(competencies, c => Assert.Contains(c.Bundles, b => b.Clinical));
        }

        [Fact]
        public async Task GetCompetencyBundleAssociations_AssessmentFilterTrue_ReturnsOnlyAssessmentCompetencies()
        {
            // Act
            var result = await _controller.GetCompetencyBundleAssociations(clinical: null, assessment: true, milestone: null);

            // Assert
            var okResult = Assert.IsType<ActionResult<List<CompetencyBundleAssociationDto>>>(result);
            var competencies = Assert.IsType<List<CompetencyBundleAssociationDto>>(okResult.Value);

            Assert.Equal(2, competencies.Count);
            Assert.Contains(competencies, c => c.Name == "Assessment Competency");
            Assert.Contains(competencies, c => c.Name == "Multi-Bundle Competency");
            Assert.All(competencies, c => Assert.Contains(c.Bundles, b => b.Assessment));
        }

        [Fact]
        public async Task GetCompetencyBundleAssociations_MilestoneFilterTrue_ReturnsOnlyMilestoneCompetencies()
        {
            // Act
            var result = await _controller.GetCompetencyBundleAssociations(clinical: null, assessment: null, milestone: true);

            // Assert
            var okResult = Assert.IsType<ActionResult<List<CompetencyBundleAssociationDto>>>(result);
            var competencies = Assert.IsType<List<CompetencyBundleAssociationDto>>(okResult.Value);

            Assert.Single(competencies);
            Assert.Equal("Milestone Competency", competencies[0].Name);
            Assert.Contains(competencies[0].Bundles, b => b.Milestone);
        }

        [Fact]
        public async Task GetCompetencyBundleAssociations_MultipleFiltersTrue_ReturnsCompetenciesMatchingAnyFilter()
        {
            // Act
            var result = await _controller.GetCompetencyBundleAssociations(clinical: true, assessment: true, milestone: false);

            // Assert
            var okResult = Assert.IsType<ActionResult<List<CompetencyBundleAssociationDto>>>(result);
            var competencies = Assert.IsType<List<CompetencyBundleAssociationDto>>(okResult.Value);

            Assert.Equal(3, competencies.Count);
            Assert.Contains(competencies, c => c.Name == "Clinical Competency");
            Assert.Contains(competencies, c => c.Name == "Assessment Competency");
            Assert.Contains(competencies, c => c.Name == "Multi-Bundle Competency");
        }

        [Fact]
        public async Task GetCompetencyBundleAssociations_AllFiltersFalse_ReturnsOnlyUnbundledCompetencies()
        {
            // Act
            var result = await _controller.GetCompetencyBundleAssociations(clinical: false, assessment: false, milestone: false);

            // Assert
            var okResult = Assert.IsType<ActionResult<List<CompetencyBundleAssociationDto>>>(result);
            var competencies = Assert.IsType<List<CompetencyBundleAssociationDto>>(okResult.Value);

            Assert.Single(competencies);
            Assert.Equal("Unbundled Competency", competencies[0].Name);
            Assert.Empty(competencies[0].Bundles);
        }

        [Fact]
        public async Task GetCompetencyBundleAssociations_OrdersByCompetencyOrder()
        {
            // Act
            var result = await _controller.GetCompetencyBundleAssociations(clinical: true, assessment: true, milestone: true);

            // Assert
            var okResult = Assert.IsType<ActionResult<List<CompetencyBundleAssociationDto>>>(result);
            var competencies = Assert.IsType<List<CompetencyBundleAssociationDto>>(okResult.Value);

            // Verify competencies are in order
            for (int i = 0; i < competencies.Count - 1; i++)
            {
                var currentNumber = int.Parse(competencies[i].Number.Split('.')[1]);
                var nextNumber = int.Parse(competencies[i + 1].Number.Split('.')[1]);
                Assert.True(currentNumber < nextNumber, "Competencies should be ordered by their order property");
            }
        }

        [Fact]
        public void CompetencyBundleAssociationDto_MapsAllPropertiesCorrectly()
        {
            // Arrange
            var competency = _testCompetencies[1]; // Clinical competency

            // Act
            var dto = new CompetencyBundleAssociationDto(competency);

            // Assert
            Assert.Equal(competency.CompetencyId, dto.CompetencyId);
            Assert.Equal(competency.Number, dto.Number);
            Assert.Equal(competency.Name, dto.Name);
            Assert.Equal(competency.Domain.Name, dto.DomainName);
            Assert.Single(dto.Bundles);
            Assert.Equal("Clinical Bundle", dto.Bundles[0].Name);
            Assert.True(dto.Bundles[0].Clinical);
        }

        [Fact]
        public void CompetencyBundleAssociationDto_HandlesNullBundleCompetencies()
        {
            // Arrange
            var competency = new Competency
            {
                CompetencyId = 10,
                Number = "2.1",
                Name = "Test",
                BundleCompetencies = null!
            };

            // Act
            var dto = new CompetencyBundleAssociationDto(competency);

            // Assert
            Assert.Empty(dto.Bundles);
        }
    }
}
