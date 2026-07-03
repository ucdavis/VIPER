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
        public async Task<ActionResult<IEnumerable<LinkDto>>> GetLinks(int collectionId, string? groupByTagCategory = null)
        {
            if (await _context.LinkCollections.AsNoTracking().FirstOrDefaultAsync(lc => lc.LinkCollectionId == collectionId) == null)
            {
                return NotFound();
            }

            var links = await _context.Links
                .AsNoTracking()
                .Include(l => l.LinkTags.OrderBy(lt => lt.LinkCollectionTagCategory.SortOrder).ThenBy(lt => lt.SortOrder).ThenBy(lt => lt.Value))
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
                }).OrderBy(lt => lt.Value).ToList()
            }).ToList();

            if (!string.IsNullOrEmpty(groupByTagCategory))
            {
                var tagCategories = await _context.LinkCollectionTagCategories
                    .AsNoTracking()
                    .Where(tc => tc.LinkCollectionId == collectionId)
                    .Where(tc => tc.LinkCollectionTagCategoryName.ToLower() == groupByTagCategory.ToLower())
                    .FirstOrDefaultAsync();
                if (tagCategories == null)
                {
                    return BadRequest("Invalid tag category to group by.");
                }

                var categoryId = tagCategories.LinkCollectionTagCategoryId;

                // Group links by tag value, taking the value order from the already-ordered
                // in-memory result. LINQ GroupBy keeps first-occurrence key order, so the response
                // is deterministic (unlike a SELECT DISTINCT with no ORDER BY) and avoids an extra
                // round-trip. A link with several values lands in each group, so DistinctBy below
                // keeps it at its first position.
                var groupedLinks = result
                    .SelectMany(r => r.LinkTags
                        .Where(lt => lt.LinkCollectionTagCategoryId == categoryId && lt.Value != null)
                        .Select(lt => new { Value = lt.Value!, Link = r }))
                    .GroupBy(x => x.Value)
                    .SelectMany(g => g.Select(x => x.Link));

                // A link with a tag in this category but no non-null value counts as uncategorized,
                // so append those links to keep the value grouping from dropping them.
                var uncategorizedLinks = result.Where(r =>
                    r.LinkTags.Any(lt => lt.LinkCollectionTagCategoryId == categoryId && lt.Value == null)
                    && !r.LinkTags.Any(lt => lt.LinkCollectionTagCategoryId == categoryId && lt.Value != null));

                var orderedGroupedResult = groupedLinks
                    .Concat(uncategorizedLinks)
                    .DistinctBy(r => r.LinkId)
                    .ToList();

                return Ok(orderedGroupedResult);
            }

            return Ok(result);
        }

        [Permission(Allow = "SVMSecure.CMS.ManageContentBlocks")]
        [HttpPost("{collectionId}/links")]
        public async Task<ActionResult<LinkDto>> PostLink(int collectionId, CreateLinkDto createDto)
        {
            if (collectionId != createDto.LinkCollectionId)
            {
                return BadRequest();
            }

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

            return CreatedAtAction(nameof(GetLinks), new { collectionId }, new LinkDto
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
        [HttpPut("{collectionId}/links/{id}")]
        public async Task<IActionResult> PutLink(int id, int collectionId, CreateLinkDto updateDto)
        {
            var link = await _context.Links.FindAsync(id);
            if (link == null)
            {
                return NotFound();
            }

            if (collectionId != updateDto.LinkCollectionId || collectionId != link.LinkCollectionId)
            {
                return BadRequest();
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
        [HttpDelete("{collectionId}/links/{id}")]
        public async Task<IActionResult> DeleteLink(int collectionId, int id)
        {
            var link = await _context.Links.FindAsync(id);
            if (link == null)
            {
                return NotFound();
            }

            if (collectionId != link.LinkCollectionId)
            {
                return BadRequest();
            }

            _context.LinkTags.RemoveRange(_context.LinkTags.Where(lt => lt.LinkId == id));
            _context.Links.Remove(link);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [Permission(Allow = "SVMSecure.CMS.ManageContentBlocks")]
        [HttpPut("{collectionId}/links-order")]
        public async Task<IActionResult> UpdateLinkOrder(int collectionId, List<UpdateLinkOrderDto> updateDto)
        {
            var linkCollection = await _context.LinkCollections.FindAsync(collectionId);
            if (linkCollection == null)
            {
                return NotFound();
            }

            var links = await _context.Links
                .Where(l => l.LinkCollectionId == collectionId)
                .ToListAsync();

            // Membership validation, not just count: an unknown or duplicate LinkId slipped past the
            // count-only guard and crashed the First() lookup below with a 500.
            var linksById = links.ToDictionary(l => l.LinkId);
            if (links.Count != updateDto.Count
                || updateDto.Any(li => !linksById.ContainsKey(li.LinkId))
                || updateDto.Select(li => li.LinkId).Distinct().Count() != updateDto.Count)
            {
                return BadRequest("One or more LinkIds are invalid.");
            }

            foreach (var li in updateDto)
            {
                linksById[li.LinkId].SortOrder = li.SortOrder;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        //Tags:
        [Permission(Allow = "SVMSecure.CMS.ManageContentBlocks")]
        [HttpPut("{collectionId}/links/{linkId}/tags")]
        public async Task<ActionResult> SaveLinkTags(int linkId, int collectionId, Dictionary<int, string> tagValues)
        {
            var link = await _context.Links.FindAsync(linkId);
            if (link == null)
            {
                return NotFound();
            }

            if (collectionId != link.LinkCollectionId)
            {
                return BadRequest();
            }

            var tagCategories = await _context.LinkCollectionTagCategories
                .Where(tc => tc.LinkCollectionId == link.LinkCollectionId)
                .OrderBy(tc => tc.SortOrder)
                .Select(tc => tc.LinkCollectionTagCategoryId)
                .ToListAsync();

            if (tagValues.Keys.Any(key => !tagCategories.Contains(key)))
            {
                return BadRequest("Invalid tag category id");
            }

            //remove tags and recreate
            using var trans = await _context.Database.BeginTransactionAsync();
            _context.LinkTags.RemoveRange(_context.LinkTags.Where(lt => lt.LinkId == linkId));
            await _context.SaveChangesAsync();

            int i = 1;
            foreach (var (tcId, v) in tagCategories
                .Where(tcId => tagValues.ContainsKey(tcId))
                .SelectMany(tcId => tagValues[tcId].Split(",")
                    .Where(v => v != null)
                    .Select(v => (tcId, v))))
            {
                var linkTag = new LinkTag
                {
                    LinkId = linkId,
                    LinkCollectionTagCategoryId = tcId,
                    SortOrder = i++,
                    Value = v
                };
                _context.LinkTags.Add(linkTag);
            }
            await _context.SaveChangesAsync();
            await trans.CommitAsync();

            return NoContent();
        }
    }
}
