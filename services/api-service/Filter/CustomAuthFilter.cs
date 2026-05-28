using Microsoft.AspNetCore.Mvc.Filters;
public class CustomAuthFilter : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (!(user?.Identity?.IsAuthenticated ?? false))
        {
            context.Result = new Microsoft.AspNetCore.Mvc.UnauthorizedResult();
        }
    }
}