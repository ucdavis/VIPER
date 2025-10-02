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
            if (await _context.LinkCollections.FirstOrDefaultAsync(lc => lc.LinkCollectionId == collectionId) == null)
            {
                return NotFound();
            }

            var links = await _context.Links
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
            });

            if (!string.IsNullOrEmpty(groupByTagCategory))
            {
                var tagCategories = await _context.LinkCollectionTagCategories
                    .Where(tc => tc.LinkCollectionId == collectionId)
                    .Where(tc => tc.LinkCollectionTagCategoryName.ToLower() == groupByTagCategory.ToLower())
                    .FirstOrDefaultAsync();
                if (tagCategories == null)
                {
                    return BadRequest("Invalid tag category to group by.");
                }

                var allValues = await _context.LinkTags
                    .Where(lt => lt.LinkCollectionTagCategoryId == tagCategories.LinkCollectionTagCategoryId)
                    .Select(lt => lt.Value)
                    .Distinct()
                    .ToListAsync();

                var orderedGroupedResult = new List<LinkDto>();
                var added = new HashSet<int>();
                foreach (var v in allValues)
                {
                    foreach (var r in result)
                    {
                        if (r.LinkTags.Any(lt => lt.Value == v && lt.LinkCollectionTagCategoryId == tagCategories.LinkCollectionTagCategoryId) && !added.Contains(r.LinkId))
                        {
                            added.Add(r.LinkId);
                            orderedGroupedResult.Add(r);
                        }
                    }
                }

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

            if (links.Count != updateDto.Count)
            {
                return BadRequest("One or more LinkIds are invalid.");
            }

            foreach (var li in updateDto)
            {
                var link = links.First(l => l.LinkId == li.LinkId);
                link.SortOrder = li.SortOrder;
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

            foreach (var key in tagValues.Keys)
            {
                if (!tagCategories.Contains(key))
                {
                    return BadRequest("Invalid tag category id");
                }
            }

            //remove tags and recreate
            using var trans = _context.Database.BeginTransaction();
            _context.LinkTags.RemoveRange(_context.LinkTags.Where(lt => lt.LinkId == linkId));
            await _context.SaveChangesAsync();

            int i = 1;
            List<LinkTag> tagsAdded = new List<LinkTag>();
            foreach (var tcId in tagCategories)
            {
                if (tagValues.ContainsKey(tcId))
                {
                    foreach (var v in tagValues[tcId].Split(","))
                    {
                        if (v != null)
                        {
                            var linkTag = new LinkTag
                            {
                                LinkId = linkId,
                                LinkCollectionTagCategoryId = tcId,
                                SortOrder = i++,
                                Value = v
                            };
                            _context.LinkTags.Add(linkTag);
                            tagsAdded.Add(linkTag);
                        }
                    }
                }
            }
            await _context.SaveChangesAsync();
            await trans.CommitAsync();

            return NoContent();
        }
    }
}
