using Areas.CMS.Models;
using Areas.CMS.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Viper.Areas.CMS.Controllers;
using Viper.Classes.SQLContext;

namespace Viper.test.CMS;

/// <summary>
/// Tests for CMSLinkCollectionLinks, which holds its EF logic directly (no service layer).
/// Covers link CRUD, grouped-by-tag-category listing, link reordering, and the batch
/// link-tag save (remove-and-recreate). A fresh EF in-memory VIPERContext is used per test;
/// the in-memory provider's transaction warning is ignored so SaveLinkTags' transaction runs.
/// </summary>
public sealed class CMSLinkCollectionLinksTests : IDisposable
{
    private readonly VIPERContext _context;
    private readonly CMSLinkCollectionLinks _controller;

    public CMSLinkCollectionLinksTests()
    {
        _context = new VIPERContext(new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase("VIPER_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);
        _controller = new CMSLinkCollectionLinks(_context);
    }

    public void Dispose() => _context.Dispose();

    private async Task<LinkCollection> SeedCollectionAsync(string name = "Resources")
    {
        var collection = new LinkCollection { LinkCollectionName = name };
        _context.LinkCollections.Add(collection);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return collection;
    }

    private async Task<Link> SeedLinkAsync(int collectionId, string title, int sortOrder)
    {
        var link = new Link
        {
            LinkCollectionId = collectionId,
            Url = "https://example.com/" + title,
            Title = title,
            SortOrder = sortOrder
        };
        _context.Links.Add(link);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return link;
    }

    private async Task<LinkCollectionTagCategory> SeedTagCategoryAsync(int collectionId, string name, int sortOrder)
    {
        var category = new LinkCollectionTagCategory
        {
            LinkCollectionId = collectionId,
            LinkCollectionTagCategoryName = name,
            SortOrder = sortOrder
        };
        _context.LinkCollectionTagCategories.Add(category);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return category;
    }

    private async Task SeedLinkTagAsync(int linkId, int categoryId, string value, int sortOrder)
    {
        _context.LinkTags.Add(new LinkTag
        {
            LinkId = linkId,
            LinkCollectionTagCategoryId = categoryId,
            Value = value,
            SortOrder = sortOrder
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    #region GetLinks

    [Fact]
    public async Task GetLinks_ReturnsLinksOrderedBySortOrder()
    {
        var collection = await SeedCollectionAsync();
        await SeedLinkAsync(collection.LinkCollectionId, "Second", 2);
        await SeedLinkAsync(collection.LinkCollectionId, "First", 1);

        var result = await _controller.GetLinks(collection.LinkCollectionId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var links = Assert.IsAssignableFrom<IEnumerable<LinkDto>>(ok.Value).ToList();
        Assert.Equal(new[] { "First", "Second" }, links.Select(l => l.Title));
    }

    [Fact]
    public async Task GetLinks_ReturnsNotFound_WhenCollectionMissing()
    {
        var result = await _controller.GetLinks(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetLinks_GroupByTagCategory_GroupsLinksSharingATagValue()
    {
        var collection = await SeedCollectionAsync();
        var category = await SeedTagCategoryAsync(collection.LinkCollectionId, "Region", 1);
        // Two links share the value "Coast"; one has "Inland". Grouping by the category emits
        // each distinct value's links together, so the two "Coast" links must be adjacent.
        var first = await SeedLinkAsync(collection.LinkCollectionId, "First", 1);
        var inland = await SeedLinkAsync(collection.LinkCollectionId, "Inland", 2);
        var third = await SeedLinkAsync(collection.LinkCollectionId, "Third", 3);
        await SeedLinkTagAsync(first.LinkId, category.LinkCollectionTagCategoryId, "Coast", 1);
        await SeedLinkTagAsync(inland.LinkId, category.LinkCollectionTagCategoryId, "Inland", 1);
        await SeedLinkTagAsync(third.LinkId, category.LinkCollectionTagCategoryId, "Coast", 1);

        var result = await _controller.GetLinks(collection.LinkCollectionId, groupByTagCategory: "region");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var titles = Assert.IsAssignableFrom<IEnumerable<LinkDto>>(ok.Value).Select(l => l.Title).ToList();
        Assert.Equal(3, titles.Count);
        Assert.Equal(new[] { "First", "Inland", "Third" }, titles.OrderBy(t => t));
        // The two "Coast" links group together regardless of their SortOrder.
        Assert.Equal(1, Math.Abs(titles.IndexOf("First") - titles.IndexOf("Third")));
    }

    [Fact]
    public async Task GetLinks_GroupByUnknownTagCategory_ReturnsBadRequest()
    {
        var collection = await SeedCollectionAsync();
        await SeedLinkAsync(collection.LinkCollectionId, "A", 1);

        var result = await _controller.GetLinks(collection.LinkCollectionId, groupByTagCategory: "missing");

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    #endregion

    #region Link CRUD

    [Fact]
    public async Task PostLink_CreatesLink()
    {
        var collection = await SeedCollectionAsync();
        var dto = new CreateLinkDto
        {
            LinkCollectionId = collection.LinkCollectionId,
            Url = "https://example.com",
            Title = "Example",
            SortOrder = 1
        };

        var result = await _controller.PostLink(collection.LinkCollectionId, dto);

        Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Single(await _context.Links.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PostLink_ReturnsBadRequest_WhenCollectionIdMismatch()
    {
        var collection = await SeedCollectionAsync();
        var dto = new CreateLinkDto
        {
            LinkCollectionId = collection.LinkCollectionId + 1,
            Url = "https://example.com",
            Title = "Example",
            SortOrder = 1
        };

        var result = await _controller.PostLink(collection.LinkCollectionId, dto);

        Assert.IsType<BadRequestResult>(result.Result);
    }

    [Fact]
    public async Task PutLink_UpdatesLink()
    {
        var collection = await SeedCollectionAsync();
        var link = await SeedLinkAsync(collection.LinkCollectionId, "Old", 1);
        var dto = new CreateLinkDto
        {
            LinkCollectionId = collection.LinkCollectionId,
            Url = "https://example.com/new",
            Title = "New",
            SortOrder = 5
        };

        var result = await _controller.PutLink(link.LinkId, collection.LinkCollectionId, dto);

        Assert.IsType<NoContentResult>(result);
        var saved = await _context.Links.FindAsync(new object[] { link.LinkId }, TestContext.Current.CancellationToken);
        Assert.Equal("New", saved!.Title);
        Assert.Equal(5, saved.SortOrder);
    }

    [Fact]
    public async Task PutLink_ReturnsNotFound_WhenLinkMissing()
    {
        var collection = await SeedCollectionAsync();
        var dto = new CreateLinkDto
        {
            LinkCollectionId = collection.LinkCollectionId,
            Url = "https://example.com",
            Title = "X",
            SortOrder = 1
        };

        var result = await _controller.PutLink(999, collection.LinkCollectionId, dto);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task PutLink_ReturnsBadRequest_WhenCollectionMismatch()
    {
        var collection = await SeedCollectionAsync();
        var link = await SeedLinkAsync(collection.LinkCollectionId, "Old", 1);
        var dto = new CreateLinkDto
        {
            LinkCollectionId = collection.LinkCollectionId + 1,
            Url = "https://example.com",
            Title = "X",
            SortOrder = 1
        };

        var result = await _controller.PutLink(link.LinkId, collection.LinkCollectionId + 1, dto);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task DeleteLink_RemovesLinkAndTags()
    {
        var collection = await SeedCollectionAsync();
        var category = await SeedTagCategoryAsync(collection.LinkCollectionId, "Region", 1);
        var link = await SeedLinkAsync(collection.LinkCollectionId, "ToDelete", 1);
        await SeedLinkTagAsync(link.LinkId, category.LinkCollectionTagCategoryId, "West", 1);

        var result = await _controller.DeleteLink(collection.LinkCollectionId, link.LinkId);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(await _context.Links.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await _context.LinkTags.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteLink_ReturnsBadRequest_WhenCollectionMismatch()
    {
        var collection = await SeedCollectionAsync();
        var link = await SeedLinkAsync(collection.LinkCollectionId, "X", 1);

        var result = await _controller.DeleteLink(collection.LinkCollectionId + 1, link.LinkId);

        Assert.IsType<BadRequestResult>(result);
        Assert.Single(await _context.Links.ToListAsync(TestContext.Current.CancellationToken));
    }

    #endregion

    #region Reorder

    [Fact]
    public async Task UpdateLinkOrder_ReassignsSortOrder()
    {
        var collection = await SeedCollectionAsync();
        var first = await SeedLinkAsync(collection.LinkCollectionId, "First", 1);
        var second = await SeedLinkAsync(collection.LinkCollectionId, "Second", 2);
        var updates = new List<UpdateLinkOrderDto>
        {
            new() { LinkId = first.LinkId, SortOrder = 2 },
            new() { LinkId = second.LinkId, SortOrder = 1 }
        };

        var result = await _controller.UpdateLinkOrder(collection.LinkCollectionId, updates);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(2, (await _context.Links.FindAsync(
            new object[] { first.LinkId }, TestContext.Current.CancellationToken))!.SortOrder);
        Assert.Equal(1, (await _context.Links.FindAsync(
            new object[] { second.LinkId }, TestContext.Current.CancellationToken))!.SortOrder);
    }

    [Fact]
    public async Task UpdateLinkOrder_ReturnsNotFound_WhenCollectionMissing()
    {
        var result = await _controller.UpdateLinkOrder(999, new List<UpdateLinkOrderDto>());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateLinkOrder_RejectsCountMismatch()
    {
        var collection = await SeedCollectionAsync();
        await SeedLinkAsync(collection.LinkCollectionId, "First", 1);
        await SeedLinkAsync(collection.LinkCollectionId, "Second", 2);
        var updates = new List<UpdateLinkOrderDto> { new() { LinkId = 1, SortOrder = 1 } };

        var result = await _controller.UpdateLinkOrder(collection.LinkCollectionId, updates);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region SaveLinkTags

    [Fact]
    public async Task SaveLinkTags_ReplacesTagsFromCommaSeparatedValues()
    {
        var collection = await SeedCollectionAsync();
        var category = await SeedTagCategoryAsync(collection.LinkCollectionId, "Region", 1);
        var link = await SeedLinkAsync(collection.LinkCollectionId, "Link", 1);
        await SeedLinkTagAsync(link.LinkId, category.LinkCollectionTagCategoryId, "Stale", 1);
        var tagValues = new Dictionary<int, string>
        {
            { category.LinkCollectionTagCategoryId, "West,East" }
        };

        var result = await _controller.SaveLinkTags(link.LinkId, collection.LinkCollectionId, tagValues);

        Assert.IsType<NoContentResult>(result);
        var tags = await _context.LinkTags.Where(lt => lt.LinkId == link.LinkId)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, tags.Count);
        Assert.DoesNotContain(tags, t => t.Value == "Stale");
        Assert.Contains(tags, t => t.Value == "West");
        Assert.Contains(tags, t => t.Value == "East");
    }

    [Fact]
    public async Task SaveLinkTags_ReturnsNotFound_WhenLinkMissing()
    {
        var collection = await SeedCollectionAsync();

        var result = await _controller.SaveLinkTags(999, collection.LinkCollectionId, new Dictionary<int, string>());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task SaveLinkTags_ReturnsBadRequest_WhenCollectionMismatch()
    {
        var collection = await SeedCollectionAsync();
        var link = await SeedLinkAsync(collection.LinkCollectionId, "Link", 1);

        var result = await _controller.SaveLinkTags(link.LinkId, collection.LinkCollectionId + 1,
            new Dictionary<int, string>());

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task SaveLinkTags_ReturnsBadRequest_WhenTagCategoryNotInCollection()
    {
        var collection = await SeedCollectionAsync();
        var link = await SeedLinkAsync(collection.LinkCollectionId, "Link", 1);
        var tagValues = new Dictionary<int, string> { { 9999, "West" } };

        var result = await _controller.SaveLinkTags(link.LinkId, collection.LinkCollectionId, tagValues);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion
}
