using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VacationManager.Data;
using VacationManager.Models;

namespace VacationManager.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public UsersController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Users
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var totalUsersCount = await _context.Users.CountAsync();
            var users = await _context.Users.ToListAsync();

            var teams = await _context.Teams.ToListAsync();
            var roles = await _context.Roles.ToListAsync();

            ViewBag.Teams = teams;
            ViewBag.Roles = roles;
            ViewBag.TotalCount = totalUsersCount;
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentPage = page;

            return View(users);
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userModel = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (userModel == null)
            {
                return NotFound();
            }

            // Fetch role name
            var roleName = await _context.Roles
                .Where(r => r.Id == userModel.RoleId)
                .Select(r => r.Name)
                .FirstOrDefaultAsync();

            // Fetch team name
            var teamName = await _context.Teams
                .Where(t => t.Id == userModel.TeamId)
                .Select(t => t.Name)
                .FirstOrDefaultAsync();

            // Add role and team names to ViewData
            ViewData["RoleName"] = roleName;
            ViewData["TeamName"] = teamName;

            return View(userModel);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Username,Password,FirstName,LastName,RoleId,TeamId,IsAway,IsHalfDay,IsSickLeave")] UserModel userModel)
        {
            if (ModelState.IsValid)
            {
                _context.Add(userModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(userModel);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userModel = await _context.Users.FindAsync(id);
            if (userModel == null)
            {
                return NotFound();
            }
            return View(userModel);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Username,Password,FirstName,LastName,RoleId,TeamId,IsAway,IsHalfDay,IsSickLeave")] UserModel userModel)
        {
            if (id != userModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(userModel);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserModelExists(userModel.Id))
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
            return View(userModel);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userModel = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);

            if (userModel == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && userModel.Username == currentUser.UserName)
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Index));
            }


            // Fetch role name
            var roleName = await _context.Roles
                .Where(r => r.Id == userModel.RoleId)
                .Select(r => r.Name)
                .FirstOrDefaultAsync();

            // Fetch team name
            var teamName = await _context.Teams
                .Where(t => t.Id == userModel.TeamId)
                .Select(t => t.Name)
                .FirstOrDefaultAsync();

            // Add role and team names to ViewData
            ViewData["RoleName"] = roleName;
            ViewData["TeamName"] = teamName;

            return View(userModel);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userModel = await _context.Users.FindAsync(id);
            if (userModel != null)
            {
                // Remove the user from the team and role if necessary
                if (userModel.RoleId == 4 || userModel.RoleId == 2 || userModel.RoleId == 1)
                {
                    var team = await _context.Teams.FirstOrDefaultAsync(t => t.Id == userModel.TeamId);
                    if (team != null)
                    {
                        // Remove user from the list of developers
                        team.Developers.Remove(userModel);
                    }

                    // Remove user from the role
                    var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == userModel.RoleId);
                    if (role != null)
                    {
                        role.Users.Remove(userModel);
                    }
                }
                else if (userModel.RoleId == 3)
                {
                    // Set TeamLeaderId to null if user is a team leader
                    var team = await _context.Teams.FirstOrDefaultAsync(t => t.Id == userModel.TeamId);
                    if (team != null && team.TeamLeaderId == id)
                    {
                        team.TeamLeaderId = null;
                    }

                    // Remove user from the role
                    var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == userModel.RoleId);
                    if (role != null)
                    {
                        role.Users.Remove(userModel);
                    }
                }

                var user = await _userManager.FindByNameAsync(userModel.Username);
                if (user != null)
                {
                    var result = await _userManager.DeleteAsync(user);
                    if (!result.Succeeded)
                    {
                        return NotFound();
                    }
                }

                _context.Users.Remove(userModel);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserModelExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}
