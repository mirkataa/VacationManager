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
    public class LeaveRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LeaveRequestsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: LeaveRequests
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var totalRequestsCount = await _context.LeaveRequests.CountAsync();
            var leaveRequests = await _context.LeaveRequests.ToListAsync();
            var leaveRemove = await _context.LeaveRequests.Where(l => !l.IsCompleted).ToListAsync();

            // Update IsAway for each applicant
            foreach (var leaveRequest in leaveRequests)
            {
                await UpdateIsAwayForUser(leaveRequest.ApplicantId);
            }

            DateTime today = DateTime.Today;
            foreach (var rem in leaveRemove)
            {
                if (rem.StartDate <= today)
                {
                    await DeleteConfirmed(rem.Id);
                }
            }

            var username = User.Identity.Name;

            var user = _context.Users.SingleOrDefault(u => u.Username == username);

            await UpdateIsAwayForUser(user.Id);

            ViewBag.TotalCount = totalRequestsCount;
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentPage = page;

            return View(leaveRequests);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveRequest(int id)
        {
            var leaveRequest = await _context.LeaveRequests.FirstOrDefaultAsync(lr => lr.Id == id);

            if (leaveRequest != null)
            {
                leaveRequest.IsApproved = true;
                leaveRequest.IsCompleted = true;
                // Fetch the user associated with the leave request
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == leaveRequest.ApplicantId);
               // user.IsAway = true;
                await _context.SaveChangesAsync();
                if (leaveRequest.IsPaid)
                {
                    // Calculate workdays between start and end date
                    var workdays = CalculateWeekdays(leaveRequest.StartDate, leaveRequest.EndDate);

                    var currentYear = DateTime.Now.Year;
                    var previousYear = currentYear - 1;

                    if (user != null && workdays > 0)
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
                await UpdateIsAwayForUser(leaveRequest.ApplicantId);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RejectRequest(int id)
        {
            var leaveRequest = await _context.LeaveRequests.FirstOrDefaultAsync(lr => lr.Id == id);

            if (leaveRequest != null)
            {
                leaveRequest.IsApproved = false;
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

                    if (user != null && workdays > 0)
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
                                }
                                else
                                {
                                    vacationDaysThisYear.PendingDays -= 0.5;
                                }
                            }
                            else
                            {
                                if (vacationDaysPreviousYear.PendingDays >= workdays)
                                {
                                    vacationDaysPreviousYear.PendingDays -= workdays;
                                }
                                else
                                {
                                    workdays -= (int)vacationDaysPreviousYear.PendingDays;
                                    vacationDaysPreviousYear.PendingDays = 0;
                                    vacationDaysThisYear.PendingDays -= workdays;
                                }
                            }
                        }
                        else
                        {
                            if (leaveRequest.IsHalfDay)
                            {
                                vacationDaysThisYear.PendingDays -= 0.5;
                            }
                            else
                            {
                                vacationDaysThisYear.PendingDays -= workdays;
                            }
                        }

                        // Save changes to the database
                        await _context.SaveChangesAsync();
                    }
                }
                await UpdateIsAwayForUser(leaveRequest.ApplicantId);
            }

            return RedirectToAction("Index");
        }

        private async Task UpdateIsAwayForUser(int userId)
        {
            var today = DateTime.Today;

            var leaveRequests = await _context.LeaveRequests
                .Where(lr => lr.ApplicantId == userId && lr.IsApproved && lr.IsCompleted && today >= lr.StartDate && today <= lr.EndDate)
                .ToListAsync();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (leaveRequests.Any())
            {
                user.IsAway = true;
            }
            else
            {
                user.IsAway = false;
            }

            await _context.SaveChangesAsync();
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

            var user = _context.Users.SingleOrDefault(u => u.Id == leaveRequest.ApplicantId);
            
            var applicantName = $"{user.FirstName} {user.LastName}";
            ViewBag.ApplicantName = applicantName; // Pass the name to the view

            var approver = _context.Users.SingleOrDefault(u => u.Id == leaveRequest.ApproverId);
            var approverName = "";
            if (approver != null) 
            { 
                 approverName = $"{user.FirstName} {user.LastName}";
            }
            else
            {
                 approverName = "Medical leaves do not have an approver.";
            }

            ViewBag.ApproverName = approverName;

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

                if (leaveRequest.IsSickLeave)
                {
                    var sickStart = leaveRequest.StartDate;
                    var sickEnd = leaveRequest.EndDate;

                    // Leaverequests that have started before the sick leave and continue during or after the sick leave
                    var intersectBeforeAndDuring = await _context.LeaveRequests
                        .Where(lr => lr.ApplicantId == leaveRequest.ApplicantId && lr.IsApproved && lr.IsCompleted &&
                                     lr.StartDate < sickStart && lr.EndDate >= sickStart)
                        .ToListAsync();

                    // Leaverequests that intersect with the sick leave (including those that start during or end during the sick leave)
                    var intersectWithSickLeave = await _context.LeaveRequests
                        .Where(lr => lr.ApplicantId == leaveRequest.ApplicantId && lr.IsApproved && lr.IsCompleted &&
                                     lr.StartDate <= sickEnd && lr.EndDate >= sickStart)
                        .ToListAsync();

                    foreach (var item in intersectWithSickLeave)
                    {

                        var workingDays = CalculateWeekdays(item.StartDate, item.EndDate);
                        var itemApplicant = await _context.Users.FirstOrDefaultAsync(u => u.Id == item.ApplicantId);

                        if (itemApplicant != null && workingDays > 0 && item.IsPaid)
                        {
                            var vacDaysThisYear = _context.VacationDaysModel
                           .Where(v => v.UserId == itemApplicant.Id && v.Year == currentYear)
                           .FirstOrDefault();

                            var vacDaysPreviousYear = _context.VacationDaysModel
                           .Where(v => v.UserId == itemApplicant.Id && v.Year == previousYear)
                           .FirstOrDefault();

                            if (vacDaysPreviousYear != null)
                            {
                                if (item.IsHalfDay)
                                {
                                    if (vacDaysThisYear.UsedDays > 0)
                                    {
                                        vacDaysThisYear.UsedDays -= 0.5;
                                    }
                                    else
                                    {
                                        vacDaysPreviousYear.UsedDays -= 0.5;
                                    }
                                }
                                else
                                {
                                    if (vacDaysThisYear.UsedDays >= workingDays)
                                    {
                                        vacDaysThisYear.UsedDays -= workingDays;
                                    }
                                    else
                                    {
                                        workingDays -= (int)vacDaysThisYear.UsedDays;
                                        vacDaysThisYear.UsedDays = 0;
                                        vacDaysPreviousYear.UsedDays -= workingDays;
                                    }
                                }
                            }
                            else
                            {
                                if (item.IsHalfDay)
                                {
                                    vacDaysThisYear.UsedDays -= 0.5;
                                }
                                else
                                {
                                    vacDaysThisYear.UsedDays -= workingDays;
                                }
                            }

                            await _context.SaveChangesAsync();

                            if (intersectBeforeAndDuring.Contains(item) && item.IsPaid)
                            {
                                var daysToGet = CalculateWeekdays(item.StartDate, sickStart);
                                if (daysToGet > 0 && vacDaysPreviousYear.VacationDays > vacDaysPreviousYear.UsedDays)
                                {
                                    if (daysToGet <= (vacDaysPreviousYear.VacationDays - vacDaysPreviousYear.UsedDays))
                                    {
                                        vacDaysPreviousYear.UsedDays += daysToGet;
                                    }
                                    else
                                    {
                                        var sup = vacDaysPreviousYear.VacationDays - vacDaysPreviousYear.UsedDays;
                                        daysToGet = daysToGet - (int)sup;
                                        vacDaysPreviousYear.UsedDays += (int)sup;
                                        vacDaysThisYear.UsedDays += daysToGet;
                                    }
                                }
                                else if (daysToGet > 0)
                                {
                                    vacDaysThisYear.UsedDays += daysToGet;
                                }
                            }

                            // Save changes to the database
                            item.IsApproved = false;
                            await _context.SaveChangesAsync();
                        }
                        else if (itemApplicant != null && !item.IsSickLeave)
                        {
                            item.IsApproved = false;
                            await _context.SaveChangesAsync();
                        }
                    }
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

            var username = User.Identity.Name;

            var user = _context.Users.SingleOrDefault(u => u.Username == username);

            // If the user is found, populate the ApplicantId field with their first and last name
            if (user != null)
            {
                var teamId = user.TeamId;
                var roleId = user.RoleId;
                var teams = _context.Teams.ToList();

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
                    var username = User.Identity.Name;

                    var user = _context.Users.SingleOrDefault(u => u.Username == username);

                    // If the user is found, populate the ApplicantId field with their first and last name
                    if (user != null)
                    {
                        var teamId = user.TeamId;
                        var roleId = user.RoleId;
                        var teams = _context.Teams.ToList();

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
                    }
                    // Get the original entry from the database
                    var originalLeaveRequest = await _context.LeaveRequests.FindAsync(id);
                    if (originalLeaveRequest == null)
                    {
                        return NotFound();
                    }
                    _context.Entry(originalLeaveRequest).State = EntityState.Detached;
                    if (originalLeaveRequest.IsPaid)
                    {
                        if (!originalLeaveRequest.IsHalfDay)
                        {
                            var newWorkDays = CalculateWeekdays(leaveRequest.StartDate, leaveRequest.EndDate);
                            var oldWorkDays = CalculateWeekdays(originalLeaveRequest.StartDate, originalLeaveRequest.EndDate);
                            int currentYear = DateTime.Now.Year;
                            int previousYear = currentYear - 1;
                            var vacationDaysCurrentYear = _context.VacationDaysModel
                                .SingleOrDefault(v => v.UserId == leaveRequest.ApplicantId && v.Year == currentYear);
                            var vacationDaysPreviousYear = _context.VacationDaysModel
                                .SingleOrDefault(v => v.UserId == leaveRequest.ApplicantId && v.Year == previousYear);

                            var sumVacDays = vacationDaysPreviousYear.VacationDays + vacationDaysCurrentYear.VacationDays;
                            var sumUsedDays = vacationDaysPreviousYear.UsedDays + vacationDaysCurrentYear.UsedDays;
                            var sumPendingDays = vacationDaysPreviousYear.PendingDays + vacationDaysCurrentYear.PendingDays;

                            var leftDaysWhole = sumVacDays - (sumUsedDays + sumPendingDays) + oldWorkDays;
                            if (newWorkDays > leftDaysWhole)
                            {
                                ModelState.AddModelError(string.Empty, "You do not have enough vacation days available to cover the requested period.");
                                return View(leaveRequest); // Display error message in the view
                            }

                            if (newWorkDays > oldWorkDays)
                            {
                                var diff = newWorkDays - oldWorkDays;
                                if (vacationDaysPreviousYear != null)
                                {
                                    if (vacationDaysPreviousYear.PendingDays + vacationDaysPreviousYear.UsedDays + diff <= vacationDaysPreviousYear.VacationDays)
                                    {
                                        vacationDaysPreviousYear.PendingDays += diff;
                                    }
                                    else
                                    {
                                        var sub = vacationDaysPreviousYear.VacationDays - (vacationDaysPreviousYear.PendingDays + vacationDaysPreviousYear.UsedDays);
                                        diff -= (int)sub;
                                        vacationDaysCurrentYear.PendingDays += diff;
                                    }
                                }
                                else
                                {
                                    vacationDaysCurrentYear.PendingDays += diff;
                                }
                            }
                            else if (newWorkDays < oldWorkDays)
                            {
                                var diff = oldWorkDays - newWorkDays;
                                if (vacationDaysPreviousYear != null)
                                {
                                    if (vacationDaysCurrentYear.PendingDays >= diff)
                                    {
                                        vacationDaysCurrentYear.PendingDays -= diff;
                                    }
                                    else
                                    {
                                        var sub = diff - vacationDaysCurrentYear.PendingDays;
                                        vacationDaysCurrentYear.PendingDays = 0;
                                        vacationDaysPreviousYear.PendingDays -= sub;
                                    }
                                }
                                else
                                {
                                    vacationDaysCurrentYear.PendingDays -= diff;
                                }
                            }
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            var newWorkDays = CalculateWeekdays(leaveRequest.StartDate, leaveRequest.EndDate);
                            var oldWorkDays = CalculateWeekdays(originalLeaveRequest.StartDate, originalLeaveRequest.EndDate);
                            int currentYear = DateTime.Now.Year;
                            int previousYear = currentYear - 1;
                            var vacationDaysCurrentYear = _context.VacationDaysModel
                                .SingleOrDefault(v => v.UserId == leaveRequest.ApplicantId && v.Year == currentYear);
                            var vacationDaysPreviousYear = _context.VacationDaysModel
                                .SingleOrDefault(v => v.UserId == leaveRequest.ApplicantId && v.Year == previousYear);
                            if (newWorkDays > oldWorkDays)
                            {
                                if (vacationDaysPreviousYear != null)
                                {
                                    if (vacationDaysPreviousYear.PendingDays + vacationDaysPreviousYear.UsedDays + 0.5 <= vacationDaysPreviousYear.VacationDays)
                                    {
                                        vacationDaysPreviousYear.PendingDays += 0.5;
                                    }
                                    else
                                    {
                                        vacationDaysCurrentYear.PendingDays += 0.5;
                                    }
                                }
                                else
                                {
                                    vacationDaysCurrentYear.PendingDays += 0.5;
                                }
                            }
                            else if (newWorkDays < oldWorkDays)
                            {
                                if (vacationDaysCurrentYear.PendingDays >= 0.5)
                                {
                                    vacationDaysCurrentYear.PendingDays -= 0.5;
                                }
                                else
                                {
                                    vacationDaysPreviousYear.PendingDays -= 0.5;
                                }
                            }
                            await _context.SaveChangesAsync();
                        }
                    }
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
                if (leaveRequest.IsPaid)
                {
                    var workDaysReturn = CalculateWeekdays(leaveRequest.StartDate, leaveRequest.EndDate);
                    int currentYear = DateTime.Now.Year;
                    int previousYear = currentYear - 1;
                    var vacationDaysCurrentYear = _context.VacationDaysModel
                        .SingleOrDefault(v => v.UserId == leaveRequest.ApplicantId && v.Year == currentYear);
                    var vacationDaysPreviousYear = _context.VacationDaysModel
                        .SingleOrDefault(v => v.UserId == leaveRequest.ApplicantId && v.Year == previousYear);

                    if (!leaveRequest.IsHalfDay)
                    {
                        if (vacationDaysPreviousYear != null)
                        {
                            if (vacationDaysCurrentYear.PendingDays >= workDaysReturn)
                            {
                                vacationDaysCurrentYear.PendingDays -= workDaysReturn;
                            }
                            else
                            {
                                var sub = workDaysReturn - vacationDaysCurrentYear.PendingDays;
                                vacationDaysCurrentYear.PendingDays = 0;
                                vacationDaysPreviousYear.PendingDays -= sub;
                            }
                        }
                        else
                        {
                            vacationDaysCurrentYear.PendingDays -= workDaysReturn;
                        }
                    }
                    else
                    {
                        if (vacationDaysPreviousYear != null)
                        {
                            if (vacationDaysCurrentYear.PendingDays >= 0.5)
                            {
                                vacationDaysCurrentYear.PendingDays -= 0.5;
                            }
                            else
                            {
                                vacationDaysCurrentYear.PendingDays = 0;
                                vacationDaysPreviousYear.PendingDays -= 0.5;
                            }
                        }
                        else
                        {
                            vacationDaysCurrentYear.PendingDays -= 0.5;
                        }
                    }
                    await _context.SaveChangesAsync();
                }
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
