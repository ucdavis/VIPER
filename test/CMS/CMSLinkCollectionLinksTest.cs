using Areas.CMS.Models;
using Areas.CMS.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using MockQueryable.NSubstitute;
using NSubstitute;
using Viper.Areas.CMS.Controllers;
using Viper.Classes.SQLContext;

namespace Viper.test.CMS
{
    /// <summary>
    /// Covers the grouped-links ordering in <c>GetLinks</c>: links are grouped by
    /// tag value, de-duplicated, and links whose only tag value in the category is
    /// null are appended (not dropped) after the grouped links.
    /// </summary>
    public class CMSLinkCollectionLinksTest
    {
        private const int CollectionId = 1;
        private const int AudienceCategoryId = 10;
        private const int OtherCategoryId = 99;

        private readonly VIPERContext _mockContext;
        private readonly CMSLinkCollectionLinks _controller;

        private static readonly LinkCollectionTagCategory _audience = new()
        {
            LinkCollectionTagCategoryId = AudienceCategoryId,
            LinkCollectionId = CollectionId,
            LinkCollectionTagCategoryName = "Audience",
            SortOrder = 1,
        };
        private static readonly LinkCollectionTagCategory _other = new()
        {
            LinkCollectionTagCategoryId = OtherCategoryId,
            LinkCollectionId = CollectionId,
            LinkCollectionTagCategoryName = "Other",
            SortOrder = 2,
        };

        public CMSLinkCollectionLinksTest()
        {
            _mockContext = Substitute.For<VIPERContext>();
            _controller = new CMSLinkCollectionLinks(_mockContext);
        }

        private static Link MakeLink(int id, int sortOrder, LinkCollectionTagCategory category, string? tagValue)
        {
            var link = new Link
            {
                LinkId = id,
                LinkCollectionId = CollectionId,
                Url = $"/link/{id}",
                Title = $"Link {id}",
                SortOrder = sortOrder,
            };
            link.LinkTags.Add(new LinkTag
            {
                LinkTagId = id * 10,
                LinkId = id,
                LinkCollectionTagCategoryId = category.LinkCollectionTagCategoryId,
                LinkCollectionTagCategory = category,
                SortOrder = 1,
                Value = tagValue,
                Link = link,
            });
            return link;
        }

        private void Setup(List<Link> links)
        {
            // BuildMockDbSet() makes its own NSubstitute calls, so materialize every
            // mock set before any .Returns() or NSubstitute loses track of the last call.
            var collections = new List<LinkCollection> { new() { LinkCollectionId = CollectionId } }.BuildMockDbSet();
            var categories = new List<LinkCollectionTagCategory> { _audience, _other }.BuildMockDbSet();
            var linkSet = links.BuildMockDbSet();

            _mockContext.LinkCollections.Returns(collections);
            _mockContext.LinkCollectionTagCategories.Returns(categories);
            _mockContext.Links.Returns(linkSet);
        }

        private static Link WithTag(Link link, LinkCollectionTagCategory category, string? value)
        {
            link.LinkTags.Add(new LinkTag
            {
                LinkTagId = link.LinkId * 10 + link.LinkTags.Count,
                LinkId = link.LinkId,
                LinkCollectionTagCategoryId = category.LinkCollectionTagCategoryId,
                LinkCollectionTagCategory = category,
                SortOrder = link.LinkTags.Count + 1,
                Value = value,
                Link = link,
            });
            return link;
        }

        private static List<LinkDto> OkLinks(ActionResult<IEnumerable<LinkDto>> result)
        {
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            return Assert.IsAssignableFrom<IEnumerable<LinkDto>>(ok.Value).ToList();
        }

        [Fact]
        public async Task GetLinks_GroupedByCategory_OrdersByTagValueThenAppendsNullValuedLinks()
        {
            Setup(new List<Link>
            {
                MakeLink(1, 1, _audience, "Students"),
                MakeLink(2, 2, _audience, "Faculty"),
                MakeLink(3, 3, _audience, "Students"),  // shares a value with link 1
                MakeLink(4, 4, _audience, null),         // uncategorized -> appended last
                MakeLink(5, 5, _other, "Other"),         // different category -> excluded
            });

            var dtos = OkLinks(await _controller.GetLinks(CollectionId, "Audience"));

            // Students group (1, 3 in sort order), then Faculty (2), then null-valued (4).
            // Link 5 (other category) is not part of this grouping.
            Assert.Equal(new[] { 1, 3, 2, 4 }, dtos.Select(d => d.LinkId).ToArray());
        }

        [Fact]
        public async Task GetLinks_GroupedByCategory_LinkWithNullAndNonNullValue_AppearsOnceInGroup()
        {
            Setup(new List<Link>
            {
                WithTag(MakeLink(1, 1, _audience, "Students"), _audience, null), // both a value and a null tag
                MakeLink(2, 2, _audience, null),                                  // only null -> uncategorized
            });

            var dtos = OkLinks(await _controller.GetLinks(CollectionId, "Audience"));

            // Link 1 is grouped under "Students" and not re-appended as uncategorized.
            Assert.Equal(new[] { 1, 2 }, dtos.Select(d => d.LinkId).ToArray());
        }

        [Fact]
        public async Task GetLinks_NoGrouping_ReturnsAllLinksInSortOrder()
        {
            Setup(new List<Link>
            {
                MakeLink(1, 1, _audience, "Students"),
                MakeLink(2, 2, _audience, "Faculty"),
                MakeLink(5, 5, _other, "Other"),
            });

            var dtos = OkLinks(await _controller.GetLinks(CollectionId));

            Assert.Equal(new[] { 1, 2, 5 }, dtos.Select(d => d.LinkId).ToArray());
        }

        [Fact]
        public async Task GetLinks_UnknownCollection_ReturnsNotFound()
        {
            Setup(new List<Link> { MakeLink(1, 1, _audience, "Students") });

            var result = await _controller.GetLinks(999);

            Assert.IsType<NotFoundResult>(result.Result);
        }
    }
}
