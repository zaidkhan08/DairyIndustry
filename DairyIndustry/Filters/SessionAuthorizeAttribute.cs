using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DairyIndustry.Filters
{
    public class SessionAuthorizeAttribute : Attribute, IActionFilter
    {
        private readonly string[] _requiredRoles;

        // Use [SessionAuthorize] → any logged-in user
        // Use [SessionAuthorize("Administrator")] → 1 role
        // Use [SessionAuthorize("Administrator", "Manager")] → 2 roles
        // Use [SessionAuthorize("Administrator", "Manager", "Supervisor")] → 3 roles
        public SessionAuthorizeAttribute(
            string role1 = null,
            string role2 = null,
            string role3 = null)
        {
            // Filter out nulls/empty strings, store only provided roles
            _requiredRoles = new[] { role1, role2, role3 }
                .Where(r => !string.IsNullOrEmpty(r))
                .ToArray();
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = context.HttpContext.Session.GetInt32("UserId");
            var roleName = context.HttpContext.Session.GetString("RoleName");

            // Not logged in
            if (userId == null || string.IsNullOrEmpty(roleName))
            {
                context.Result = new RedirectToActionResult("Login", "Admin", null);
                return;
            }

            // Logged in but role doesn't match any of the required roles
            if (_requiredRoles.Length > 0 && !_requiredRoles.Contains(roleName))
            {
                context.Result = new ViewResult
                {
                    ViewName = "AccessDenied"
                };
                return;
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}