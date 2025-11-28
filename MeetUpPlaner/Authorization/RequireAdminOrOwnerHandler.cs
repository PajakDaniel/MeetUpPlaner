using MeetupPlanner.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MeetUpPlaner.Authorization
{
    /// AuthorizationHandler that grants access when the current user is in the 'Admin' role
    /// or is the owner (CreatedByUserId) of the Event identified by the 'id' route value.
    public class RequireAdminOrOwnerHandler : AuthorizationHandler<RequireAdminOrOwnerRequirement>
    {
        private readonly ApplicationDbContext _db;

        public RequireAdminOrOwnerHandler(ApplicationDbContext db)
        {
            _db = db;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RequireAdminOrOwnerRequirement requirement)
        {
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return;
            }

            if (context.Resource is AuthorizationFilterContext mvcContext)
            {
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

        }
    }
}