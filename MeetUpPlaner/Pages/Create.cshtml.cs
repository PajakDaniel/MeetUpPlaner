using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetUpPlaner.Data;
using MeetUpPlaner.Models;
using Microsoft.AspNetCore.Identity;
using MeetUpPlaner.Services.Interfaces;
using MeetUpPlaner.Services;

namespace MeetUpPlaner.Pages
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly IEventService _eventService;
        private readonly UserManager<IdentityUser> _userManager;

        public CreateModel(IEventService eventService, UserManager<IdentityUser> userManager)
        {
            _eventService = eventService;
            _userManager = userManager;
        }

        [BindProperty]
        public Event Event { get; set; } = new();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var userId = _userManager.GetUserId(User) ?? string.Empty;
            var result = await _eventService.CreateEventAsync(Event, userId);

            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, err);
                }
                return Page();
            }

            return RedirectToPage("Index");
        }
    }
}