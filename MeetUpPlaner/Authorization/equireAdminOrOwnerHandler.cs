using MeetUpPlaner.Data;
using MeetupPlanner.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MeetUpPlaner.Authorization
{
    /// <summary>
    /// AuthorizationHandler that grants access when the current user is in the 'Admin' role
    /// or is the owner (CreatedByUserId) of the Event identified by the 'id' route value.
    /// </summary>
    public class RequireAdminOrOwnerHandler : AuthorizationHandler<RequireAdminOrOwnerRequirement>
    {
        private readonly ApplicationDbContext _db;

        public RequireAdminOrOwnerHandler(ApplicationDbContext db)
        {
            _db = db;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RequireAdminOrOwnerRequirement requirement)
        {
            // If user is in Admin role, succeed immediately
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return;
            }

            // Try to get the route data id - resource for MVC/Razor is usually AuthorizationFilterContext
            if (context.Resource is AuthorizationFilterContext mvcContext)
            {
                // Try route values first
                if (mvcContext.RouteData.Values.TryGetValue("id", out var idObj) && int.TryParse(idObj?.ToString(), out var id))
                {
                    var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
                    if (ev != null)
                    {
                        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                        if (!string.IsNullOrEmpty(userId) && ev.CreatedByUserId == userId)
                        {
                            context.Succeed(requirement);
                            return;
                        }
                    }
                }

                // If id not in route data, try query string (in case your page sends id via ?id=)
                var qs = mvcContext.HttpContext.Request.Query;
                if (qs.TryGetValue("id", out var qVal) && int.TryParse(qVal.ToString(), out var qid))
                {
                    var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == qid);
                    if (ev != null)
                    {
                        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                        if (!string.IsNullOrEmpty(userId) && ev.CreatedByUserId == userId)
                        {
                            context.Succeed(requirement);
                            return;
                        }
                    }
                }
            }

            // Resource was not the MVC filter context or owner check failed -> do not succeed.
            // Do not call context.Fail() explicitly; leaving it will result in failure if no requirement succeeded.
        }
    }
}