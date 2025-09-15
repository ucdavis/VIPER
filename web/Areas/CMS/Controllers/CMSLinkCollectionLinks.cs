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
    public class CMSLinkCollectionLinks : ApiController
    {
        private readonly VIPERContext _context;
        public CMSLinkCollectionLinks(VIPERContext context)
        {
            _context = context;
        }

        [HttpGet("{collectionId}/links")]
        public async Task<ActionResult<IEnumerable<LinkDto>>> GetLinks(int collectionId)
        {
            if (_context.LinkCollections.FirstOrDefault(lc => lc.LinkCollectionId == collectionId) == null)
            {
                return NotFound();
            }

            var links = await _context.Links
                .Include(l => l.LinkTags.OrderBy(lt => lt.SortOrder))
                    .ThenInclude(lt => lt.LinkCollectionTagCategory)
                .Where(l => l.LinkCollectionId == collectionId)
                .OrderBy(l => l.SortOrder)
                .ToListAsync();

            var result = links.Select(l => new LinkDto
            {
                LinkId = l.LinkId,
                LinkCollectionId = l.LinkCollectionId,
                Url = l.Url,
                Title = l.Title,
                Description = l.Description,
                SortOrder = l.SortOrder,
                LinkTags = l.LinkTags.Select(lt => new LinkTagDto
                {
                    LinkTagId = lt.LinkTagId,
                    LinkId = lt.LinkId,
                    LinkCollectionTagCategoryId = lt.LinkCollectionTagCategoryId,
                    SortOrder = lt.SortOrder,
                    Value = lt.Value,
                    CategoryName = lt.LinkCollectionTagCategory.LinkCollectionTagCategoryName
                }).ToList()
            });

            return Ok(result);
        }

        [Permission(Allow = "SVMSecure.CMS.ManageContentBlocks")]
        [HttpPost("links")]
        public async Task<ActionResult<LinkDto>> PostLink(CreateLinkDto createDto)
        {
            var link = new Link
            {
                LinkCollectionId = createDto.LinkCollectionId,
                Url = createDto.Url,
                Title = createDto.Title,
                Description = createDto.Description,
                SortOrder = createDto.SortOrder
            };

            _context.Links.Add(link);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(link), new { id = link.LinkId }, new LinkDto
            {
                LinkId = link.LinkId,
                LinkCollectionId = link.LinkCollectionId,
                Url = link.Url,
                Title = link.Title,
                Description = link.Description,
                SortOrder = link.SortOrder,
                LinkTags = new List<LinkTagDto>()
            });
        }

        [Permission(Allow = "SVMSecure.CMS.ManageContentBlocks")]
        [HttpPut("links/{id}")]
        public async Task<IActionResult> PutLink(int id, CreateLinkDto updateDto)
        {
            var link = await _context.Links.FindAsync(id);
            if (link == null)
            {
                return NotFound();
            }

            link.LinkCollectionId = updateDto.LinkCollectionId;
            link.Url = updateDto.Url;
            link.Title = updateDto.Title;
            link.Description = updateDto.Description;
            link.SortOrder = updateDto.SortOrder;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [Permission(Allow = "SVMSecure.CMS.ManageContentBlocks")]
        [HttpDelete("links/{id}")]
        public async Task<IActionResult> DeleteLink(int id)
        {
            var link = await _context.Links.FindAsync(id);
            if (link == null)
            {
                return NotFound();
            }

            _context.Links.Remove(link);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        //Tags:
        [Permission(Allow = "SVMSecure.CMS.ManageContentBlocks")]
        [HttpPost("links/{linkId}/tags")]
        public async Task<ActionResult<LinkTagDto>> PostLinkTag(int linkId, CreateLinkTagDto createDto)
        {
            var link = await _context.Links.FindAsync(linkId);
            if (link == null)
            {
                return NotFound();
            }
            var tagCategory = await _context.LinkCollectionTagCategories.FindAsync(createDto.LinkCollectionTagCategoryId);
            if (tagCategory == null || tagCategory.LinkCollectionId != link.LinkCollectionId)
            {
                return BadRequest("Invalid tag category for this link's collection.");
            }
            var linkTag = new LinkTag
            {
                LinkId = linkId,
                LinkCollectionTagCategoryId = createDto.LinkCollectionTagCategoryId,
                SortOrder = createDto.SortOrder,
                Value = createDto.Value
            };
            _context.LinkTags.Add(linkTag);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(linkTag), new { id = linkTag.LinkTagId }, new LinkTagDto
            {
                LinkTagId = linkTag.LinkTagId,
                LinkId = linkTag.LinkId,
                LinkCollectionTagCategoryId = linkTag.LinkCollectionTagCategoryId,
                SortOrder = linkTag.SortOrder,
                Value = linkTag.Value,
                CategoryName = tagCategory.LinkCollectionTagCategoryName
            });
        }

        [Permission(Allow = "SVMSecure.CMS.ManageContentBlocks")]
        [HttpDelete("links/{linkId}/tags/{id}")]
        public async Task<IActionResult> DeleteLinkTag(int linkId, int id)
        {
            var linkTag = await _context.LinkTags.FindAsync(id);
            if (linkTag == null || linkTag.LinkId != linkId)
            {
                return NotFound();
            }
            _context.LinkTags.Remove(linkTag);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
