using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VacationManager.Data;
using VacationManager.Models;

namespace VacationManager.Controllers
{
    public class TeamsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TeamsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Teams
        public async Task<IActionResult> Index()
        {
            ViewBag.Context = _context;
            return View(await _context.Teams.ToListAsync());
        }

        // GET: Teams/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teamModel = await _context.Teams
                .Include(t => t.Developers) // Include the related users
                .FirstOrDefaultAsync(m => m.Id == id);

            if (teamModel == null)
            {
                return NotFound();
            }

            // Fetch the project name
            var projectName = string.Empty;
            if (teamModel.ProjectId != null)
            {
                var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == teamModel.ProjectId);
                projectName = project != null ? project.Name : "No Project";
            }
            else
            {
                projectName = "No Project";
            }
            ViewData["ProjectName"] = projectName;

            // Fetch the team leader
            var teamLeader = await _context.Users.FirstOrDefaultAsync(u => u.Id == teamModel.TeamLeaderId);
            if (teamLeader != null)
            {
                ViewData["TeamLeaderName"] = $"{teamLeader.FirstName} {teamLeader.LastName}";
            }

            // Get all users with Role Unassigned
            var unassignedUsers = await _context.Users
                .Where(u => u.RoleId == 4)
                .ToListAsync();

            // Pass the list of unassigned teams to the view
            ViewBag.UnassignedUsers = unassignedUsers;

            return View(teamModel);
        }

        // GET: Teams/Create
        public IActionResult Create()
        {
            // Fetch the list of projects and team leaders from the database
            var projects = _context.Projects.ToList();
            // Fetch the list of users with Unassigned role
            var teamLeaders = _context.Users.Where(u => u.RoleId == 4).ToList();

            // Map the project and team leader names to SelectListItem for dropdowns
            var projectItems = projects.Select(p => new SelectListItem
            {
                Text = p.Name,
                Value = p.Id.ToString()
            });

            var teamLeaderItems = teamLeaders.Select(t => new SelectListItem
            {
                Text = t.FirstName + " " + t.LastName,
                Value = t.Id.ToString()
            });

            // Add an option for "No Project" with a null value
            projectItems = new List<SelectListItem>
            {
                new SelectListItem { Text = "No Project", Value = "" }
            }.Concat(projectItems);

            // Add an option for "No Team Leader" with a null value
            teamLeaderItems = new List<SelectListItem>
            {
                new SelectListItem { Text = "No Team Leader", Value = "" }
            }.Concat(teamLeaderItems);

            // Pass the SelectListItem collections to the ViewBag
            ViewBag.Projects = projectItems;
            ViewBag.TeamLeaders = teamLeaderItems;
            return View();
        }

        // POST: Teams/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,ProjectId,TeamLeaderId")] TeamModel teamModel)
        {
            if (ModelState.IsValid)
            {
                // Add the team to the context
                _context.Add(teamModel);
                await _context.SaveChangesAsync();

                if (teamModel.TeamLeaderId != null)
                {

                    // Change the RoleId of the team leader to 3 (TeamLead)
                    var teamLeader = await _context.Users.FindAsync(teamModel.TeamLeaderId);
                    if (teamLeader != null)
                    {
                        var exRole = await _context.Roles.FindAsync(teamLeader.RoleId);
                        if (exRole != null)
                        {
                            exRole.Users.Remove(teamLeader);
                            _context.Update(exRole);
                            await _context.SaveChangesAsync();
                        }
                        // Remove the user from the "Not in a team" team
                        var notInTeam = await _context.Teams.FindAsync(1);
                        if (notInTeam != null)
                        {
                            notInTeam.Developers.Remove(teamLeader);
                            _context.Update(notInTeam);
                            await _context.SaveChangesAsync();
                        }

                        teamLeader.RoleId = 3; // Assuming RoleId 3 represents TeamLead
                        var newRole = await _context.Roles.FindAsync(teamLeader.RoleId);
                        if (newRole != null)
                        {
                            newRole.Users.Add(teamLeader);
                            _context.Update(newRole);
                            await _context.SaveChangesAsync();
                        }
                        teamLeader.TeamId = teamModel.Id;
                        _context.Update(teamLeader);
                        await _context.SaveChangesAsync();
                    }
                }
                // Add the team to the list of teams associated with the project
                if (teamModel.ProjectId != null)
                {
                    var project = await _context.Projects.FindAsync(teamModel.ProjectId);
                    if (project != null)
                    {
                        project.Teams.Add(teamModel);
                        _context.Update(project);
                        await _context.SaveChangesAsync();
                    }
                }

                return RedirectToAction(nameof(Index));
            }
            return View(teamModel);
        }

        // GET: Teams/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teamModel = await _context.Teams.FindAsync(id);
            if (teamModel == null)
            {
                return NotFound();
            }

            // Populate ViewBag with projects
            //ViewBag.Projects = new SelectList(_context.Projects, "Id", "Name", teamModel.ProjectId);

            // Populate ViewBag with projects
            var projects = await _context.Projects.ToListAsync();
            var projectItems = projects.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.Name,
            }).ToList();

            // Set the SelectList for the ViewBag
            ViewBag.Projects = new SelectList(projectItems, "Value", "Text", teamModel.ProjectId);


            // Populate ViewBag with users with Role "Unassigned"
            var teamLeaders = await _context.Users.Where(u => u.RoleId == 4).ToListAsync();

            // Create a list of SelectListItems for the team leaders
            var teamLeaderItems = teamLeaders.Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = u.FirstName + " " + u.LastName,
            }).ToList();

            // If TeamLeaderId is null, add an option for "No Team Leader"
            if (teamModel.TeamLeaderId == null)
            {
                teamLeaderItems.Insert(0, new SelectListItem { Value = null, Text = "No Team Leader" });
            }

            // Add the current team leader to the SelectList
            if (teamModel.TeamLeaderId != null)
            {
                var currentTeamLeader = await _context.Users.FindAsync(teamModel.TeamLeaderId);
                if (currentTeamLeader != null)
                {
                    teamLeaderItems.Insert(0, new SelectListItem { Value = currentTeamLeader.Id.ToString(), Text = currentTeamLeader.FirstName + " " + currentTeamLeader.LastName });
                }
            }

            // Set the SelectList for the ViewBag
            ViewBag.TeamLeaders = new SelectList(teamLeaderItems, "Value", "Text", teamModel.TeamLeaderId);

            return View(teamModel);
        }

        // POST: Teams/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,ProjectId,TeamLeaderId")] TeamModel teamModel)
        {
            if (id != teamModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the current team
                    var currentTeam = await _context.Teams.FindAsync(id);
                    if (currentTeam == null)
                    {
                        return NotFound();
                    }

                    // If "No Project" is selected, set ProjectId to null
                    if (teamModel.ProjectId == null)
                    {
                        // Remove the team from its current project
                        var currentProject = await _context.Projects.FindAsync(currentTeam.ProjectId);
                        if (currentProject != null)
                        {
                            currentProject.Teams.Remove(currentTeam);
                            _context.Update(currentProject);
                        }
                        currentTeam.ProjectId = null;
                        _context.Update(currentTeam);
                    }
                    else
                    {
                        // Check if the team is being moved to a different project
                        if (currentTeam.ProjectId != teamModel.ProjectId)
                        {
                            // Remove the team from its current project
                            var currentProject = await _context.Projects.FindAsync(currentTeam.ProjectId);
                            if (currentProject != null)
                            {
                                currentProject.Teams.Remove(currentTeam);
                                _context.Update(currentProject);
                            }

                            // Add the team to the new project
                            var newProject = await _context.Projects.FindAsync(teamModel.ProjectId);
                            if (newProject != null)
                            {
                                newProject.Teams.Add(currentTeam);
                                _context.Update(newProject);
                            }
                        }
                        // Update the team's project
                        currentTeam.ProjectId = teamModel.ProjectId;
                    }

                    // Save changes to the database
                    await _context.SaveChangesAsync();

                    // Check if "No Team Leader" option is selected
                    if (teamModel.TeamLeaderId == null)
                    {
                        // Update the role of the current team leader to "Unassigned" (RoleId = 4)
                        var currentTeamLeader = await _context.Users.FindAsync(currentTeam.TeamLeaderId);
                        if (currentTeamLeader != null && currentTeamLeader.RoleId == 3)
                        {
                            var exRole = await _context.Roles.FindAsync(currentTeamLeader.RoleId);
                            if (exRole != null)
                            {
                                exRole.Users.Remove(currentTeamLeader);
                                _context.Update(exRole);
                                await _context.SaveChangesAsync();
                            }
                            var notInTeam = await _context.Teams.FindAsync(1);
                            if (notInTeam != null)
                            {
                                notInTeam.Developers.Add(currentTeamLeader);
                                _context.Update(notInTeam);
                                await _context.SaveChangesAsync();
                            }
                            currentTeamLeader.TeamId = 1;
                            currentTeamLeader.RoleId = 4;
                            var newRole = await _context.Roles.FindAsync(currentTeamLeader.RoleId);
                            if (newRole != null)
                            {
                                newRole.Users.Add(currentTeamLeader);
                                _context.Update(newRole);
                                await _context.SaveChangesAsync();
                            }
                            _context.Update(currentTeamLeader);
                        }
                        // Set TeamLeaderId to null
                        currentTeam.TeamLeaderId = null;
                    }
                    else
                    {
                        var newTeamLeader = await _context.Users.FindAsync(teamModel.TeamLeaderId);

                        if (newTeamLeader != null)
                        {
                            // If the new team leader is a developer (RoleId = 2), remove them from their current team
                            /* if (newTeamLeader.RoleId == 2)
                             {
                                 var currentTeamOfNewLeader = await _context.Teams.FirstOrDefaultAsync(t => t.Developers.Any(d => d.Id == newTeamLeader.Id));
                                 currentTeamOfNewLeader.Developers.Remove(newTeamLeader);
                                 _context.Update(currentTeamOfNewLeader);
                                 newTeamLeader.TeamId = teamModel.Id;
                                 newTeamLeader.RoleId = 3;
                                 _context.Update(newTeamLeader);

                             }*/

                            // If the new team leader's role is "Unassigned" (RoleId = 4), change it to Team Leader (RoleId = 3)
                            if (newTeamLeader.RoleId == 4)
                            {
                                var exRole = await _context.Roles.FindAsync(newTeamLeader.RoleId);
                                if (exRole != null)
                                {
                                    exRole.Users.Remove(newTeamLeader);
                                    _context.Update(exRole);
                                    await _context.SaveChangesAsync();
                                }
                                var notInTeam = await _context.Teams.FindAsync(1);
                                if (notInTeam != null)
                                {
                                    notInTeam.Developers.Remove(newTeamLeader);
                                    _context.Update(notInTeam);
                                    await _context.SaveChangesAsync();
                                }
                                newTeamLeader.TeamId = teamModel.Id;
                                newTeamLeader.RoleId = 3;
                                var newRole = await _context.Roles.FindAsync(newTeamLeader.RoleId);
                                if (newRole != null)
                                {
                                    newRole.Users.Add(newTeamLeader);
                                    _context.Update(newRole);
                                    await _context.SaveChangesAsync();
                                }
                                _context.Update(newTeamLeader);
                            }

                            // Update the role of the current team leader to "Unassigned" (RoleId = 4)
                            var currentTeamLeader = await _context.Users.FindAsync(currentTeam.TeamLeaderId);
                            if (currentTeamLeader != null && currentTeamLeader.RoleId == 3 && currentTeamLeader.Id != newTeamLeader.Id)
                            {
                                var exRole = await _context.Roles.FindAsync(currentTeamLeader.RoleId);
                                if (exRole != null)
                                {
                                    exRole.Users.Remove(currentTeamLeader);
                                    _context.Update(exRole);
                                    await _context.SaveChangesAsync();
                                }
                                var notInTeam = await _context.Teams.FindAsync(1);
                                if (notInTeam != null)
                                {
                                    notInTeam.Developers.Add(currentTeamLeader);
                                    _context.Update(notInTeam);
                                    await _context.SaveChangesAsync();
                                }
                                currentTeamLeader.TeamId = 1;
                                currentTeamLeader.RoleId = 4;
                                var newRole = await _context.Roles.FindAsync(currentTeamLeader.RoleId);
                                if (newRole != null)
                                {
                                    newRole.Users.Add(currentTeamLeader);
                                    _context.Update(newRole);
                                    await _context.SaveChangesAsync();
                                }
                                _context.Update(currentTeamLeader);
                            }
                        }

                        // Set the new team leader's ID for the current team
                        currentTeam.TeamLeaderId = teamModel.TeamLeaderId;
                    }

                    currentTeam.Name = teamModel.Name;
                    _context.Update(currentTeam);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TeamModelExists(teamModel.Id))
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
            return View(teamModel);
        }

        // GET: Teams/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teamModel = await _context.Teams
                .FirstOrDefaultAsync(m => m.Id == id);
            if (teamModel == null)
            {
                return NotFound();
            }

            // Fetch the team leader's name
            var teamLeaderName = string.Empty;
            if (teamModel.TeamLeaderId != null)
            {
                var teamLeader = await _context.Users.FirstOrDefaultAsync(u => u.Id == teamModel.TeamLeaderId);
                if (teamLeader != null)
                {
                    teamLeaderName = $"{teamLeader.FirstName} {teamLeader.LastName}";
                }
            }
            ViewData["TeamLeaderName"] = teamLeaderName;

            // Fetch the project name based on ProjectId
            var projectName = string.Empty;
            if (teamModel.ProjectId != null)
            {
                var project = await _context.Projects.FindAsync(teamModel.ProjectId);
                if (project != null)
                {
                    projectName = project.Name;
                }
            }
            ViewData["ProjectName"] = projectName;

            return View(teamModel);
        }

        // POST: Teams/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var teamModel = await _context.Teams.FindAsync(id);
            if (teamModel == null)
            {
                return NotFound();
            }

            // Change the RoleId of the team leader to 4 (Unassigned)
            if (teamModel.TeamLeaderId != null)
            {
                var teamLeader = await _context.Users.FindAsync(teamModel.TeamLeaderId);
                if (teamLeader != null)
                {
                    var exRole = await _context.Roles.FindAsync(teamLeader.RoleId);
                    if (exRole != null)
                    {
                        exRole.Users.Remove(teamLeader);
                        _context.Update(exRole);
                        await _context.SaveChangesAsync();
                    }
                    teamLeader.RoleId = 4; // Unassigned
                    teamLeader.TeamId = 1; // Move team leader to "No Team"
                    var newRole = await _context.Roles.FindAsync(teamLeader.RoleId);
                    if (newRole != null)
                    {
                        newRole.Users.Add(teamLeader);
                        _context.Update(newRole);
                        await _context.SaveChangesAsync();
                    }
                    var notInTeam = await _context.Teams.FindAsync(1);
                    if (notInTeam != null)
                    {
                        notInTeam.Developers.Add(teamLeader);
                        _context.Update(notInTeam);
                        await _context.SaveChangesAsync();
                    }
                    _context.Update(teamLeader);
                }
            }

            // Change the RoleId of all developers in the team to 4 (Unassigned)
            foreach (var developer in teamModel.Developers)
            {
                var exRole = await _context.Roles.FindAsync(developer.RoleId);
                if (exRole != null)
                {
                    exRole.Users.Remove(developer);
                    _context.Update(exRole);
                    await _context.SaveChangesAsync();
                }
                teamModel.Developers.Remove(developer);
                developer.RoleId = 4; // Unassigned
                developer.TeamId = 1; // Move developer to "No Team"
                var newRole = await _context.Roles.FindAsync(developer.RoleId);
                if (newRole != null)
                {
                    newRole.Users.Add(developer);
                    _context.Update(newRole);
                    await _context.SaveChangesAsync();
                }

                var notInTeam = await _context.Teams.FindAsync(1);
                if (notInTeam != null)
                {
                    notInTeam.Developers.Add(developer);
                    _context.Update(notInTeam);
                    await _context.SaveChangesAsync();
                }
                _context.Update(developer);
            }

            // Remove the team from the list of teams associated with the project
            if (teamModel.ProjectId != null)
            {
                var project = await _context.Projects.FindAsync(teamModel.ProjectId);
                if (project != null)
                {
                    project.Teams.Remove(teamModel);
                    _context.Update(project);
                }
            }

            _context.Teams.Remove(teamModel);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Add/Delete developer 

        // POST: Teams/AddDeveloper
        [HttpPost]
        public async Task<IActionResult> AddDeveloper(int teamId, int userId)
        {
            var team = await _context.Teams.Include(t => t.Developers).FirstOrDefaultAsync(t => t.Id == teamId);
            var developer = await _context.Users.FindAsync(userId);

            if (team != null && developer != null)
            {
                team.Developers.Add(developer);
                developer.TeamId = teamId;
                var exRole = await _context.Roles.FindAsync(developer.RoleId);
                if (exRole != null)
                {
                    exRole.Users.Remove(developer);
                    _context.Update(exRole);
                    await _context.SaveChangesAsync();
                }
                developer.RoleId = 2; //From Unassigned to Developer
                var newRole = await _context.Roles.FindAsync(developer.RoleId);
                if (newRole != null)
                {
                    newRole.Users.Add(developer);
                    _context.Update(newRole);
                    await _context.SaveChangesAsync();
                }
                var notInTeam = await _context.Teams.FindAsync(1);
                if (notInTeam != null)
                {
                    notInTeam.Developers.Remove(developer);
                    _context.Update(notInTeam);
                    await _context.SaveChangesAsync();
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = teamId });
        }

        // POST: Teams/RemoveDeveloper
        [HttpPost]
        public async Task<IActionResult> RemoveDeveloper(int teamId, int userId)
        {
            var team = await _context.Teams.Include(t => t.Developers).FirstOrDefaultAsync(t => t.Id == teamId);
            var developer = team?.Developers.FirstOrDefault(d => d.Id == userId);

            if (team != null && developer != null)
            {
                team.Developers.Remove(developer);
                developer.TeamId = 1; //Add to "Not in a team" team
                var exRole = await _context.Roles.FindAsync(developer.RoleId);
                if (exRole != null)
                {
                    exRole.Users.Remove(developer);
                    _context.Update(exRole);
                    await _context.SaveChangesAsync();
                }
                developer.RoleId = 4; //Role to Unassigned
                var newRole = await _context.Roles.FindAsync(developer.RoleId);
                if (newRole != null)
                {
                    newRole.Users.Add(developer);
                    _context.Update(newRole);
                    await _context.SaveChangesAsync();
                }
                var notInTeam = await _context.Teams.FindAsync(1);
                if (notInTeam != null)
                {
                    notInTeam.Developers.Add(developer);
                    _context.Update(notInTeam);
                    await _context.SaveChangesAsync();
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = teamId });
        }

        private bool TeamModelExists(int id)
        {
            return _context.Teams.Any(e => e.Id == id);
        }
    }
}

