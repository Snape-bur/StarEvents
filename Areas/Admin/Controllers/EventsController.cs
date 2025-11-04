using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ Original INDEX (shows all events for admin overview)
        public async Task<IActionResult> Index()
        {
            var events = _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .Include(e => e.Venue);

            return View(await events.ToListAsync());
        }

        // ✅ Event Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var @event = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(m => m.EventId == id);

            if (@event == null)
                return NotFound();

            return View(@event);
        }

        // ✅ Create Event (Admin override)
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name");
            ViewData["OrganizerId"] = new SelectList(_context.Users, "Id", "Email");
            ViewData["VenueId"] = new SelectList(
                _context.Venues
                    .Select(v => new
                    {
                        v.VenueId,
                        Display = v.Name + " (" + v.Location + ")"
                    }),
                "VenueId",
                "Display"
            );

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EventId,Title,Description,StartDate,EndDate,TicketPrice,TotalSeats,AvailableSeats,VenueId,CategoryId,OrganizerId,Status")] Event @event)
        {
            if (ModelState.IsValid)
            {
                _context.Add(@event);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Event created successfully.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", @event.CategoryId);
            ViewData["OrganizerId"] = new SelectList(_context.Users, "Id", "Email", @event.OrganizerId);
            ViewData["VenueId"] = new SelectList(
                _context.Venues.Select(v => new { v.VenueId, Display = v.Name + " (" + v.Location + ")" }),
                "VenueId",
                "Display",
                @event.VenueId
            );

            return View(@event);
        }

        // ✅ Edit Event
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var @event = await _context.Events.FindAsync(id);
            if (@event == null)
                return NotFound();

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", @event.CategoryId);
            ViewData["OrganizerId"] = new SelectList(_context.Users, "Id", "Email", @event.OrganizerId);
            ViewData["VenueId"] = new SelectList(
                _context.Venues.Select(v => new { v.VenueId, Display = v.Name + " (" + v.Location + ")" }),
                "VenueId",
                "Display",
                @event.VenueId
            );

            return View(@event);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EventId,Title,Description,StartDate,EndDate,TicketPrice,TotalSeats,AvailableSeats,VenueId,CategoryId,OrganizerId,Status")] Event @event)
        {
            if (id != @event.EventId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(@event);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Event updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventExists(@event.EventId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", @event.CategoryId);
            ViewData["OrganizerId"] = new SelectList(_context.Users, "Id", "Email", @event.OrganizerId);
            ViewData["VenueId"] = new SelectList(
                _context.Venues.Select(v => new { v.VenueId, Display = v.Name + " (" + v.Location + ")" }),
                "VenueId",
                "Display",
                @event.VenueId
            );

            return View(@event);
        }

        // ✅ Delete Event
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var @event = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(m => m.EventId == id);

            if (@event == null)
                return NotFound();

            return View(@event);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var @event = await _context.Events.FindAsync(id);
            if (@event != null)
            {
                _context.Events.Remove(@event);
                await _context.SaveChangesAsync();
            }

            TempData["Warning"] = "Event deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ✅ ADMIN EVENT APPROVAL WORKFLOW

        public async Task<IActionResult> Manage(string status)
        {
            var events = _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .Include(e => e.Venue)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "All")
                events = events.Where(e => e.Status == status);

            ViewBag.SelectedStatus = status;
            ViewBag.StatusList = new List<string> { "All", "Pending", "Approved", "Rejected" };

            return View(await events.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();

            ev.Status = "Approved";
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Event '{ev.Title}' has been approved.";
            return RedirectToAction(nameof(Manage));
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();

            ev.Status = "Rejected";
            await _context.SaveChangesAsync();

            TempData["Warning"] = $"Event '{ev.Title}' has been rejected.";
            return RedirectToAction(nameof(Manage));
        }

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.EventId == id);
        }
    }
}
