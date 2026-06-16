using Areas.CMS.Models;
using Areas.CMS.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.CMS.Controllers;
using Viper.Classes.SQLContext;

namespace Viper.test.CMS;

/// <summary>
/// Tests for CMSLinkCollectionController, which holds its EF logic directly (no service layer).
/// Covers collection CRUD with duplicate-name rejection and tag-category create/reorder/delete,
/// using a fresh EF in-memory VIPERContext per test.
/// </summary>
public sealed class CMSLinkCollectionControllerTests : IDisposable
{
    private readonly VIPERContext _context;
    private readonly CMSLinkCollectionController _controller;

    public CMSLinkCollectionControllerTests()
    {
        _context = new VIPERContext(new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase("VIPER_" + Guid.NewGuid()).Options);
        _controller = new CMSLinkCollectionController(_context);
    }

    public void Dispose() => _context.Dispose();

    private async Task<LinkCollection> SeedCollectionAsync(string name = "Resources")
    {
        var collection = new LinkCollection { LinkCollectionName = name };
        _context.LinkCollections.Add(collection);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return collection;
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

    #region Collections

    [Fact]
    public async Task GetLinkCollections_ReturnsOrderedWithTagCategories()
    {
        var beta = await SeedCollectionAsync("Beta");
        await SeedCollectionAsync("Alpha");
        await SeedTagCategoryAsync(beta.LinkCollectionId, "Region", 2);
        await SeedTagCategoryAsync(beta.LinkCollectionId, "Topic", 1);

        var result = await _controller.GetLinkCollections();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var collections = Assert.IsAssignableFrom<IEnumerable<LinkCollectionDto>>(ok.Value).ToList();
        // Collections are ordered by name at the top level.
        Assert.Equal(new[] { "Alpha", "Beta" }, collections.Select(c => c.LinkCollection));
        var betaDto = collections.Single(c => c.LinkCollection == "Beta");
        // Both nested tag categories are projected with their SortOrder (the in-memory provider
        // does not honor the filtered-include OrderBy, so assert membership, not sequence).
        Assert.Equal(new[] { "Region", "Topic" },
            betaDto.LinkCollectionTagCategories.Select(tc => tc.LinkCollectionTagCategory).OrderBy(n => n));
        Assert.Equal(1, betaDto.LinkCollectionTagCategories.Single(tc => tc.LinkCollectionTagCategory == "Topic").SortOrder);
    }

    [Fact]
    public async Task PostLinkCollection_CreatesCollection()
    {
        var result = await _controller.PostLinkCollection(new CreateLinkCollectionDto { LinkCollection = "New" });

        Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.True(await _context.LinkCollections.AnyAsync(lc => lc.LinkCollectionName == "New",
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PostLinkCollection_RejectsDuplicateName()
    {
        await SeedCollectionAsync("Resources");

        var result = await _controller.PostLinkCollection(new CreateLinkCollectionDto { LinkCollection = "Resources" });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("already exists", badRequest.Value?.ToString());
        Assert.Single(await _context.LinkCollections.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PutLinkCollection_RenamesCollection()
    {
        var collection = await SeedCollectionAsync("Old Name");

        var result = await _controller.PutLinkCollection(collection.LinkCollectionId,
            new CreateLinkCollectionDto { LinkCollection = "New Name" });

        Assert.IsType<OkObjectResult>(result);
        var saved = await _context.LinkCollections.FindAsync(
            new object[] { collection.LinkCollectionId }, TestContext.Current.CancellationToken);
        Assert.Equal("New Name", saved!.LinkCollectionName);
    }

    [Fact]
    public async Task PutLinkCollection_ReturnsNotFound_WhenMissing()
    {
        var result = await _controller.PutLinkCollection(999, new CreateLinkCollectionDto { LinkCollection = "X" });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task PutLinkCollection_RejectsRenameToExistingName()
    {
        await SeedCollectionAsync("Taken");
        var collection = await SeedCollectionAsync("Mine");

        var result = await _controller.PutLinkCollection(collection.LinkCollectionId,
            new CreateLinkCollectionDto { LinkCollection = "Taken" });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteLinkCollection_RemovesCollection()
    {
        var collection = await SeedCollectionAsync();

        var result = await _controller.DeleteLinkCollection(collection.LinkCollectionId);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(await _context.LinkCollections.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteLinkCollection_ReturnsNotFound_WhenMissing()
    {
        var result = await _controller.DeleteLinkCollection(999);

        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region Tag categories

    [Fact]
    public async Task GetLinkCollectionTagCategories_ReturnsOrdered()
    {
        var collection = await SeedCollectionAsync();
        await SeedTagCategoryAsync(collection.LinkCollectionId, "Second", 2);
        await SeedTagCategoryAsync(collection.LinkCollectionId, "First", 1);

        var result = await _controller.GetLinkCollectionTagCategories(collection.LinkCollectionId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var categories = Assert.IsAssignableFrom<IEnumerable<LinkCollectionTagCategoryDto>>(ok.Value).ToList();
        Assert.Equal(new[] { "First", "Second" }, categories.Select(c => c.LinkCollectionTagCategory));
    }

    [Fact]
    public async Task GetLinkCollectionTagCategories_ReturnsNotFound_WhenCollectionMissing()
    {
        var result = await _controller.GetLinkCollectionTagCategories(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateLinkCollectionTagCategory_AddsCategory()
    {
        var collection = await SeedCollectionAsync();
        var dto = new CreateLinkCollectionTagCategoryDto
        {
            LinkCollectionId = collection.LinkCollectionId,
            LinkCollectionTagCategory = "Topic",
            SortOrder = 1
        };

        var result = await _controller.CreateLinkCollectionTagCategory(collection.LinkCollectionId, dto);

        Assert.IsType<CreatedAtActionResult>(result.Result);
        var saved = await _context.LinkCollectionTagCategories.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Topic", saved.LinkCollectionTagCategoryName);
        Assert.Equal(collection.LinkCollectionId, saved.LinkCollectionId);
    }

    [Fact]
    public async Task CreateLinkCollectionTagCategory_ReturnsNotFound_WhenCollectionMissing()
    {
        var dto = new CreateLinkCollectionTagCategoryDto
        {
            LinkCollectionId = 999,
            LinkCollectionTagCategory = "Topic",
            SortOrder = 1
        };

        var result = await _controller.CreateLinkCollectionTagCategory(999, dto);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateLinkCollectionTagCategory_RejectsDuplicateNameInCollection()
    {
        var collection = await SeedCollectionAsync();
        await SeedTagCategoryAsync(collection.LinkCollectionId, "Topic", 1);
        var dto = new CreateLinkCollectionTagCategoryDto
        {
            LinkCollectionId = collection.LinkCollectionId,
            LinkCollectionTagCategory = "Topic",
            SortOrder = 2
        };

        var result = await _controller.CreateLinkCollectionTagCategory(collection.LinkCollectionId, dto);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateLinkCollectionTagCategoryOrder_ReassignsSortOrder()
    {
        var collection = await SeedCollectionAsync();
        var first = await SeedTagCategoryAsync(collection.LinkCollectionId, "First", 1);
        var second = await SeedTagCategoryAsync(collection.LinkCollectionId, "Second", 2);
        var updates = new List<UpdateLinkCollectionTagCategoryOrderDto>
        {
            new() { LinkCollectionTagCategoryId = first.LinkCollectionTagCategoryId, SortOrder = 2 },
            new() { LinkCollectionTagCategoryId = second.LinkCollectionTagCategoryId, SortOrder = 1 }
        };

        var result = await _controller.UpdateLinkCollectionTagCategoryOrder(collection.LinkCollectionId, updates);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(2, (await _context.LinkCollectionTagCategories.FindAsync(
            new object[] { first.LinkCollectionTagCategoryId }, TestContext.Current.CancellationToken))!.SortOrder);
        Assert.Equal(1, (await _context.LinkCollectionTagCategories.FindAsync(
            new object[] { second.LinkCollectionTagCategoryId }, TestContext.Current.CancellationToken))!.SortOrder);
    }

    [Fact]
    public async Task UpdateLinkCollectionTagCategoryOrder_RejectsCountMismatch()
    {
        var collection = await SeedCollectionAsync();
        await SeedTagCategoryAsync(collection.LinkCollectionId, "First", 1);
        await SeedTagCategoryAsync(collection.LinkCollectionId, "Second", 2);
        var updates = new List<UpdateLinkCollectionTagCategoryOrderDto>
        {
            new() { LinkCollectionTagCategoryId = 1, SortOrder = 1 }
        };

        var result = await _controller.UpdateLinkCollectionTagCategoryOrder(collection.LinkCollectionId, updates);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteLinkCollectionTagCategory_RemovesCategory()
    {
        var collection = await SeedCollectionAsync();
        var category = await SeedTagCategoryAsync(collection.LinkCollectionId, "Topic", 1);

        var result = await _controller.DeleteLinkCollectionTagCategory(collection.LinkCollectionId,
            category.LinkCollectionTagCategoryId);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(await _context.LinkCollectionTagCategories.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteLinkCollectionTagCategory_ReturnsNotFound_WhenCategoryMissing()
    {
        var collection = await SeedCollectionAsync();

        var result = await _controller.DeleteLinkCollectionTagCategory(collection.LinkCollectionId, 999);

        Assert.IsType<NotFoundResult>(result);
    }

    #endregion
}
