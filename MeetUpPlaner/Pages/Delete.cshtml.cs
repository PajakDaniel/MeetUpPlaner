using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetUpPlaner.Models;
using Microsoft.AspNetCore.Identity;
using MeetUpPlaner.Services.Interfaces;

namespace MeetUpPlaner.Pages
{
    [Authorize(Policy = "RequireAdminOrOwner")]
    public class DeleteModel : PageModel
    {
        private readonly IEventService _eventService;
        private readonly UserManager<IdentityUser> _userManager;

        public DeleteModel(IEventService eventService, UserManager<IdentityUser> userManager)
        {
            _eventService = eventService;
            _userManager = userManager;
        }

        public Event? Event { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Event = await _eventService.GetByIdAsync(id);
            if (Event == null) return NotFound();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var currentUserId = _userManager.GetUserId(User) ?? string.Empty;
            var isAdmin = User.IsInRole("Admin");

            var result = await _eventService.DeleteEventAsync(id, currentUserId, isAdmin);
            if (!result.Succeeded)
            {
                // surface errors to the UI
                foreach (var e in result.Errors) TempData["Message"] = (TempData["Message"] != null ? TempData["Message"] + " " : "") + e;
                return RedirectToPage(new { id });
            }

            return RedirectToPage("Index");
        }
    }
}