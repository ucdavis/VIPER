using Areas.CMS.Models;
using Areas.CMS.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
                .Where(lc => linkCollectionName == null || lc.LinkCollectionName.ToLower() == linkCollectionName.ToLower())
                .OrderBy(lc => lc.LinkCollectionName)
                .ToListAsync();

            var result = collections.Select(lc => new LinkCollectionDto
            {
                LinkCollectionId = lc.LinkCollectionId,
                LinkCollection = lc.LinkCollectionName,
                LinkCollectionTagCategories = lc.LinkCollectionTagCategories.Select(tc => new LinkCollectionTagCategoryDto
                {
                    LinkCollectionTagCategoryId = tc.LinkCollectionTagCategoryId,
                    LinkCollectionId = tc.LinkCollectionId,
                    LinkCollectionTagCategory = tc.LinkCollectionTagCategoryName,
                    SortOrder = tc.SortOrder
                }).ToList()
            });

            return Ok(result);
        }

        [Permission(Allow = "SVMSecure.CMS.ManageContentBlocks")]
        [HttpPost]
        public async Task<ActionResult<LinkCollectionDto>> PostLinkCollection(CreateLinkCollectionDto createDto)
        {
            var dupeCheck = await _context.LinkCollections
                .FirstOrDefaultAsync(lc => lc.LinkCollectionName.ToLower() == createDto.LinkCollection.ToLower());
            if (dupeCheck != null)
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

            return CreatedAtAction(nameof(linkCollection), new { id = linkCollection.LinkCollectionId }, result);
        }

        [Permission(Allow = "SVMSecure.CMS.ManageContentBlocks")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutLinkCollection(int id, CreateLinkCollectionDto updateDto)
        {
            var linkCollection = await _context.LinkCollections.FindAsync(id);
            if (linkCollection == null)
            {
                return NotFound();
            }

            var dupeCheck = await _context.LinkCollections
                .FirstOrDefaultAsync(lc => lc.LinkCollectionName.ToLower() == updateDto.LinkCollection.ToLower()
                    && lc.LinkCollectionId != id);
            if (dupeCheck != null)
            {
                return BadRequest("A link collection with that name already exists.");
            }

            linkCollection.LinkCollectionName = updateDto.LinkCollection;
            await _context.SaveChangesAsync();

            return Ok(updateDto);
        }

        [Permission(Allow = "SVMSecure.CMS.ManageContentBlocks")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLinkCollection(int id)
        {
            var linkCollection = await _context.LinkCollections.FindAsync(id);
            if (linkCollection == null)
            {
                return NotFound();
            }

            _context.LinkCollections.Remove(linkCollection);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        //Tags:
        [HttpGet("{collectionId}/tags")]
        public async Task<ActionResult<IEnumerable<LinkCollectionTagCategoryDto>>> GetLinkCollectionTagCategories(int collectionId)
        {
            if (_context.LinkCollections.FirstOrDefault(lc => lc.LinkCollectionId == collectionId) == null)
            {
                return NotFound();
            }

            var categories = await _context.LinkCollectionTagCategories
                .Where(lc => lc.LinkCollectionId == collectionId)
                .OrderBy(lc => lc.SortOrder)
                .ToListAsync();

            var results = categories
                .Select(c => new LinkCollectionTagCategoryDto
                {
                    LinkCollectionTagCategoryId = c.LinkCollectionTagCategoryId,
                    LinkCollectionId = c.LinkCollectionId,
                    LinkCollectionTagCategory= c.LinkCollectionTagCategoryName,
                    SortOrder = c.SortOrder
                })
                .ToList();

            return Ok(results);
        }

        [Permission(Allow = "SVMSecure.CMS.ManageContentBlocks")]
        [HttpPost("{collectionId}/tags")]
        public async Task<ActionResult<CreateLinkCollectionTagCategoryDto>> CreateLinkCollectionTagCategory(int collectionId, CreateLinkCollectionTagCategoryDto createLinkTagCategoryDto)
        {
            if(_context.LinkCollections.FirstOrDefault(lc => lc.LinkCollectionId == collectionId) == null)
            {
                return NotFound();
            }

            var dupeCheck = await _context.LinkCollectionTagCategories
                .FirstOrDefaultAsync(tc => tc.LinkCollectionTagCategoryName.ToLower() == createLinkTagCategoryDto.LinkCollectionTagCategory.ToLower()
                    && tc.LinkCollectionId == collectionId);
            if (dupeCheck != null)
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
            return CreatedAtAction(nameof(tagCategory), new { id = tagCategory.LinkCollectionTagCategoryId }, createLinkTagCategoryDto);
        }

        [Permission(Allow = "SVMSecure.CMS.ManageContentBlocks")]
        [HttpDelete("{collectionId}/tags/{tagCategoryId}")]
        public async Task<IActionResult> DeleteLinkCollectionTagCategory(int collectionId, int tagCategoryId)
        {
            if (_context.LinkCollections.FirstOrDefault(lc => lc.LinkCollectionId == collectionId) == null)
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
    }
}
