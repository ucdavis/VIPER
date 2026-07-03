using Areas.CMS.Models;
using Areas.CMS.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.CMS.Constants;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Web.Authorization;

namespace Viper.Areas.CMS.Controllers
{
    [Route("/api/cms/linkCollections")]
    [Permission(Allow = "SVMSecure")]
    public class CMSLinkCollectionController : ApiController
    {
        private readonly VIPERContext _context;
        public CMSLinkCollectionController(VIPERContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LinkCollectionDto>>> GetLinkCollections(string? linkCollectionName = null)
        {
            var collections = await _context.LinkCollections
                .Include(lc => lc.LinkCollectionTagCategories.OrderBy(tc => tc.SortOrder))
                .Where(lc => linkCollectionName == null || lc.LinkCollectionName == linkCollectionName)
                .OrderBy(lc => lc.LinkCollectionName)
                .ToListAsync();

            var result = collections.Select(lc => new LinkCollectionDto
            {
                LinkCollectionId = lc.LinkCollectionId,
                LinkCollection = lc.LinkCollectionName,
                LinkCollectionTagCategories = lc.LinkCollectionTagCategories.Select(ToTagCategoryDto).ToList()
            });

            return Ok(result);
        }

        [Permission(Allow = CmsPermissions.ManageContentBlocks)]
        [HttpPost]
        public async Task<ActionResult<LinkCollectionDto>> PostLinkCollection(CreateLinkCollectionDto createDto)
        {
            if (await _context.LinkCollections
                .AnyAsync(lc => lc.LinkCollectionName == createDto.LinkCollection))
            {
                return BadRequest("A link collection with that name already exists.");
            }

            var linkCollection = new LinkCollection
            {
                LinkCollectionName = createDto.LinkCollection
            };

            _context.LinkCollections.Add(linkCollection);
            await _context.SaveChangesAsync();

            var result = new LinkCollectionDto
            {
                LinkCollectionId = linkCollection.LinkCollectionId,
                LinkCollection = linkCollection.LinkCollectionName,
                LinkCollectionTagCategories = new List<LinkCollectionTagCategoryDto>()
            };

            return CreatedAtAction(nameof(GetLinkCollections), new { linkCollectionName = linkCollection.LinkCollectionName }, result);
        }

        [Permission(Allow = CmsPermissions.ManageContentBlocks)]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutLinkCollection(int id, CreateLinkCollectionDto updateDto)
        {
            var linkCollection = await _context.LinkCollections.FindAsync(id);
            if (linkCollection == null)
            {
                return NotFound();
            }

            if (await _context.LinkCollections
                .AnyAsync(lc => lc.LinkCollectionName == updateDto.LinkCollection
                    && lc.LinkCollectionId != id))
            {
                return BadRequest("A link collection with that name already exists.");
            }

            linkCollection.LinkCollectionName = updateDto.LinkCollection;
            await _context.SaveChangesAsync();

            return Ok(updateDto);
        }

        [Permission(Allow = CmsPermissions.ManageContentBlocks)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLinkCollection(int id)
        {
            var linkCollection = await _context.LinkCollections.FindAsync(id);
            if (linkCollection == null)
            {
                return NotFound();
            }

            // FK constraints are not ON DELETE CASCADE, so remove dependents before
            // the collection. Queuing all removals into a single SaveChanges keeps it
            // atomic. Order: link tags -> links -> tag categories -> collection.
            var collectionLinks = await _context.Links
                .Where(l => l.LinkCollectionId == id)
                .ToListAsync();
            var collectionLinkIds = collectionLinks.Select(l => l.LinkId).ToList();
            _context.LinkTags.RemoveRange(
                _context.LinkTags.Where(lt => collectionLinkIds.Contains(lt.LinkId)));
            _context.Links.RemoveRange(collectionLinks);
            _context.LinkCollectionTagCategories.RemoveRange(
                _context.LinkCollectionTagCategories.Where(tc => tc.LinkCollectionId == id));
            _context.LinkCollections.Remove(linkCollection);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        //Tags:
        [HttpGet("{collectionId}/tags")]
        public async Task<ActionResult<IEnumerable<LinkCollectionTagCategoryDto>>> GetLinkCollectionTagCategories(int collectionId)
        {
            if (!await _context.LinkCollections.AnyAsync(lc => lc.LinkCollectionId == collectionId))
            {
                return NotFound();
            }

            var categories = await _context.LinkCollectionTagCategories
                .Where(lc => lc.LinkCollectionId == collectionId)
                .OrderBy(lc => lc.SortOrder)
                .ToListAsync();

            var results = categories.Select(ToTagCategoryDto).ToList();

            return Ok(results);
        }

        [Permission(Allow = CmsPermissions.ManageContentBlocks)]
        [HttpPost("{collectionId}/tags")]
        public async Task<ActionResult<LinkCollectionTagCategoryDto>> CreateLinkCollectionTagCategory(int collectionId, CreateLinkCollectionTagCategoryDto createLinkTagCategoryDto)
        {
            if (!await _context.LinkCollections.AnyAsync(lc => lc.LinkCollectionId == collectionId))
            {
                return NotFound();
            }

            if (createLinkTagCategoryDto.LinkCollectionId != collectionId)
            {
                return BadRequest("Collection ID in route does not match the request body.");
            }

            if (await _context.LinkCollectionTagCategories
                .AnyAsync(tc => tc.LinkCollectionTagCategoryName == createLinkTagCategoryDto.LinkCollectionTagCategory
                    && tc.LinkCollectionId == collectionId))
            {
                return BadRequest("A tag category with that name already exists in this link collection.");
            }

            var tagCategory = new LinkCollectionTagCategory
            {
                LinkCollectionId = collectionId,
                LinkCollectionTagCategoryName = createLinkTagCategoryDto.LinkCollectionTagCategory,
                SortOrder = createLinkTagCategoryDto.SortOrder
            };
            _context.LinkCollectionTagCategories.Add(tagCategory);
            await _context.SaveChangesAsync();

            // Return the saved DTO so the client receives the generated id. The follow-up
            // tag-order PUT needs that id to address the newly created category.
            return CreatedAtAction(nameof(GetLinkCollectionTagCategories), new { collectionId }, ToTagCategoryDto(tagCategory));
        }

        [Permission(Allow = CmsPermissions.ManageContentBlocks)]
        [HttpPut("{collectionId}/tags/order")]
        public async Task<IActionResult> UpdateLinkCollectionTagCategoryOrder(int collectionId, List<UpdateLinkCollectionTagCategoryOrderDto> updateDto)
        {
            if (!await _context.LinkCollections.AnyAsync(lc => lc.LinkCollectionId == collectionId))
            {
                return NotFound();
            }

            var tagCategories = await _context.LinkCollectionTagCategories
                .Where(tc => tc.LinkCollectionId == collectionId)
                .ToListAsync();

            if (tagCategories.Count != updateDto.Count)
            {
                return BadRequest("Mismatch in number of tag categories.");
            }

            foreach (var dto in updateDto)
            {
                var tagCategory = tagCategories.FirstOrDefault(tc => tc.LinkCollectionTagCategoryId == dto.LinkCollectionTagCategoryId);
                if (tagCategory == null)
                {
                    return BadRequest($"Tag category with ID {dto.LinkCollectionTagCategoryId} not found in this collection.");
                }
                tagCategory.SortOrder = dto.SortOrder;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [Permission(Allow = CmsPermissions.ManageContentBlocks)]
        [HttpDelete("{collectionId}/tags/{tagCategoryId}")]
        public async Task<IActionResult> DeleteLinkCollectionTagCategory(int collectionId, int tagCategoryId)
        {
            if (!await _context.LinkCollections.AnyAsync(lc => lc.LinkCollectionId == collectionId))
            {
                return NotFound();
            }
            var tagCategory = await _context.LinkCollectionTagCategories
                .FirstOrDefaultAsync(tc => tc.LinkCollectionTagCategoryId == tagCategoryId && tc.LinkCollectionId == collectionId);
            if (tagCategory == null)
            {
                return NotFound();
            }
            _context.LinkCollectionTagCategories.Remove(tagCategory);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private static LinkCollectionTagCategoryDto ToTagCategoryDto(LinkCollectionTagCategory tagCategory) => new()
        {
            LinkCollectionTagCategoryId = tagCategory.LinkCollectionTagCategoryId,
            LinkCollectionId = tagCategory.LinkCollectionId,
            LinkCollectionTagCategory = tagCategory.LinkCollectionTagCategoryName,
            SortOrder = tagCategory.SortOrder
        };
    }
}
