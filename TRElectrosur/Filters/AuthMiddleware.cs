using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TRElectrosur.Services;

namespace TRElectrosur.Filters
{
    public class AuthorizeAttribute : TypeFilterAttribute
    {
        public AuthorizeAttribute() : base(typeof(AuthorizeFilter))
        {
        }
    }

    public class AuthorizeFilter : IAuthorizationFilter
    {
        private readonly AuthService _authService;

        public AuthorizeFilter(AuthService authService)
        {
            _authService = authService;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (!_authService.IsAuthenticated())
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }
        }
    }
}