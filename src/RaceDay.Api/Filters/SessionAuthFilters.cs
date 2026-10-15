using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireAuthAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var userId = context.HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                context.Result = new ObjectResult(new { message = "Authentication required. Please log in." })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _requiredRole;
        public RequireRoleAttribute(string requiredRole) => _requiredRole = requiredRole;

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var userId = context.HttpContext.Session.GetInt32("UserId");
            var role = context.HttpContext.Session.GetString("Role");

            if (!userId.HasValue || string.IsNullOrEmpty(role))
            {
                context.Result = new ObjectResult(new { message = "Authentication required. Please log in." })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
                return;
            }

            if (!string.Equals(role, _requiredRole, StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new ObjectResult(new { message = $"Forbidden. This action requires the '{_requiredRole}' role." })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
    }
}