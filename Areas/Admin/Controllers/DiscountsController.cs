using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.Models;

namespace StarEvents.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DiscountsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DiscountsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Load event list for dropdown
        private void LoadEvents()
        {
            ViewBag.EventList = _context.Events
                .Where(e => e.Status == "Approved")
                .OrderBy(e => e.Title)
                .Select(e => new SelectListItem
                {
                    Value = e.EventId.ToString(),
                    Text = e.Title
                }).ToList();
        }

        // GET: Admin/Discounts
        public async Task<IActionResult> Index()
        {
            var discounts = await _context.Discounts
                .Include(d => d.Event)
                .OrderByDescending(d => d.StartDate)
                .ToListAsync();

            return View(discounts);
        }

        // GET: Admin/Discounts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var discount = await _context.Discounts
                .Include(d => d.Event)
                .FirstOrDefaultAsync(m => m.DiscountId == id);

            if (discount == null) return NotFound();

            return View(discount);
        }

        // GET: Admin/Discounts/Create
        public IActionResult Create()
        {
            LoadEvents(); // load event dropdown
            return View();
        }

        // POST: Admin/Discounts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Discount discount)
        {
            if (discount.StartDate > discount.EndDate)
            {
                ModelState.AddModelError("", "Start date cannot be after End date.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(discount);
                await _context.SaveChangesAsync();
                TempData["Ok"] = "✅ Discount created successfully.";
                return RedirectToAction(nameof(Index));
            }

            LoadEvents();
            return View(discount);
        }

        // GET: Admin/Discounts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null) return NotFound();

            LoadEvents();
            return View(discount);
        }

        // POST: Admin/Discounts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Discount discount)
        {
            if (id != discount.DiscountId) return NotFound();

            if (discount.StartDate > discount.EndDate)
            {
                ModelState.AddModelError("", "Start date cannot be after End date.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(discount);
                    await _context.SaveChangesAsync();
                    TempData["Ok"] = "✅ Discount updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DiscountExists(discount.DiscountId))
                        return NotFound();
                    else
                        throw;
                }

                return RedirectToAction(nameof(Index));
            }

            LoadEvents();
            return View(discount);
        }

        // GET: Admin/Discounts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var discount = await _context.Discounts
                .Include(d => d.Event)
                .FirstOrDefaultAsync(m => m.DiscountId == id);

            if (discount == null) return NotFound();

            return View(discount);
        }

        // POST: Admin/Discounts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var discount = await _context.Discounts.FindAsync(id);
            if (discount != null)
            {
                _context.Discounts.Remove(discount);
                await _context.SaveChangesAsync();
                TempData["Ok"] = "🗑️ Discount deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool DiscountExists(int id)
        {
            return _context.Discounts.Any(e => e.DiscountId == id);
        }
    }
}
