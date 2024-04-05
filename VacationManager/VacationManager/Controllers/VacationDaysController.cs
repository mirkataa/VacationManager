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
    public class VacationDaysController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VacationDaysController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: VacationDays
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.ToListAsync();
            ViewBag.Users = users;

            return View(await _context.VacationDaysModel.ToListAsync());
        }

        // GET: VacationDays/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vacationDaysModel = await _context.VacationDaysModel
                .FirstOrDefaultAsync(m => m.Id == id);
            if (vacationDaysModel == null)
            {
                return NotFound();
            }

            return View(vacationDaysModel);
        }

        // GET: VacationDays/Create
        public IActionResult Create()
        {
            var users = _context.Users.ToList();

            var userOptions = users.Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = $"{u.Username} - {u.FirstName} {u.LastName}"
            }).ToList();

            ViewBag.UserOptions = userOptions;

            // Get the current year and previous year
            var currentYear = DateTime.Now.Year;
            var previousYear = currentYear - 1;

            // Create a list of selectable years
            var selectableYears = new List<SelectListItem>
            {
                new SelectListItem { Text = currentYear.ToString(), Value = currentYear.ToString() },
                new SelectListItem { Text = previousYear.ToString(), Value = previousYear.ToString() }
            };

            // Pass the list of selectable years to the view
            ViewBag.SelectableYears = selectableYears;

            // Set initial value for UsedDays
            var model = new VacationDaysModel
            {
                UsedDays = 0, // Initial value for UsedDays
                PendingDays = 0
            };

            return View(model);
        }

        // POST: VacationDays/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UserId,Year,VacationDays,UsedDays,PendingDays")] VacationDaysModel vacationDaysModel)
        {
            // Get the current year and previous year
            var currentYear = DateTime.Now.Year;
            var previousYear = currentYear - 1;

            // Create a list of selectable years
            var selectableYears = new List<SelectListItem>
            {
                new SelectListItem { Text = currentYear.ToString(), Value = currentYear.ToString() },
                new SelectListItem { Text = previousYear.ToString(), Value = previousYear.ToString() }
            };

            // Pass the list of selectable years to the view
            ViewBag.SelectableYears = selectableYears;

            var users = _context.Users.ToList();

            var userOptions = users.Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = $"{u.Username} - {u.FirstName} {u.LastName}"
            }).ToList();

            ViewBag.UserOptions = userOptions;

            if (ModelState.IsValid)
            {
                // Check if there's already an entry for the selected user and year
                var existingEntry = await _context.VacationDaysModel
                    .FirstOrDefaultAsync(v => v.UserId == vacationDaysModel.UserId && v.Year == vacationDaysModel.Year);

                if (existingEntry != null)
                {
                    // If an entry already exists, display a message to the user and return to the create view
                    ModelState.AddModelError(string.Empty, "A vacation days entry already exists for the selected user and year.");
                    return View(vacationDaysModel);
                }

                // Set UsedDays to 0 before adding to the context
                vacationDaysModel.UsedDays = 0;
                vacationDaysModel.PendingDays = 0;

                _context.Add(vacationDaysModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(vacationDaysModel);
        }

        // GET: VacationDays/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vacationDaysModel = await _context.VacationDaysModel.FindAsync(id);
            if (vacationDaysModel == null)
            {
                return NotFound();
            }
            return View(vacationDaysModel);
        }

        // POST: VacationDays/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,Year,VacationDays,UsedDays,PendingDays")] VacationDaysModel vacationDaysModel)
        {
            if (id != vacationDaysModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(vacationDaysModel);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VacationDaysModelExists(vacationDaysModel.Id))
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
            return View(vacationDaysModel);
        }

        // GET: VacationDays/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vacationDaysModel = await _context.VacationDaysModel
                .FirstOrDefaultAsync(m => m.Id == id);
            if (vacationDaysModel == null)
            {
                return NotFound();
            }

            return View(vacationDaysModel);
        }

        // POST: VacationDays/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var vacationDaysModel = await _context.VacationDaysModel.FindAsync(id);
            if (vacationDaysModel != null)
            {
                _context.VacationDaysModel.Remove(vacationDaysModel);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool VacationDaysModelExists(int id)
        {
            return _context.VacationDaysModel.Any(e => e.Id == id);
        }
    }
}
