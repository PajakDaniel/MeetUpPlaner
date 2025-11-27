using Microsoft.AspNetCore.Authorization;

namespace MeetUpPlaner.Authorization
{
    public class RequireAdminOrOwnerRequirement : IAuthorizationRequirement
    {
    }
}