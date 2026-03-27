using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DairyIndustry.Filters
{
    public class SessionAuthorizeAttribute : Attribute, IActionFilter
    {
        private readonly string _requiredRole;

        // Use [SessionAuthorize] for any logged-in user
        // Use [SessionAuthorize("Administrator")] for specific role
        public SessionAuthorizeAttribute(string requiredRole = null)
        {
            _requiredRole = requiredRole;
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

            // Logged in but wrong role
            if (_requiredRole != null && roleName != _requiredRole)
            {
                context.Result = new ViewResult
                {
                    ViewName = "AccessDenied"
                };
                return;
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}