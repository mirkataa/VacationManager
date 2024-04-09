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
    public class LeaveRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LeaveRequestsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: LeaveRequests
        public async Task<IActionResult> Index()
        {
            return View(await _context.LeaveRequests.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> ApproveRequest(int id)
        {
            var leaveRequest = await _context.LeaveRequests.FirstOrDefaultAsync(lr => lr.Id == id);

            if (leaveRequest != null)
            {
                leaveRequest.IsApproved = true;
                leaveRequest.IsCompleted = true;
                await _context.SaveChangesAsync();
                if (leaveRequest.IsPaid)
                {
                    // Calculate workdays between start and end date
                    var workdays = CalculateWeekdays(leaveRequest.StartDate, leaveRequest.EndDate);

                    // Fetch the user associated with the leave request
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == leaveRequest.ApplicantId);
                    var currentYear = DateTime.Now.Year;
                    var previousYear = currentYear - 1;

                    if (user != null)
                    {
                        var vacationDaysThisYear = _context.VacationDaysModel
                       .Where(v => v.UserId == user.Id && v.Year == currentYear)
                       .FirstOrDefault();

                        var vacationDaysPreviousYear = _context.VacationDaysModel
                       .Where(v => v.UserId == user.Id && v.Year == previousYear)
                       .FirstOrDefault();

                        if (vacationDaysPreviousYear != null)
                        {
                            if (leaveRequest.IsHalfDay)
                            {
                                if (vacationDaysPreviousYear.PendingDays >= 0.5)
                                {
                                    vacationDaysPreviousYear.PendingDays -= 0.5;
                                    vacationDaysPreviousYear.UsedDays += 0.5;
                                }
                                else
                                {
                                    vacationDaysThisYear.PendingDays -= 0.5;
                                    vacationDaysThisYear.UsedDays += 0.5;
                                }
                            }
                            else
                            {
                                if (vacationDaysPreviousYear.PendingDays >= workdays)
                                {
                                    vacationDaysPreviousYear.PendingDays -= workdays;
                                    vacationDaysPreviousYear.UsedDays += workdays;
                                }
                                else
                                {
                                    vacationDaysPreviousYear.UsedDays += vacationDaysPreviousYear.PendingDays;
                                    workdays -= (int)vacationDaysPreviousYear.PendingDays;
                                    vacationDaysPreviousYear.PendingDays = 0;
                                    vacationDaysThisYear.PendingDays -= workdays;
                                    vacationDaysThisYear.UsedDays += workdays;
                                }
                            }
                        }
                        else
                        {
                            if (leaveRequest.IsHalfDay)
                            {
                                vacationDaysThisYear.PendingDays -= 0.5;
                                vacationDaysThisYear.UsedDays += 0.5;
                            }
                            else
                            {
                                vacationDaysThisYear.PendingDays -= workdays;
                                vacationDaysThisYear.UsedDays += workdays;
                            }
                        }

                        // Save changes to the database
                        await _context.SaveChangesAsync();
                    }
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RejectRequest(int id)
        {
            var leaveRequest = await _context.LeaveRequests.FirstOrDefaultAsync(lr => lr.Id == id);

            if (leaveRequest != null)
            {
                // TODO

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        // GET: LeaveRequests/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var leaveRequest = await _context.LeaveRequests
                .FirstOrDefaultAsync(m => m.Id == id);
            if (leaveRequest == null)
            {
                return NotFound();
            }

            return View(leaveRequest);
        }

        public async Task<IActionResult> Download(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var leaveRequest = await _context.LeaveRequests.FindAsync(id);
            if (leaveRequest == null || leaveRequest.MedicalCertificate == null)
            {
                return NotFound();
            }

            // Return the file for download
            return File(leaveRequest.MedicalCertificate, "application/octet-stream", "MedicalCertificate.pdf");
        }

        // GET: LeaveRequests/Create
        public IActionResult Create()
        {
            // Fetch the current user's username
            var username = User.Identity.Name;

            // Find the user in the AspNetUsers table by their username
            var user = _context.Users.SingleOrDefault(u => u.Username == username);

            // If the user is found, populate the ApplicantId field with their first and last name
            if (user != null)
            {
                var teamId = user.TeamId;
                var roleId = user.RoleId;
                var teams = _context.Teams.ToList();

                // Approvers based on RoleId
                if (teamId != null && roleId != 3 && roleId != 1 && roleId != 4)
                {
                    var userTeam = teams.FirstOrDefault(t => t.Id == teamId);
                    if (userTeam.TeamLeaderId == null)
                    {
                        var approvers = _context.Users
                        .Where(u => u.RoleId == 1)
                        .OrderByDescending(u => u.RoleId)
                        .Select(u => new SelectListItem
                        {
                            Value = u.Id.ToString(),
                            Text = $"{u.FirstName} {u.LastName}"
                        })
                        .ToList();
                        ViewBag.Approvers = approvers;
                    }
                    else
                    {
                        var approvers = _context.Users
                        .Where(u => (u.RoleId == 3 && u.TeamId == teamId))
                        .OrderByDescending(u => u.RoleId)
                        .Select(u => new SelectListItem
                        {
                            Value = u.Id.ToString(),
                            Text = $"{u.FirstName} {u.LastName}"
                        })
                        .ToList();
                        ViewBag.Approvers = approvers;
                    }
                }
                else if (roleId == 1)
                {
                    var approvers = _context.Users
                        .Where(u => u.RoleId == 1 && u.Id != user.Id)
                        .OrderByDescending(u => u.RoleId)
                        .Select(u => new SelectListItem
                        {
                            Value = u.Id.ToString(),
                            Text = $"{u.FirstName} {u.LastName}"
                        })
                        .ToList();
                    ViewBag.Approvers = approvers;
                }
                else //(roleId == 3 || roleId == 4)
                {
                    var approvers = _context.Users
                        .Where(u => u.RoleId == 1)
                        .OrderByDescending(u => u.RoleId)
                        .Select(u => new SelectListItem
                        {
                            Value = u.Id.ToString(),
                            Text = $"{u.FirstName} {u.LastName}"
                        })
                        .ToList();
                    ViewBag.Approvers = approvers;
                }

                //ViewBag.Approvers = approvers;

                var applicantName = $"{user.FirstName} {user.LastName}";
                ViewBag.ApplicantName = applicantName; // Pass the name to the view

                // Get the current year
                var currentYear = DateTime.Now.Year;
                var previousYear = currentYear - 1;

                // Find VacationDays for the logged-in user for the current year
                var vacationDaysThisYear = _context.VacationDaysModel
                    .Where(v => v.UserId == user.Id && v.Year == currentYear)
                    .FirstOrDefault();

                if (vacationDaysThisYear != null)
                {
                    ViewBag.CurrentYearVacationDays = vacationDaysThisYear.VacationDays;
                    ViewBag.CurrentYearUsedDays = vacationDaysThisYear.UsedDays;
                    var daysLeftThisYear = vacationDaysThisYear.VacationDays - vacationDaysThisYear.UsedDays - vacationDaysThisYear.PendingDays;
                    ViewBag.LeftDaysThisYear = daysLeftThisYear;
                }
                else
                {
                    ViewBag.CurrentYearVacationDays = 0; // Set default value if no record found
                    ViewBag.CurrentYearUsedDays = 0;
                    ViewBag.LeftDaysThisYear = 0;
                }

                // Find VacationDays for the logged-in user for the previous year
                var vacationDaysLastYear = _context.VacationDaysModel
                    .Where(v => v.UserId == user.Id && v.Year == previousYear)
                    .FirstOrDefault();

                if (vacationDaysLastYear != null)
                {
                    ViewBag.PreviousYearVacationDays = vacationDaysLastYear.VacationDays;
                    ViewBag.PreviousYearUsedDays = vacationDaysLastYear.UsedDays;
                    var daysLeftPreviousYear = vacationDaysLastYear.VacationDays - vacationDaysLastYear.UsedDays - vacationDaysLastYear.PendingDays;
                    ViewBag.LeftDaysPreviousYear = daysLeftPreviousYear;
                }
                else
                {
                    ViewBag.PreviousYearVacationDays = 0; // Set default value if no record found
                    ViewBag.PreviousYearUsedDays = 0;
                    ViewBag.LeftDaysPreviousYear = 0;
                }

                var leaveRequest = new LeaveRequest
                {
                    ApplicantId = user.Id,
                    RequestCreationDate = DateTime.Today,
                    StartDate = DateTime.Today.AddDays(1), // Next day from today
                    EndDate = DateTime.Today.AddDays(1)
                };

                return View(leaveRequest);
            }

            return View();
        }

        // POST: LeaveRequests/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,RequestCreationDate,ApplicantId,StartDate,EndDate,IsSickLeave,MedicalCertificate,IsPaid,IsHalfDay,IsApproved,ApproverId,IsCompleted")] LeaveRequest leaveRequest, double previousYearDays, double currentYearDays)
        {
            if (ModelState.IsValid)
            {
                // Validate the total number of days
                /* var startDate = leaveRequest.StartDate;
                 var endDate = leaveRequest.EndDate;
                 var workdays = CalculateWeekdays(startDate, endDate);*/

                // Fetch the current user's username
                var username = User.Identity.Name;

                var user = _context.Users.SingleOrDefault(u => u.Username == username);

                // If the user is found, set the ApplicantId
                if (user != null)
                {
                    leaveRequest.ApplicantId = user.Id;
                }

                // Handle the uploaded medical certificate file
                if (Request.Form.Files.Count > 0)
                {
                    var medicalCertificateFile = Request.Form.Files[0];
                    if (medicalCertificateFile.Length > 0)
                    {
                        // Read the file content into a byte array
                        using (var memoryStream = new MemoryStream())
                        {
                            await medicalCertificateFile.CopyToAsync(memoryStream);
                            leaveRequest.MedicalCertificate = memoryStream.ToArray(); // Store the file content as a byte array
                        }
                    }
                }

                int currentYear = DateTime.Now.Year;
                int previousYear = currentYear - 1;
                var vacationDaysCurrentYear = _context.VacationDaysModel
                    .SingleOrDefault(v => v.UserId == user.Id && v.Year == currentYear);
                var vacationDaysPreviousYear = _context.VacationDaysModel
                    .SingleOrDefault(v => v.UserId == user.Id && v.Year == previousYear);

                if (vacationDaysCurrentYear != null && vacationDaysPreviousYear != null)
                {
                    vacationDaysCurrentYear.PendingDays += currentYearDays;
                    _context.Update(vacationDaysCurrentYear);
                    vacationDaysPreviousYear.PendingDays += previousYearDays;
                    _context.Update(vacationDaysPreviousYear);
                }

                _context.Add(leaveRequest);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(leaveRequest);
        }

        private int CalculateWeekdays(DateTime startDate, DateTime endDate)
        {
            int count = 0;
            DateTime currentDate = startDate;

            while (currentDate <= endDate)
            {
                if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    count++;
                }
                currentDate = currentDate.AddDays(1);
            }

            return count;
        }

        // GET: LeaveRequests/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var leaveRequest = await _context.LeaveRequests.FindAsync(id);
            if (leaveRequest == null)
            {
                return NotFound();
            }
            return View(leaveRequest);
        }

        // POST: LeaveRequests/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,RequestCreationDate,ApplicantId,StartDate,EndDate,IsSickLeave,MedicalCertificate,IsPaid,IsHalfDay,IsApproved,ApproverId,IsCompleted")] LeaveRequest leaveRequest)
        {
            if (id != leaveRequest.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(leaveRequest);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LeaveRequestExists(leaveRequest.Id))
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
            return View(leaveRequest);
        }

        // GET: LeaveRequests/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var leaveRequest = await _context.LeaveRequests
                .FirstOrDefaultAsync(m => m.Id == id);
            if (leaveRequest == null)
            {
                return NotFound();
            }

            return View(leaveRequest);
        }

        // POST: LeaveRequests/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var leaveRequest = await _context.LeaveRequests.FindAsync(id);
            if (leaveRequest != null)
            {
                _context.LeaveRequests.Remove(leaveRequest);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LeaveRequestExists(int id)
        {
            return _context.LeaveRequests.Any(e => e.Id == id);
        }
    }
}
