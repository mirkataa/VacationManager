using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VacationManager.Data;
using VacationManager.Models;

namespace VacationManager.Controllers
{
    [Authorize]
    public class RolesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RolesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Roles
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var totalRolesCount = await _context.Roles.CountAsync();
            var roles = await _context.Roles.Include(r => r.Users).ToListAsync();

            ViewBag.TotalCount = totalRolesCount;
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentPage = page;

            return View(roles);
        }

        // GET: Roles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var roleModel = await _context.Roles
                .Include(r => r.Users)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (roleModel == null)
            {
                return NotFound();
            }

            return View(roleModel);
        }

        // GET: Roles/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Roles/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] RoleModel roleModel)
        {
            if (ModelState.IsValid)
            {
                // Check if there's already an entry for the selected name
                var existingEntry = await _context.Roles
                    .FirstOrDefaultAsync(v => v.Name == roleModel.Name);

                if (existingEntry != null)
                {  
                    ModelState.AddModelError(string.Empty, "A role with this name already exists.");
                    return View(roleModel);
                }

                _context.Add(roleModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(roleModel);
        }

        // GET: Roles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var roleModel = await _context.Roles.FindAsync(id);
            if (roleModel == null)
            {
                return NotFound();
            }
            return View(roleModel);
        }

        // POST: Roles/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] RoleModel roleModel)
        {
            if (id != roleModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(roleModel);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoleModelExists(roleModel.Id))
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
            return View(roleModel);
        }

        // GET: Roles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var roleModel = await _context.Roles
                .FirstOrDefaultAsync(m => m.Id == id);
            if (roleModel == null)
            {
                return NotFound();
            }

            return View(roleModel);
        }

        // POST: Roles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var roleModel = await _context.Roles.FindAsync(id);
            if (roleModel == null)
            {
                return NotFound();
            }

            var unassignedRole = await _context.Roles.FirstOrDefaultAsync(r => r.Id == 4);
            if (unassignedRole == null)
            {
                return BadRequest("Unassigned role not found.");
            }

            var usersWithRole = await _context.Users.Where(u => u.RoleId == roleModel.Id).ToListAsync();

            foreach (var user in usersWithRole)
            {
                user.RoleId = unassignedRole.Id;
                var newRole = await _context.Roles.FindAsync(user.RoleId);
                if (newRole != null)
                {
                    newRole.Users.Add(user);
                    _context.Update(newRole);
                    await _context.SaveChangesAsync();
                }
            }

            _context.Roles.Remove(roleModel);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RoleModelExists(int id)
        {
            return _context.Roles.Any(e => e.Id == id);
        }
    }
}
