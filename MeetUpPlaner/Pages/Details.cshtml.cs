using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetUpPlaner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using MeetUpPlaner.Services.Interfaces;

namespace MeetUpPlaner.Pages
{
    public class DetailsModel : PageModel
    {
        private readonly IEventService _eventService;
        private readonly IRsvpService _rsvpService;
        private readonly UserManager<IdentityUser> _userManager;

        public DetailsModel(IEventService eventService, IRsvpService rsvpService, UserManager<IdentityUser> um)
        {
            _eventService = eventService;
            _rsvpService = rsvpService;
            _userManager = um;
        }

        public Event? Event { get; set; }
        public bool UserRsvpExists { get; set; }
        public string? UserRsvpStatus { get; set; }

        public List<AttendeeViewModel> Attendees { get; set; } = new();

        public class AttendeeViewModel
        {
            public string DisplayName { get; set; } = "";
            public RsvpStatus Status { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Event = await _eventService.GetByIdAsync(id);
            if (Event == null) return NotFound();

            var rsvps = await _rsvpService.GetRsvpsForEventAsync(id);

            var userIds = rsvps.Select(r => r.UserId).Distinct().ToList();

            if (rsvps.Any())
            {
                var users = await _userManager.Users
                    .Where(u => userIds.Contains(u.Id))
                    .ToListAsync();

                var userMap = users.ToDictionary(u => u.Id, u => !string.IsNullOrEmpty(u.Email) ? u.Email : (u.UserName ?? u.Id));

                Attendees = rsvps
                    .Select(r => new AttendeeViewModel
                    {
                        DisplayName = userMap.ContainsKey(r.UserId) ? userMap[r.UserId] : r.UserId,
                        Status = r.Status
                    })
                    .OrderByDescending(a => a.Status == RsvpStatus.Going) // show going first
                    .ToList();
            }

            if (User.Identity?.IsAuthenticated ?? false)
            {
                var userId = _userManager.GetUserId(User) ?? string.Empty;
                UserRsvpExists = await _rsvpService.HasRsvpAsync(id, userId);
                var r = (await _rsvpService.GetRsvpsForEventAsync(id)).FirstOrDefault(x => x.UserId == userId);
                UserRsvpStatus = r?.Status.ToString();
            }

            return Page();
        }

        [Authorize]
        public async Task<IActionResult> OnPostRsvpAsync(int id, string status)
        {
            var userId = _userManager.GetUserId(User) ?? string.Empty;

            if (await _rsvpService.HasRsvpAsync(id, userId))
            {
                TempData["Message"] = "You already have an RSVP for this event.";
                return RedirectToPage(new { id });
            }

            if (!Enum.TryParse<RsvpStatus>(status, out var parsedStatus))
                parsedStatus = RsvpStatus.Going;

            var result = await _rsvpService.CreateRsvpAsync(id, userId, parsedStatus);
            if (!result.Succeeded)
            {
                TempData["Message"] = string.Join("; ", result.Errors);
            }
            else
            {
                TempData["Success"] = "RSVP submitted.";
            }

            return RedirectToPage(new { id });
        }

        [Authorize]
        public async Task<IActionResult> OnPostChangeStatusAsync(int id, string status)
        {
            var userId = _userManager.GetUserId(User) ?? string.Empty;

            if (!Enum.TryParse<RsvpStatus>(status, out var newStatus))
                newStatus = RsvpStatus.Going;

            var result = await _rsvpService.ChangeRsvpStatusAsync(id, userId, newStatus);
            if (!result.Succeeded)
            {
                TempData["Message"] = string.Join("; ", result.Errors);
            }
            else
            {
                TempData["Success"] = "RSVP updated.";
            }

            return RedirectToPage(new { id });
        }

        [Authorize]
        public async Task<IActionResult> OnPostCancelAsync(int id)
        {
            var userId = _userManager.GetUserId(User) ?? string.Empty;

            var result = await _rsvpService.CancelRsvpAsync(id, userId);
            if (!result.Succeeded)
            {
                TempData["Message"] = string.Join("; ", result.Errors);
            }
            else
            {
                TempData["Success"] = "RSVP canceled.";
            }

            return RedirectToPage(new { id });
        }
    }
}