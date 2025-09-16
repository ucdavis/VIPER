using Areas.CMS.Models;
using Areas.CMS.Models.DTOs;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Polly;
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
        [HttpPost("{collectionId}/links")]
        public async Task<ActionResult<LinkDto>> PostLink(int collectionId, CreateLinkDto createDto)
        {
            if(collectionId != createDto.LinkCollectionId)
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

            foreach(var key in tagValues.Keys)
            {
                if(!tagCategories.Contains(key))
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
            foreach(var tcId in tagCategories)
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
