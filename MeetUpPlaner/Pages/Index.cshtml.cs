using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetUpPlaner.Services.Interfaces;
using MeetUpPlaner.Models;
using Microsoft.AspNetCore.Identity;

namespace MeetUpPlaner.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IEventService _eventService;
        private readonly UserManager<IdentityUser> _userManager;

        public IndexModel(IEventService eventService, UserManager<IdentityUser> userManager)
        {
            _eventService = eventService;
            _userManager = userManager;
        }

        public List<Event> Events { get; set; } = new();
        public string? CurrentUserId { get; set; }
        public bool CurrentUserIsAdmin { get; set; }

        public async Task OnGetAsync()
        {
            Events = await _eventService.GetUpcomingAsync(30);

            if (User.Identity?.IsAuthenticated ?? false)
            {
                CurrentUserId = _userManager.GetUserId(User);
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    CurrentUserIsAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
                }
            }
        }
    }
}