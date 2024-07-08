using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.CMS.Models;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.VIPER;
using Web.Authorization;

namespace Viper.Areas.CMS.Controllers
{
    //NOTE: no controller-wide permissions being checked because CMS content can be publically accessible
    [Route("/api/CMS/content")]
    public class CMSContentController : ApiController
    {
        private readonly VIPERContext _context;
        public IUserHelper UserHelper;

        public CMSContentController(VIPERContext context)
        {
            _context = context;
            UserHelper = new UserHelper();
        }

        //GET: content
        [HttpGet]
        [Permission(Allow = "SVMSecure.CMS.ManageContentBlocks")]
        public ActionResult<List<ContentBlock>> GetContentBlocks()
        {
            if (_context.ContentBlocks == null)
            {
                return NotFound();
            }
            return new Data.CMS().GetContentBlocks()?.ToList() ?? new List<ContentBlock>();
        }

        //GET: content/fn/{friendlyName}
        [HttpGet("fn/{friendlyName}")]
        public ActionResult<ContentBlock?> GetContentBlockByFn(string friendlyName)
        {
            var blocks = new Data.CMS().GetContentBlocksAllowed(null, friendlyName, null, null, null, null, null, null);
            if(blocks == null || !blocks.Any())
            {
                return NotFound();
            }

            return blocks.First();
        }

        //PUT: content/5
        [HttpPut("{contentBlockId}")]
        [Permission(Allow = "SVMSecure.CMS.ManageContentBlocks")]
        public async Task<ActionResult<ContentBlock>> UpdateContentBlock(int contentBlockId, CMSBlockAddEdit block)
        {
            //check data is valid and block is found
            var existingBlock = _context.ContentBlocks.Find(contentBlockId);
            if (existingBlock == null)
            {
                return NotFound();
            }

            if (contentBlockId != block.ContentBlockId)
            {
                return BadRequest();
            }

            string inputCheck = CheckBlockForRequiredFields(block);
            if(!string.IsNullOrEmpty(inputCheck))
            {
                return BadRequest(inputCheck);
            }
            
            var friendlyNameCheck = new Data.CMS().GetContentBlocks(friendlyName: block.FriendlyName)?.FirstOrDefault();
            if (friendlyNameCheck != null && friendlyNameCheck.ContentBlockId != contentBlockId)
            {
                return ValidationProblem("Friendly name must be unique");
            }
            else if (friendlyNameCheck != null)
            {
                _context.Entry(friendlyNameCheck).State = EntityState.Detached;
            }

            //modify database object
            ModifyBlockWithUserInput(existingBlock, block);
            _context.Entry(existingBlock).State = EntityState.Modified;

            //save history
            var contentHistory = new ContentHistory()
            {
                ContentBlockId = contentBlockId,
                ContentBlockContent = block.Content,
                ModifiedOn = DateTime.Now,
                ModifiedBy = UserHelper.GetCurrentUser()?.LoginId
            };
            _context.ContentHistories.Add(contentHistory);

            //save and return the saved block
            await _context.SaveChangesAsync();
            var returnBlock = new Data.CMS().GetContentBlocks(contentBlockId: contentBlockId)?.FirstOrDefault();
            if(returnBlock == null)
            {
                return NotFound();
            }
            return returnBlock;
        }

        //POST: content
        [HttpPost]
        [Permission(Allow = "SVMSecure.CMS.ManageContentBlocks")]
        public async Task<ActionResult<ContentBlock>> CreateContentBlock(CMSBlockAddEdit block)
        {
            string inputCheck = CheckBlockForRequiredFields(block);
            if (!string.IsNullOrEmpty(inputCheck))
            {
                return BadRequest(inputCheck);
            }
            var friendlyNameCheck = new Data.CMS().GetContentBlocks(friendlyName: block.FriendlyName)?.FirstOrDefault();
            if (friendlyNameCheck != null)
            {
                return ValidationProblem("Friendly name must be unique");
            }

            var newBlock = new ContentBlock();
            ModifyBlockWithUserInput(newBlock, block);

            _context.ContentBlocks.Add(newBlock);
            await _context.SaveChangesAsync();

            /*
            foreach (var p in permissions)
            {
                block.ContentBlockToPermissions.Add(new ContentBlockToPermission
                {
                    Permission = p,
                    ContentBlockId = block.ContentBlockId,
                });
            }
            _context.Entry(block).State = EntityState.Modified;
            */

            var contentHistory = new ContentHistory()
            {
                ContentBlockId = block.ContentBlockId,
                ContentBlockContent = block.Content,
                ModifiedOn = DateTime.Now,
                ModifiedBy = UserHelper.GetCurrentUser()?.LoginId
            };
            _context.ContentHistories.Add(contentHistory);
            await _context.SaveChangesAsync();
            
            return newBlock;
        }

        //DELETE: content/5
        [HttpDelete("{contentBlockId}")]
        [Permission(Allow = "SVMSecure.CMS.ManageContentBlocks")]
        public async Task<ActionResult<ContentBlock>> DeleteContentBlock(int contentBlockId)
        {
            var block = new Data.CMS().GetContentBlocks(contentBlockId: contentBlockId)?.FirstOrDefault();
            if (block == null)
            {
                return NotFound();
            }

            block.DeletedOn = DateTime.Now;
            block.ModifiedBy = UserHelper.GetCurrentUser()?.LoginId;
            _context.Entry(block).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return block;
        }

        private string CheckBlockForRequiredFields(CMSBlockAddEdit userInput)
        {
            string errors = "";
            if(string.IsNullOrEmpty(userInput.Title))
            {
                errors += "Title is required. ";
            }
            if (string.IsNullOrEmpty(userInput.System))
            {
                errors += "System is required. ";
            }
            return errors;
        }

        private void ModifyBlockWithUserInput(ContentBlock contentBlock, CMSBlockAddEdit userInput)
        {
            //update info
            contentBlock.Title = userInput.Title;
            contentBlock.Content = userInput.Content;
            contentBlock.FriendlyName = userInput.FriendlyName;
            contentBlock.System = userInput.System;
            contentBlock.Application = userInput.Application;
            contentBlock.Page = userInput.Page;
            contentBlock.ViperSectionPath = userInput.ViperSectionPath;
            contentBlock.AllowPublicAccess = userInput.AllowPublicAccess;
            contentBlock.BlockOrder = userInput.BlockOrder;
            contentBlock.ModifiedOn = DateTime.Now;
            contentBlock.ModifiedBy = UserHelper.GetCurrentUser()?.LoginId;

            //adjust permissions
            //remove content block permisisons that are not in the user input
            foreach (var cbp in contentBlock.ContentBlockToPermissions.Where(cbp => !userInput.Permissions.Contains(cbp.Permission)))
            {
                contentBlock.ContentBlockToPermissions.Remove(cbp);
            }

            //add new content block permissions, if they are not in the existing list
            var existingPermissions = contentBlock.ContentBlockToPermissions.Select(p => p.Permission).ToList();
            foreach (var p in userInput.Permissions.Where(p => !existingPermissions.Contains(p)))
            {
                contentBlock.ContentBlockToPermissions.Add(new ContentBlockToPermission
                {
                    Permission = p,
                    ContentBlockId = userInput.ContentBlockId
                });
            }
        }
    }
}
