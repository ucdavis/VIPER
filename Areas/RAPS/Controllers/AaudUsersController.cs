// This is a auto-generate sample file.
// Shows how to make very basic CRUD operations against the database
// The routes listed do not work.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

namespace Viper.Areas.RAPS.Controllers
{
    [Area("RAPS")]
    public class AaudUsersController : Controller
    {
        private readonly AAUDContext _context;

        public AaudUsersController(AAUDContext context)
        {
            _context = context;
        }

        // GET: RAPS/AaudUsers
        public async Task<IActionResult> Index()
        {
              return _context.AaudUsers != null ? 
                          View(await _context.AaudUsers.ToListAsync()) :
                          Problem("Entity set 'AAUDContext.AaudUsers'  is null.");
        }

        // GET: RAPS/AaudUsers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.AaudUsers == null)
            {
                return NotFound();
            }

            var aaudUser = await _context.AaudUsers
                .FirstOrDefaultAsync(m => m.AaudUserId == id);
            if (aaudUser == null)
            {
                return NotFound();
            }

            return View(aaudUser);
        }

        // GET: RAPS/AaudUsers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: RAPS/AaudUsers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AaudUserId,ClientId,MothraId,LoginId,MailId,SpridenId,Pidm,EmployeeId,VmacsId,VmcasId,UnexId,MivId,LastName,FirstName,MiddleName,DisplayLastName,DisplayFirstName,DisplayMiddleName,DisplayFullName,CurrentStudent,FutureStudent,CurrentEmployee,FutureEmployee,StudentTerm,EmployeeTerm,PpsId,StudentPKey,EmployeePKey,Current,Future,IamId,Ross,Added")] AaudUser aaudUser)
        {
            if (ModelState.IsValid)
            {
                _context.Add(aaudUser);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(aaudUser);
        }

        // GET: RAPS/AaudUsers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.AaudUsers == null)
            {
                return NotFound();
            }

            var aaudUser = await _context.AaudUsers.FindAsync(id);
            if (aaudUser == null)
            {
                return NotFound();
            }
            return View(aaudUser);
        }

        // POST: RAPS/AaudUsers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AaudUserId,ClientId,MothraId,LoginId,MailId,SpridenId,Pidm,EmployeeId,VmacsId,VmcasId,UnexId,MivId,LastName,FirstName,MiddleName,DisplayLastName,DisplayFirstName,DisplayMiddleName,DisplayFullName,CurrentStudent,FutureStudent,CurrentEmployee,FutureEmployee,StudentTerm,EmployeeTerm,PpsId,StudentPKey,EmployeePKey,Current,Future,IamId,Ross,Added")] AaudUser aaudUser)
        {
            if (id != aaudUser.AaudUserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(aaudUser);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AaudUserExists(aaudUser.AaudUserId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(aaudUser);
        }

        // GET: RAPS/AaudUsers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.AaudUsers == null)
            {
                return NotFound();
            }

            var aaudUser = await _context.AaudUsers
                .FirstOrDefaultAsync(m => m.AaudUserId == id);
            if (aaudUser == null)
            {
                return NotFound();
            }

            return View(aaudUser);
        }

        // POST: RAPS/AaudUsers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.AaudUsers == null)
            {
                return Problem("Entity set 'AAUDContext.AaudUsers'  is null.");
            }
            var aaudUser = await _context.AaudUsers.FindAsync(id);
            if (aaudUser != null)
            {
                _context.AaudUsers.Remove(aaudUser);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AaudUserExists(int id)
        {
          return (_context.AaudUsers?.Any(e => e.AaudUserId == id)).GetValueOrDefault();
        }
    }
}
