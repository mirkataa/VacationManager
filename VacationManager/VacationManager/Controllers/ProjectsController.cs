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
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProjectsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Projects
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var totalProjectsCount = await _context.Projects.CountAsync();

            ViewBag.TotalCount = totalProjectsCount;
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentPage = page;
            return View(await _context.Projects.ToListAsync());
        }

        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .Include(p => p.Teams)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (project == null)
            {
                return NotFound();
            }

            // Get all teams that are not associated with any project & not Not in a team
            var teamsNotAssigned = await _context.Teams
                .Where(t => t.ProjectId == null && t.Id != 1)
                .ToListAsync();

            // Pass the list of unassigned teams to the view
            ViewBag.TeamsNotAssigned = teamsNotAssigned;

            return View(project);
        }

        // GET: Projects/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Projects/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description")] Project project)
        {
            if (ModelState.IsValid)
            {
                // Check if there's already an entry for the selected name
                var existingEntry = await _context.Projects
                    .FirstOrDefaultAsync(v => v.Name == project.Name);

                if (existingEntry != null)
                {
                    ModelState.AddModelError(string.Empty, "A project with this name already exists.");
                    return View(project);
                }

                _context.Add(project);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(project);
        }

        // GET: Projects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }
            return View(project);
        }

        // POST: Projects/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] Project project)
        {
            if (id != project.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(project);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExists(project.Id))
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
            return View(project);
        }

        // GET: Projects/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .FirstOrDefaultAsync(m => m.Id == id);
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            // Find teams associated with the project
            var teams = await _context.Teams.Where(t => t.ProjectId == id).ToListAsync();

            // Update the ProjectId of associated teams to null
            foreach (var team in teams)
            {
                team.ProjectId = null;
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Add/Delete team 

        // POST: Projects/AddTeam
        [HttpPost]
        public async Task<IActionResult> AddTeam(int projectId, int teamId)
        {
            var project = await _context.Projects.Include(p => p.Teams).FirstOrDefaultAsync(p => p.Id == projectId);
            var team = await _context.Teams.FindAsync(teamId);

            if (project != null && team != null)
            {
                project.Teams.Add(team);
                team.ProjectId = projectId;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = projectId });
        }

        // POST: Projects/RemoveTeam
        [HttpPost]
        public async Task<IActionResult> RemoveTeam(int projectId, int teamId)
        {
            var project = await _context.Projects.Include(p => p.Teams).FirstOrDefaultAsync(p => p.Id == projectId);
            var team = project?.Teams.FirstOrDefault(t => t.Id == teamId);

            if (project != null && team != null)
            {
                project.Teams.Remove(team);
                team.ProjectId = null;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = projectId });
        }

        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.Id == id);
        }
    }
}
