using MockQueryable.NSubstitute;
using NSubstitute;
using Viper.Areas.CTS.Controllers;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;

namespace Viper.test.CTS
{
    public class CompetencyControllerTest
    {
        private readonly VIPERContext _mockContext;
        private readonly CompetencyController _controller;
        private static readonly Domain _domain = new() { DomainId = 1, Name = "Domain", Order = 1 };

        public CompetencyControllerTest()
        {
            _mockContext = Substitute.For<VIPERContext>();
            _controller = new CompetencyController(_mockContext);
        }

        private void SetupCompetencies(params Competency[] comps)
        {
            // BuildMockDbSet() makes its own NSubstitute calls, so materialize it
            // before .Returns() or NSubstitute loses track of the last call.
            var mockDbSet = comps.ToList().BuildMockDbSet();
            _mockContext.Competencies.Returns(mockDbSet);
        }

        private static Competency Comp(int id, string number, int? parentId, int order)
            => new()
            {
                CompetencyId = id,
                Number = number,
                Name = $"Competency {number}",
                DomainId = _domain.DomainId,
                Domain = _domain,
                ParentId = parentId,
                Order = order,
            };

        [Fact]
        public async Task GetCompetencyHierarchy_NestsChildrenUnderParents()
        {
            SetupCompetencies(
                Comp(1, "1", null, 1),      // root
                Comp(2, "1.1", 1, 2),       // child of 1
                Comp(3, "1.1.1", 2, 3),     // grandchild (child of 2)
                Comp(4, "2", null, 4));     // second root

            var result = await _controller.GetCompetencyHierarchy();

            var roots = result.Value!;
            Assert.Equal(new[] { 1, 4 }, roots.Select(r => r.CompetencyId).ToArray());

            var root1 = roots.Single(c => c.CompetencyId == 1);
            var child = Assert.Single(root1.Children);
            Assert.Equal(2, child.CompetencyId);
            var grandchild = Assert.Single(child.Children);
            Assert.Equal(3, grandchild.CompetencyId);

            Assert.Empty(roots.Single(c => c.CompetencyId == 4).Children);
        }

        [Fact]
        public async Task GetCompetencyHierarchy_OrphanWithMissingParent_IsDroppedNotDuplicated()
        {
            SetupCompetencies(
                Comp(1, "1", null, 1),       // root
                Comp(2, "9.9", 999, 2));     // parent 999 is not in the set

            var result = await _controller.GetCompetencyHierarchy();

            var roots = result.Value!;
            var root = Assert.Single(roots);
            Assert.Equal(1, root.CompetencyId);
            Assert.Empty(root.Children);
        }
    }
}
