using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetUpPlaner.Models;
using Microsoft.AspNetCore.Identity;
using MeetUpPlaner.Services.Interfaces;

namespace MeetUpPlaner.Pages
{
    // Keep policy so authorization is enforced early via handler (owner or admin).
    [Authorize(Policy = "RequireAdminOrOwner")]
    public class EditModel : PageModel
    {
        private readonly IEventService _eventService;
        private readonly UserManager<IdentityUser> _userManager;

        public EditModel(IEventService eventService, UserManager<IdentityUser> userManager)
        {
            _eventService = eventService;
            _userManager = userManager;
        }

        [BindProperty]
        public Event Event { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var ev = await _eventService.GetByIdAsync(id);
            if (ev == null) return NotFound();

            Event = ev;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            var result = await _eventService.UpdateEventAsync(Event, currentUserId ?? string.Empty, isAdmin);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e);
                return Page();
            }

            return RedirectToPage("Index");
        }
    }
}